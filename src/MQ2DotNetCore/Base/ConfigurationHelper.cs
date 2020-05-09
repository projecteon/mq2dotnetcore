using Newtonsoft.Json;
using System.IO;

namespace MQ2DotNetCore.Base
{
	public static class ConfigurationHelper
	{
		private static MQ2DotNetCoreConfiguration? _configuration;
		private static object _lock = new object();

		public static MQ2DotNetCoreConfiguration? GetConfiguration()
		{
			// TODO: Add a FileWatchHandler to detect and changes and automatically update the config?

			if (_configuration != null)
			{
				return _configuration;
			}

			lock (_lock)
			{
				if (_configuration != null)
				{
					return _configuration;
				}

				var configurationPath = Path.Combine(AssemblyInformation.AssemblyDirectory, "appsettings.json");
				if (!File.Exists(configurationPath))
				{
					return null;
				}

				var configurationFileContent = File.ReadAllText(configurationPath);
				_configuration = JsonConvert.DeserializeObject<MQ2DotNetCoreConfiguration>(configurationFileContent);
			}

			return _configuration;
		}
	}
}
