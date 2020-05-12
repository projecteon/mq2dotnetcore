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
		private readonly string? _primaryAssemblyDirectory;
		private readonly string _primaryAssemblyPath;

		public SubmoduleAssemblyLoadContext(string name, string primaryAssemblyPath)
			: base(name, true)
		{
			_primaryAssemblyPath = primaryAssemblyPath;
			_primaryAssemblyDirectory = Directory.GetParent(primaryAssemblyPath)?.FullName;
		}

		protected override Assembly? Load(AssemblyName assemblyName)
		{
			FileLoggingHelper.LogDebug($"[{Name}]  {nameof(Load)}(..) is being called for assembly name: {assemblyName}");

			Exception? baseLoadException = null;
			try
			{
				var loadedAssembly = base.Load(assemblyName);

				if (loadedAssembly != null)
				{
					FileLoggingHelper.LogDebug($"[{Name}]  base.{nameof(Load)}(..) successfully loaded assembly: {assemblyName}");
					return loadedAssembly;
				}

				FileLoggingHelper.LogDebug($"[{Name}]  base.{nameof(Load)}(..) returned null for assembly: {assemblyName}");
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogWarning($"[{Name}]  base.{nameof(Load)}(..) encountered an exception attempting to load the assembly: {assemblyName}\n\n{exc}\n");
				baseLoadException = exc;
			}

			if (AssemblyLoadContext.Default.Assemblies.Any(sharedAssembly => sharedAssembly.FullName == assemblyName.FullName))
			{
#if DEBUG
				FileLoggingHelper.LogTrace($"[{Name}]  Requested assembly name ({assemblyName}) matches an assembly in the default assembly load context.");
#endif

				// Ok to return null here it will get supplied from the AssemblyLoadContext.Default.Assemblies...
				return null;
			}

			if (assemblyName?.Name == "MQ2DotNetCore")
			{
				var assemblyNamesInDefaultLoadContext = AssemblyLoadContext.Default.Assemblies.Select(assembly => assembly.FullName);

#if DEBUG
				FileLoggingHelper.LogTrace($"[{Name}]  MQ2DotNetCore is being requested. Assemblies in default load context:\n\n\t{string.Join(",\n\t", assemblyNamesInDefaultLoadContext)}\n\n");
#endif

				var mq2DotNetCoreAssembly = AssemblyLoadContext.Default.Assemblies
					.FirstOrDefault(assembly => assembly.FullName?.Contains("MQ2DotNetCore") == true);

				if (mq2DotNetCoreAssembly != null)
				{
					FileLoggingHelper.LogDebug($"[{Name}]  MQ2DotNetCore was located in AssemblyLoadContext.Default");
					return mq2DotNetCoreAssembly;
				}

				mq2DotNetCoreAssembly = typeof(SubmoduleAssemblyLoadContext).Assembly;
				if (mq2DotNetCoreAssembly != null)
				{
					FileLoggingHelper.LogDebug($"[{Name}]  Returning the value of typeof(SubmoduleAssemblyLoadContext).Assembly for the MQ2DotNetCore assembly.");
					return mq2DotNetCoreAssembly;
				}
			}

			if (!string.IsNullOrEmpty(_primaryAssemblyDirectory) && !string.IsNullOrEmpty(assemblyName?.Name))
			{
				var targetAssemblyPath = Path.Combine(_primaryAssemblyDirectory, $"{assemblyName.Name}.dll");
				if (File.Exists(targetAssemblyPath))
				{
					FileLoggingHelper.LogDebug($"[{Name}]  Attempting to load assembly from path: {targetAssemblyPath}");
					var targetAssembly = LoadFromAssemblyPath(targetAssemblyPath);
					if (targetAssembly != null)
					{
						return targetAssembly;
					}
				}
			}

			FileLoggingHelper.LogWarning($"[{Name}]  Failed to locate or load assembly: {assemblyName}");
			return null;
		}
	}
}
