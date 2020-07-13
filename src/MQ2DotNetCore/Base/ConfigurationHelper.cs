using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.Base
{
	public static class ConfigurationHelper
	{
		private static IConfiguration? _configuration;
		private static readonly object _lock = new object();
		private static readonly string _logFilePath = Path.Combine(MQ2DotNetCoreAssemblyInformation.AssemblyDirectory, "debug_entry_point.log");

		public static ILoggerFactory? CreateLoggerFactory(IConfiguration configuration, MQ2DotNetCoreOptions mq2DotNetCoreOptions)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}

			if (mq2DotNetCoreOptions == null)
			{
				throw new ArgumentNullException(nameof(mq2DotNetCoreOptions));
			}

			var isAnyLoggingProviderEnabled =
				mq2DotNetCoreOptions.IsConsoleLoggingEnabled
				|| mq2DotNetCoreOptions.IsDebugLoggingEnabled
				|| mq2DotNetCoreOptions.IsFileLoggingEnabled;
			//|| mq2DotNetCoreOptions.IsMQ2LoggingEnabled;

			ILoggerFactory? loggerFactory = null;
			if (isAnyLoggingProviderEnabled)
			{
				loggerFactory = LoggerFactory.Create(loggingBuilder =>
				{
					loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));

					if (mq2DotNetCoreOptions.IsConsoleLoggingEnabled)
					{
						loggingBuilder.AddConsole();
					}

					if (mq2DotNetCoreOptions.IsDebugLoggingEnabled)
					{
						loggingBuilder.AddDebug();
					}

					if (mq2DotNetCoreOptions.IsFileLoggingEnabled)
					{
						loggingBuilder.AddFile(fileOptions =>
						{
							fileOptions.Extension = ".log";
							fileOptions.FileName = "debug-entry-point-";
							fileOptions.FileSizeLimit = 20 * 1024 * 1024;
							fileOptions.LogDirectory = Path.Combine(MQ2DotNetCoreAssemblyInformation.AssemblyDirectory, "logs");
							fileOptions.RetainedFileCountLimit = null;
						});
					}

					if (mq2DotNetCoreOptions.IsMQ2LoggingEnabled)
					{
						// TODO: Create an MQ2 logging provider that writes to the MQ2 chat window
					}
				});
			}

			return loggerFactory;
		}

		public static IConfiguration GetConfiguration()
		{
			try
			{
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

					_configuration = new ConfigurationBuilder()
						.SetBasePath(MQ2DotNetCoreAssemblyInformation.AssemblyDirectory)
						.AddJsonFile("MQ2DotNetCore.appsettings.json", optional: false, reloadOnChange: true)
						.Build();
				}

				return _configuration;
			}
			catch (Exception loadConfigurationException)
			{
				TryAppendToFile($"{PrefixLogEntry(LogLevel.Critical, "ConfigurationHelper.cs", "GetConfiguration")}  An exception occurred:\n\n{loadConfigurationException}\n");
				throw;
			}
		}

		private static string PrefixLogEntry(LogLevel logLevel, string? callerFilePath, string? callerMemberName)
		{
			var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
			return $"[{DateTime.Now} ({Thread.CurrentThread.ManagedThreadId}] ({logLevel})  {callSite}]";
		}

		// Fallback logging in case loading the config / configuring the logger factory throws
		private static void TryAppendToFile(string message)
		{
			try
			{
				// Forcing these onto a thread pool thread using Task.Run so we're not blocking the EQ/MQ2 thread with
				// file I/O, and so that we can use File.AppendAllTextAsync(..) in lieu of AppendAllText(..)
				// Could probably push these into a queue and have a continuous background handler process them if we
				// want to be fancy
				Task.Run(async () =>
				{
					var writeAttempts = 0;
					while (writeAttempts < 3)
					{
						try
						{
							++writeAttempts;
							await File.AppendAllTextAsync(_logFilePath, message);
							break;
						}
						catch (Exception writeException)
						{
							try
							{
								Console.WriteLine(writeException);
							}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
							catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
							{
							}
						}
					}
				});
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
			}
		}
	}
}
