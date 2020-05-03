using System;
using System.Diagnostics;

namespace RhinoBot.Base
{
	public static class RhinoBotConstants
	{
		public static readonly FileVersionInfo AssemblyFileVersionInfo;
		public static readonly string AssemblyLocation;
		public static readonly Version? AssemblyVersion;

		static RhinoBotConstants()
		{
			var thisAssembly = typeof(RhinoBotConstants).Assembly;
			AssemblyLocation = thisAssembly.Location;

			AssemblyFileVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyLocation);

			var assemblyName = thisAssembly.GetName();
			AssemblyVersion = assemblyName.Version;
		}
	}
}
