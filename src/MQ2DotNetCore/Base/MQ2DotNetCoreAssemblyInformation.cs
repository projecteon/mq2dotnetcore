using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MQ2DotNetCore.Base
{
	public static class MQ2DotNetCoreAssemblyInformation
	{
		public static readonly string AssemblyDirectory;
		public static readonly string AssemblyLocation;
		public static readonly AssemblyName AssemblyName;
		public static readonly FileVersionInfo FileVersionInfo;
		public static readonly Version Version;
		public static readonly Assembly MQ2DotNetCoreAssembly;

		static MQ2DotNetCoreAssemblyInformation()
		{
			MQ2DotNetCoreAssembly = typeof(MQ2DotNetCoreAssemblyInformation).Assembly;
			AssemblyLocation = MQ2DotNetCoreAssembly.Location;
			AssemblyDirectory = Directory.GetParent(AssemblyLocation).FullName;

			FileVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyLocation);

			AssemblyName = MQ2DotNetCoreAssembly.GetName();
			Version = AssemblyName.Version ?? new Version();
		}
	}
}
