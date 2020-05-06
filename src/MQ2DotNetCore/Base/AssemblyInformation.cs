using System;
using System.Diagnostics;
using System.IO;

namespace MQ2DotNetCore.Base
{
	public static class AssemblyInformation
	{
		public static readonly string AssemblyDirectory;
		public static readonly string AssemblyLocation;
		public static readonly FileVersionInfo FileVersionInfo;
		public static readonly Version Version;

		static AssemblyInformation()
		{
			var thisAssembly = typeof(AssemblyInformation).Assembly;
			AssemblyLocation = thisAssembly.Location;
			AssemblyDirectory = Directory.GetParent(AssemblyLocation).FullName;

			FileVersionInfo = FileVersionInfo.GetVersionInfo(AssemblyLocation);

			Version = thisAssembly.GetName().Version ?? new Version();
		}
	}
}
