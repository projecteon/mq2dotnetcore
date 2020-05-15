using MQ2DotNetCore.Base;
using MQ2DotNetCore.Interop;
using MQ2DotNetCore.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MQ2DotNetCore.MQ2Api
{
	/// <summary>
	/// Creates a wrapper class from an MQ2TypeVar
	/// </summary>
	public class MQ2TypeFactory : IDisposable
	{
		internal static readonly MQ2TypeFactory RootFactory;

		static MQ2TypeFactory()
		{
			RootFactory = new MQ2TypeFactory();
			RootFactory.RegisterTypesInAssembly(MQ2DotNetCoreAssemblyInformation.MQ2DotNetCoreAssembly);
		}

		private readonly ConcurrentDictionary<IntPtr, Func<MQ2TypeFactory, MQ2TypeVar, MQ2DataType>> _constructorsDictionary =
			new ConcurrentDictionary<IntPtr, Func<MQ2TypeFactory, MQ2TypeVar, MQ2DataType>>();

		private bool _isDisposed = false;

		private readonly object _lock = new object();

		private readonly List<Assembly> _registeredAssemblies = new List<Assembly>();

		private readonly MQ2TypeFactory? _parentFactory;

		private MQ2TypeFactory()
		{
			_parentFactory = null;
		}

		/// <summary>
		/// Create a new MQ2TypeFactory that can create any loaded types with an MQ2Type attribute.
		/// </summary>
		public MQ2TypeFactory(MQ2TypeFactory parentFactory)
		{
			_parentFactory = parentFactory;
		}

		public static bool CanSkipProgressing(Assembly assemblyToCheck)
		{
			if (assemblyToCheck == null)
			{
				return true;
			}

			// We only need to look for types in the MQ2DotNetCore assembly or in assemblies that reference it...
			if (assemblyToCheck == MQ2DotNetCoreAssemblyInformation.MQ2DotNetCoreAssembly)
			{
				return false;
			}

			var thisAssemblyName = MQ2DotNetCoreAssemblyInformation.AssemblyName.Name ?? "MQ2DotNetCore";
			return !assemblyToCheck.GetReferencedAssemblies()
				.Any(referencedAssembly => referencedAssembly?.Name?.Contains(thisAssemblyName) == true);
		}

		/// <summary>
		/// Create the appropriate wrapper type given an MQ2TypeVar
		/// </summary>
		/// <param name="typeVar"></param>
		/// <returns></returns>
		public MQ2DataType Create(MQ2TypeVar typeVar)
		{
			CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2TypeFactory));

#if DEBUG
			FileLoggingHelper.LogTrace($"Attempting to construct instance of: {typeVar}");
#endif

			if (_parentFactory != null
				&& _parentFactory._constructorsDictionary.TryGetValue(typeVar.pType, out var registeredConstructorFromParent))
			{
				return registeredConstructorFromParent(this, typeVar);
			}

			// If we have a special constructor registered, use it, otherwise create an MQ2DataType by default
			if (_constructorsDictionary.TryGetValue(typeVar.pType, out var registeredConstructor))
			{
				return registeredConstructor(this, typeVar);
			}

			FileLoggingHelper.LogWarning($"Did not find registered constructor for: {typeVar}");
			return new MQ2DataType(this, typeVar);
		}

		public void RegisterTypesInAssembly(Assembly assemblyToRegisterTypesFor)
		{
			CleanupHelper.DisposedCheck(_isDisposed, nameof(MQ2TypeFactory));

			if (assemblyToRegisterTypesFor == null)
			{
				FileLoggingHelper.LogWarning($"Assembly to register types for is null");
				return;
			}

			FileLoggingHelper.LogDebug($"Registering types in assembly: {assemblyToRegisterTypesFor.FullName}");

			if (_registeredAssemblies.Contains(assemblyToRegisterTypesFor)
				|| _parentFactory?._registeredAssemblies.Contains(assemblyToRegisterTypesFor) == true)
			{
				FileLoggingHelper.LogDebug($"Assembly is already registered: {assemblyToRegisterTypesFor.FullName}");
				return;
			}

			// Ensure assemblies are only processed once
			// Hoping that this combined with registering the event prior to all existing types is enough to ensure exactly once processing of each...
			lock (_lock)
			{
				if (_registeredAssemblies.Contains(assemblyToRegisterTypesFor))
				{
					return;
				}

				_registeredAssemblies.Add(assemblyToRegisterTypesFor);
			}

			try
			{
				if (CanSkipProgressing(assemblyToRegisterTypesFor))
				{
					return;
				}

				var constructorTypeArguments = new[] { typeof(MQ2TypeFactory), typeof(MQ2TypeVar) };

				// Find all subclasses of MQ2DataType, and get their MQ2Type attribute
				foreach (var type in assemblyToRegisterTypesFor.GetTypes())
				{
					if (!type.IsSubclassOf(typeof(MQ2DataType)))
					{
						continue;
					}

					var mq2TypeAttribute = type.GetCustomAttribute<MQ2TypeAttribute>();
					// Not sure why you'd want it without the attribute but I'll allow it
					if (mq2TypeAttribute == null)
					{
						continue;
					}

					// It needs a public constructor with a MQ2TypeFacotry and a MQ2TypeVar parameter
					var constructorForType = type.GetConstructor(constructorTypeArguments);

					
					if (constructorForType == null
						&& assemblyToRegisterTypesFor == MQ2DotNetCoreAssemblyInformation.MQ2DotNetCoreAssembly)
					{
						// If the type is defined in this assembly, allow a non public constructor too
						constructorForType = type
							.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
							.SingleOrDefault(nextConstructor =>
							{
								var parameters = nextConstructor.GetParameters();
								return nextConstructor.IsAssembly
									&& parameters.Length == 2
									&& parameters[0].ParameterType == typeof(MQ2TypeFactory)
									&& parameters[1].ParameterType == typeof(MQ2TypeVar);
							});
					}

					if (constructorForType == null)
					{
						throw new MissingMethodException(
							$"{type.Name} is marked as an MQ2Type, but does not have a constructor taking an MQ2TypeFactory & MQ2TypeVar");
					}

					// Create a compiled lambda that creates an instance of this type
					// Ivan says it's fast: https://ru.stackoverflow.com/questions/860901/%D0%A1%D1%83%D1%89%D0%B5%D1%81%D1%82%D0%B2%D1%83%D0%B5%D1%82-%D0%BB%D0%B8-%D0%B2%D0%BE%D0%B7%D0%BC%D0%BE%D0%B6%D0%BD%D0%BE%D1%81%D1%82%D1%8C-%D1%81%D0%BE%D0%B7%D0%B4%D0%B0%D0%B2%D0%B0%D1%82%D1%8C-%D0%BE%D0%B1%D1%8A%D0%B5%D0%BA%D1%82-%D0%BE%D0%BF%D1%80%D0%B5%D0%B4%D0%B5%D0%BB%D0%B5%D0%BD%D0%BD%D0%BE%D0%B3%D0%BE-%D1%82%D0%B8%D0%BF%D0%B0-%D0%B1%D0%B5%D0%B7-%D0%B8%D1%81%D0%BF%D0%BE%D0%BB%D1%8C%D0%B7%D0%BE%D0%B2%D0%B0%D0%BD%D0%B8%D1%8F/860921#860921
					var typeFactoryParam = Expression.Parameter(typeof(MQ2TypeFactory));
					var typeVarParam = Expression.Parameter(typeof(MQ2TypeVar));
					var constructor = Expression.Lambda<Func<MQ2TypeFactory, MQ2TypeVar, MQ2DataType>>(
						Expression.New(constructorForType, typeFactoryParam, typeVarParam), typeFactoryParam, typeVarParam);

#if DEBUG
					FileLoggingHelper.LogTrace($"Registering constructor for type: {mq2TypeAttribute.TypeName}");
#endif

					Register(mq2TypeAttribute.TypeName, constructor.Compile());
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError("Error finding types in assembly: " + assemblyToRegisterTypesFor.GetName());
				FileLoggingHelper.LogError(exc);
			}
		}

		/// <summary>
		/// Register a type
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="constructor"></param>
		private void Register(string typeName, Func<MQ2TypeFactory, MQ2TypeVar, MQ2DataType> constructor)
		{
			var dataType = MQ2Main.NativeMethods.FindMQ2DataType(typeName);

			if (dataType == IntPtr.Zero)
			{
				throw new KeyNotFoundException($"Could not find data type: {typeName}");
			}

			if (_constructorsDictionary.ContainsKey(dataType))
			{
				throw new InvalidOperationException($"An MQ2DataType for {typeName} has already been registered");
			}

#if DEBUG
			FileLoggingHelper.LogTrace($"Adding constructor to dictionary for data type pointer: {dataType}");
#endif

			if (!_constructorsDictionary.TryAdd(dataType, constructor))
			{
				throw new InvalidOperationException($"Failed to add constructor for {typeName} to the {nameof(_constructorsDictionary)}");
			}
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_constructorsDictionary.Clear();
			_registeredAssemblies.Clear();

			_isDisposed = true;
		}
	}
}
