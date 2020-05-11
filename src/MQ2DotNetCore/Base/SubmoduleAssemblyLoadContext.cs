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

			if (assemblyName?.Name == "MQ2DotNetCore")
			{
				var assemblyNamesInDefaultLoadContext = AssemblyLoadContext.Default.Assemblies.Select(assembly => assembly.FullName);
				FileLoggingHelper.LogDebug($"[{Name}]  MQ2DotNetCore is being requested. Assemblies in default load context:\n\n\t{string.Join(",\n\t", assemblyNamesInDefaultLoadContext)}\n\n");

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

			FileLoggingHelper.LogDebug($"[{Name}]  Failed to locate or load assembly: {assemblyName}");

			// If it didn't load here it should fallback to using the assembly from the AssemblyLoadContext.Default.Assemblies (if present). If not
			// present in the default load context a type load exception will be thrown...

			return null;
		}
	}
}
