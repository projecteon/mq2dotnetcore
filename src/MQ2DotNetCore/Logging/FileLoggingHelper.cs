using MQ2DotNetCore.Base;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MQ2DotNetCore.Logging
{
	/// <summary>
	/// Don't want to setup a whole bunch of complex dependencies in the loader entry point assembly so we'll just
	/// use a simple static file logger and mimic the standard extenion methods of ILogger
	/// </summary>
	public static class FileLoggingHelper
	{
		private static readonly string _logFilePath = Path.Combine(AssemblyInformation.AssemblyDirectory, "debug_entry_point.log");

		public static LogLevel GetLogLevel()
		{
			try
			{
				return ConfigurationHelper.GetConfiguration()?.LogLevel ?? LogLevel.Information;
			}
			catch (Exception exc)
			{
				try
				{
					File.AppendAllText(Path.Combine(AssemblyInformation.AssemblyDirectory, "debug_entry_point.log"), $"[{DateTime.Now} {nameof(GetLogLevel)}()]  {exc.ToString()}");
				}
				catch (Exception)
				{

				}
			}

			return LogLevel.Information;
		}

		public static void LogCritical(string message, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				if (GetLogLevel() < LogLevel.Critical)
				{
					return;
				}

				TryAppendToFile($"{PrefixLogEntry(LogLevel.Critical, callerMemberName)}  {message}\n");
			}
			catch (Exception exc)
			{
				// TODO: Anything ?
			}
		}

		public static void LogDebug(string message, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				if (GetLogLevel() < LogLevel.Debug)
				{
					return;
				}

				TryAppendToFile($"{PrefixLogEntry(LogLevel.Debug, callerMemberName)}  {message}\n");
			}
			catch (Exception exc)
			{
				// TODO: Anything ?
			}
		}

		public static void LogError(string message, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				if (GetLogLevel() < LogLevel.Error)
				{
					return;
				}

				TryAppendToFile($"{PrefixLogEntry(LogLevel.Error, callerMemberName)}  {message}\n");
			}
			catch (Exception exc)
			{
				// TODO: Anything ?
			}
		}

		public static void LogInformation(string message, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				if (GetLogLevel() < LogLevel.Information)
				{
					return;
				}

				TryAppendToFile($"{PrefixLogEntry(LogLevel.Information, callerMemberName)}  {message}\n");
			}
			catch (Exception exc)
			{
				// TODO: Anything ?
			}
		}

		public static void LogTrace(string message, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				if (GetLogLevel() < LogLevel.Trace)
				{
					return;
				}

				TryAppendToFile($"{PrefixLogEntry(LogLevel.Trace, callerMemberName)}  {message}\n");
			}
			catch (Exception exc)
			{
				// TODO: Anything ?
			}
		}

		public static void LogWarning(string message, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				if (GetLogLevel() < LogLevel.Warning)
				{
					return;
				}

				TryAppendToFile($"{PrefixLogEntry(LogLevel.Warning, callerMemberName)}  {message}\n");
			}
			catch (Exception exc)
			{
				// TODO: Anything ?
			}
		}

		public static string PrefixLogEntry(LogLevel logLevel, string? callerMemberName)
		{
			return $"[{DateTime.Now} ({Thread.CurrentThread.ManagedThreadId}] ({logLevel.ToString()})  {callerMemberName}]";
		}

		private static bool TryAppendToFile(string message)
		{
			try
			{
				var writeAttempts = 0;
				while (writeAttempts < 3)
				{
					try
					{
						++writeAttempts;
						File.AppendAllText(_logFilePath, message);
						return true;
					}
					catch (Exception writeException)
					{
						try
						{
							Console.WriteLine(writeException);
						}
						catch (Exception)
						{
						}
					}
				}
			}
			catch(Exception)
			{
			}

			return false;
		}
	}
}
