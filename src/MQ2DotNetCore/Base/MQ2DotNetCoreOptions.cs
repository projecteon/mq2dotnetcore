using Microsoft.Extensions.Configuration;
using System;

namespace MQ2DotNetCore.Base
{
	public class MQ2DotNetCoreOptions : ReloadableOptionsBase
	{
		public MQ2DotNetCoreOptions(IConfiguration configuration) : base(configuration)
		{
		}

		public Version? FileVersion { get; set; }
		public bool IsConsoleLoggingEnabled { get; set; }
		public bool IsDebugLoggingEnabled { get; set; }
		public bool IsFileLoggingEnabled { get; set; }
		public bool IsMQ2LoggingEnabled { get; set; }
	}
}
