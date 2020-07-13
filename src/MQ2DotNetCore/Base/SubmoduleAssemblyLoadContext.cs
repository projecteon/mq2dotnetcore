using Microsoft.Extensions.Logging;
using MQ2DotNetCore.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace MQ2DotNetCore.Base
{
	public class SubmoduleAssemblyLoadContext : AssemblyLoadContext
	{
		internal static readonly Assembly[] MQ2DotNetCoreDependencies = new Assembly[]
		{
			typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly,							// Microsoft.Extensions.Configuration.Abstractions
			typeof(Microsoft.Extensions.Configuration.ConfigurationBinder).Assembly,					// Microsoft.Extensions.Configuration.Binder
			typeof(Microsoft.Extensions.Configuration.ConfigurationBuilder).Assembly,					// Microsoft.Extensions.Configuration
			typeof(Microsoft.Extensions.Configuration.FileConfigurationExtensions).Assembly,			// Microsoft.Extensions.Configuration.FileExtensions
			typeof(Microsoft.Extensions.Configuration.JsonConfigurationExtensions).Assembly,			// Microsoft.Extensions.Configuration.Json
			typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly,				// Microsoft.Extensions.DependencyInjection.Abstractions
			typeof(Microsoft.Extensions.DependencyInjection.ServiceProvider).Assembly,					// Microsoft.Extensions.DependencyInjection
			typeof(Microsoft.Extensions.FileProviders.IFileProvider).Assembly,							// Microsoft.Extensions.FileProviders.Abstractions
			typeof(Microsoft.Extensions.FileProviders.PhysicalFileProvider).Assembly,					// Microsoft.Extensions.FileProviders.Physical
			typeof(Microsoft.Extensions.FileSystemGlobbing.Matcher).Assembly,							// Microsoft.Extensions.FileSystemGlobbing
			typeof(Microsoft.Extensions.Logging.ILoggerFactory).Assembly,								// Microsoft.Extensions.Logging.Abstractions
			typeof(Microsoft.Extensions.Logging.LoggerFactory).Assembly,								// Microsoft.Extensions.Logging
			typeof(Microsoft.Extensions.Logging.Configuration.LoggerProviderOptions).Assembly,			// Microsoft.Extensions.Logging.Configuration
			typeof(Microsoft.Extensions.Logging.ConsoleLoggerExtensions).Assembly,						// Microsoft.Extensions.Logging.Console
			typeof(Microsoft.Extensions.Logging.DebugLoggerFactoryExtensions).Assembly,					// Microsoft.Extensions.Logging.Debug
			typeof(Microsoft.Extensions.Options.ConfigurationChangeTokenSource<>).Assembly,				// Microsoft.Extensions.Options.ConfigurationExtensions
			typeof(Microsoft.Extensions.Options.IOptions<>).Assembly,									// Microsoft.Extensions.Options
			typeof(Microsoft.Extensions.Primitives.IChangeToken).Assembly,								// Microsoft.Extensions.Primitives
			typeof(Microsoft.Extensions.Logging.FileLoggerFactoryExtensions).Assembly,					// NetEscapades.Extensions.Logging.RollingFile
			typeof(Newtonsoft.Json.JsonConverter).Assembly												// Newtonsoft.Json
		};

		private readonly ILogger<SubmoduleAssemblyLoadContext>? _logger;
		private readonly string? _primaryAssemblyDirectory;
		private readonly string _primaryAssemblyPath;

		public SubmoduleAssemblyLoadContext(
			ILogger<SubmoduleAssemblyLoadContext>? logger,
			string name,
			string primaryAssemblyPath
		)
			: base(name, true)
		{
			_logger = logger;
			_primaryAssemblyPath = primaryAssemblyPath;
			_primaryAssemblyDirectory = Directory.GetParent(primaryAssemblyPath)?.FullName;
		}

		protected override Assembly? Load(AssemblyName assemblyName)
		{
			if (assemblyName == null)
			{
				throw new ArgumentNullException(nameof(assemblyName));
			}

			_logger?.LogDebugPrefixed($"[{Name}]  {nameof(Load)}(..) is being called for assembly name: {assemblyName}");

			Exception? baseLoadException = null;
			try
			{
				var loadedAssembly = base.Load(assemblyName);

				if (loadedAssembly != null)
				{
					_logger?.LogDebugPrefixed($"[{Name}]  base.{nameof(Load)}(..) successfully loaded assembly: {assemblyName}");
					return loadedAssembly;
				}

				_logger?.LogDebugPrefixed($"[{Name}]  base.{nameof(Load)}(..) returned null for assembly: {assemblyName}");
			}
			catch (Exception exc)
			{
				_logger?.LogWarningPrefixed($"[{Name}]  base.{nameof(Load)}(..) encountered an exception attempting to load the assembly: {assemblyName}\n\n{exc}\n");
				baseLoadException = exc;
			}

			if (AssemblyLoadContext.Default.Assemblies.Any(sharedAssembly => sharedAssembly.FullName == assemblyName.FullName))
			{
				_logger?.LogDebugPrefixed($"[{Name}]  Requested assembly name ({assemblyName}) matches an assembly in the default assembly load context.");

				// Ok to return null here it will get supplied from the AssemblyLoadContext.Default.Assemblies...
				return null;
			}

			if (assemblyName.Name == "MQ2DotNetCore")
			{
				var assemblyNamesInDefaultLoadContext = AssemblyLoadContext.Default.Assemblies.Select(assembly => assembly.FullName);

#if DEBUG
				_logger?.LogTracePrefixed($"[{Name}]  MQ2DotNetCore is being requested. Assemblies in default load context:\n\n\t{string.Join(",\n\t", assemblyNamesInDefaultLoadContext)}\n\n");
#endif

				var mq2DotNetCoreAssembly = AssemblyLoadContext.Default.Assemblies
					.FirstOrDefault(assembly => assembly.FullName?.Contains("MQ2DotNetCore", StringComparison.Ordinal) == true);

				if (mq2DotNetCoreAssembly != null)
				{
					_logger?.LogDebugPrefixed($"[{Name}]  MQ2DotNetCore was located in AssemblyLoadContext.Default");
					return mq2DotNetCoreAssembly;
				}

				mq2DotNetCoreAssembly = typeof(SubmoduleAssemblyLoadContext).Assembly;
				if (mq2DotNetCoreAssembly != null)
				{
					// shows up as dynamically loaded assembly, we'll need to load the MQ2DotNetCore assemblies ourselves
					TryLoadMQ2DotNetCoreDependencies();

					_logger?.LogDebugPrefixed($"[{Name}]  Returning the value of typeof(SubmoduleAssemblyLoadContext).Assembly for the MQ2DotNetCore assembly.  [MQ2DotNetCore AssemblyName: {mq2DotNetCoreAssembly.GetName()}] [MQ2DotNetCore Assembly Location: {TryGetAssemblyLocation(mq2DotNetCoreAssembly)}]");
					return mq2DotNetCoreAssembly;
				}
			}

			var mq2DotNetCoreDependency = Array.Find(MQ2DotNetCoreDependencies, dependency => dependency.GetName().FullName == assemblyName.FullName);
			if (mq2DotNetCoreDependency != null)
			{
				_logger?.LogDebugPrefixed($"Returning MQ2DotNetCoreDependencies instance for assembly name: {assemblyName}");
				return mq2DotNetCoreDependency;
			}

			if (!string.IsNullOrEmpty(_primaryAssemblyDirectory) && !string.IsNullOrEmpty(assemblyName?.Name))
			{
				var targetAssemblyPath = Path.Combine(_primaryAssemblyDirectory, $"{assemblyName.Name}.dll");
				if (File.Exists(targetAssemblyPath))
				{
					_logger?.LogDebugPrefixed($"[{Name}]  Attempting to load assembly from path: {targetAssemblyPath}");
					var targetAssembly = LoadFromAssemblyPath(targetAssemblyPath);
					if (targetAssembly != null)
					{
						return targetAssembly;
					}
				}
			}


			try
			{
				// Throw an exception so we can get a stack trace for where the load was triggered
				throw new InvalidOperationException();
			}
			catch (InvalidOperationException failedToLoadException)
			{
				_logger?.LogWarningPrefixed($"[{Name}]  Failed to locate or load assembly: {assemblyName}\n\n{failedToLoadException.StackTrace}\n");
			}

			return null;
		}

		internal string TryGetAssemblyLocation(Assembly assembly)
		{
			try
			{
				return assembly.Location;
			}
#pragma warning disable CS0168 // Variable is declared but never used
			catch (Exception exc)
#pragma warning restore CS0168 // Variable is declared but never used
			{
#if DEBUG
				_logger?.LogTracePrefixed(exc);
#endif

				return string.Empty;
			}
		}

		internal void TryLoadMQ2DotNetCoreDependencies()
		{
			foreach (var nextDependency in MQ2DotNetCoreDependencies)
			{
				try
				{
					LoadFromAssemblyName(nextDependency.GetName());
				}
				catch (Exception exc)
				{
					_logger?.LogWarningPrefixed(exc);
				}
			}
		}
	}
}
