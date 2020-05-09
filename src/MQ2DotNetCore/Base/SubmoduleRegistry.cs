using MQ2DotNetCore.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.Base
{
	internal sealed class SubmoduleRegistry : CriticalFinalizerObject, IDisposable
	{
		internal static readonly SubmoduleRegistry Instance = new SubmoduleRegistry();

		private bool _isDisposed = false;
		private object _lock = new object();

		private readonly Dictionary<string, SubmoduleProgramWrapper> _programsDictionary
			= new Dictionary<string, SubmoduleProgramWrapper>();

		private SubmoduleRegistry()
		{

		}

		// Finalizer
		~SubmoduleRegistry()
		{
			UnloadAll();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			UnloadAll();
			GC.SuppressFinalize(this);
			_isDisposed = true;
		}

		internal bool StartProgram(string submoduleProgramName, string[] commandArguments)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			AssemblyLoadContext? assemblyLoadContext = null;
			CancellationTokenSource? cancellationTokenSource = null;
			IMQ2Program? submoduleProgramInstance = null;
			Task? submoduleProgramTask = null;
			try
			{
				if (string.IsNullOrWhiteSpace(submoduleProgramName))
				{
					throw new ArgumentNullException(nameof(submoduleProgramName), "cannot be null, empty, or whitespace.");
				}

				if (_programsDictionary.ContainsKey(submoduleProgramName))
				{
					FileLoggingHelper.LogInformation($"A submodule program instance is already loaded/running with the name: {submoduleProgramName}");
					return false;
				}

				lock (_lock)
				{
					if (_programsDictionary.ContainsKey(submoduleProgramName))
					{
						FileLoggingHelper.LogInformation($"A submodule program instance is already loaded/running with the name: {submoduleProgramName}");
						return false;
					}


					var submoduleFilePath = Path.Combine(AssemblyInformation.AssemblyDirectory, "Programs", submoduleProgramName, $"{submoduleProgramName}.dll");
					if (!File.Exists(submoduleFilePath))
					{
						FileLoggingHelper.LogWarning($"Submodule program file not found: {submoduleFilePath}");
						return false;
					}

					assemblyLoadContext = new SubmoduleAssemblyLoadContext(submoduleProgramName, submoduleFilePath);
					var submoduleAssembly = assemblyLoadContext.LoadFromAssemblyPath(submoduleFilePath);

					var submoduleTypes = submoduleAssembly.GetTypes();
					var submoduleProgramTypes = submoduleTypes
						.Where(nextType => typeof(IMQ2Program).IsAssignableFrom(nextType))
						.ToList();

					FileLoggingHelper.LogInformation($"Found {submoduleProgramTypes.Count} {nameof(IMQ2Program)} types in the {submoduleProgramName} assembly: {string.Join(", ", submoduleProgramTypes.Select(type => type.FullName))}");

					var submoduleProgramClassType = submoduleProgramTypes
						.FirstOrDefault(nextType => nextType.Name.Contains(submoduleProgramName));

					if (submoduleProgramClassType == null && submoduleProgramTypes.Count > 0)
					{
						FileLoggingHelper.LogWarning($"Did not find a program type with name: {submoduleProgramName}, falling back to the first {nameof(IMQ2Program)} type found.");
						submoduleProgramClassType = submoduleProgramTypes.FirstOrDefault();
					}	

					if (submoduleProgramClassType == null)
					{
						throw new InvalidOperationException($"Failed to locate type {submoduleProgramName} within the assembly!");
					}

					if (!submoduleProgramClassType.IsClass
						|| submoduleProgramClassType.IsAbstract
						|| !typeof(IMQ2Program).IsAssignableFrom(submoduleProgramClassType))
					{
						throw new InvalidOperationException($"Submodule program type {submoduleProgramClassType.FullName} must be a non-abstract class that implements the {nameof(IMQ2Program)} interface!");
					}

					var submoduleProgramConstructor = submoduleProgramClassType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
						.FirstOrDefault(constructor => constructor.GetParameters().Length == 0);

					if (submoduleProgramConstructor == null)
					{
						throw new InvalidOperationException($"Submodule program type {submoduleProgramClassType.FullName} must have a public default constructor (a constructor that does not require parameters)!");
					}

					submoduleProgramInstance = (IMQ2Program)submoduleProgramConstructor.Invoke(null);

#pragma warning disable CA2000 // Dispose objects before losing scope
					cancellationTokenSource = new CancellationTokenSource();

					submoduleProgramTask = Task.Run(() => submoduleProgramInstance.RunAsync(commandArguments, cancellationTokenSource.Token));
					var wrapper = new SubmoduleProgramWrapper(assemblyLoadContext, cancellationTokenSource, submoduleProgramName, submoduleProgramInstance, submoduleProgramTask);

					_programsDictionary.Add(submoduleProgramName, wrapper);
#pragma warning restore CA2000 // Dispose objects before losing scope

					return true;
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"{nameof(SubmoduleRegistry)}.{nameof(StartProgram)}(..) encountered an unexpected exception while trying to start submodule program: {submoduleProgramName}\n\n{exc.ToString()}");

				CleanupHelper.TryDispose(cancellationTokenSource);

				if (submoduleProgramInstance is IDisposable disposableSubmoduleProgramInstance)
				{
					CleanupHelper.TryDispose(disposableSubmoduleProgramInstance);
				}

				// Do our best to cleanup if an exception is thrown
				CleanupHelper.TryUnload(assemblyLoadContext);

				return false;
			}
		}

		internal bool StopProgram(string submoduleProgramName)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			try
			{
				if (string.IsNullOrWhiteSpace(submoduleProgramName))
				{
					throw new ArgumentNullException(nameof(submoduleProgramName), "cannot be null, empty, or whitespace.");
				}

				if (!_programsDictionary.ContainsKey(submoduleProgramName))
				{
					FileLoggingHelper.LogInformation($"A submodule program instance is not currently loaded/running with the name: {submoduleProgramName}");
					return false;
				}

				lock (_lock)
				{
					if (!_programsDictionary.Remove(submoduleProgramName, out var submoduleProgramWrapper)
						|| submoduleProgramWrapper == null)
					{
						FileLoggingHelper.LogInformation($"A submodule program instance is not currently loaded/running with the name: {submoduleProgramName}");
						return false;
					}

					submoduleProgramWrapper.Dispose();

					return true;
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"{nameof(SubmoduleRegistry)}.{nameof(StartProgram)}(..) encountered an unexpected exception while trying to start submodule program: {submoduleProgramName}\n\n{exc.ToString()}");

				return false;
			}
		}

		private void UnloadAll()
		{
			if (_programsDictionary == null)
			{
				return;
			}

			foreach (var submoduleProgramWrapper in _programsDictionary.Values)
			{
				try
				{
					submoduleProgramWrapper.Dispose();
				}
				catch (Exception disposeException)
				{
					// Not going to try to log here since this happens in critical finalizer code
				}
			}

			_programsDictionary.Clear();
		}

		private class SubmoduleProgramWrapper : IDisposable
		{
			private bool _isDisposed = false;

			public SubmoduleProgramWrapper(
				AssemblyLoadContext assemblyLoadContext,
				CancellationTokenSource cancellationTokenSource,
				string name,
				IMQ2Program programInstance,
				Task task
			)
			{
				AssemblyLoadContext = assemblyLoadContext;
				CancellationTokenSource = cancellationTokenSource;
				Name = name;
				ProgramInstance = programInstance;
				Task = task;
			}

			public AssemblyLoadContext? AssemblyLoadContext { get; private set; }
			public CancellationTokenSource? CancellationTokenSource { get; private set; }
			public string Name { get; private set; }
			public IMQ2Program? ProgramInstance { get; private set; }
			public Task? Task { get; private set; }

			/// <inheritdoc />
			public void Dispose()
			{
				if (_isDisposed)
				{
					return;
				}

				try
				{
					CancellationTokenSource?.Cancel();
				}
				catch (Exception)
				{

				}

				CleanupHelper.TryDispose(CancellationTokenSource);
				CleanupHelper.TryDispose(Task);
				if (ProgramInstance is IDisposable disposableProgramInstance)
				{
					CleanupHelper.TryDispose(disposableProgramInstance);
				}

				CleanupHelper.TryUnload(AssemblyLoadContext);
				AssemblyLoadContext = null;

				_isDisposed = true;
			}
		}
	}
}
