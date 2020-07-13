using Microsoft.Extensions.Logging;
using MQ2DotNetCore.Base;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MQ2DotNetCore.Logging
{
	public static class ILoggerExtensions
	{
		private static readonly Action<ILogger, DateTime, int, string, string, Exception?> _logCriticalPrefixed =
			LoggerMessage.Define<DateTime, int, string, string>(
				LogLevel.Critical,
				LoggingHelper.DefaultEventId,
				"[{timestamp} ({managedThreadId}] (Critical)  {callSite}]  {message}"
			);

		public static void LogCriticalPrefixed(this ILogger logger, string message, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logCriticalPrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, message, null);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}

		public static void LogCriticalPrefixed(this ILogger logger, Exception exceptionToLog, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logCriticalPrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, $"An exception occurred:\n\n{exceptionToLog}\n", exceptionToLog);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}

		public static void LogCriticalPrefixed(this ILogger logger, string message, Exception exceptionToLog, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logCriticalPrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, message, exceptionToLog);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}

		private static readonly Action<ILogger, DateTime, int, string, string, Exception?> _logDebugPrefixed =
			LoggerMessage.Define<DateTime, int, string, string>(
				LogLevel.Debug,
				LoggingHelper.DefaultEventId,
				"[{timestamp} ({managedThreadId}] (Debug)  {callSite}]  {message}"
			);

		public static void LogDebugPrefixed(this ILogger logger, string message, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logDebugPrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, message, null);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}

		private static readonly Action<ILogger, DateTime, int, string, string, Exception?> _logErrorPrefixed =
			LoggerMessage.Define<DateTime, int, string, string>(
				LogLevel.Error,
				LoggingHelper.DefaultEventId,
				"[{timestamp} ({managedThreadId}] (Error)  {callSite}]  {message}"
			);

		public static void LogErrorPrefixed(this ILogger logger, string message, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logErrorPrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, message, null);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}

		public static void LogErrorPrefixed(this ILogger logger, Exception exceptionToLog, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logErrorPrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, $"An exception occurred:\n\n{exceptionToLog}\n", exceptionToLog);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}

		public static void LogErrorPrefixed(this ILogger logger, string message, Exception exceptionToLog, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logErrorPrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, message, exceptionToLog);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}

		private static readonly Action<ILogger, DateTime, int, string, string, Exception?> _logInformationPrefixed =
			LoggerMessage.Define<DateTime, int, string, string>(
				LogLevel.Information,
				LoggingHelper.DefaultEventId,
				"[{timestamp} ({managedThreadId}] (Information)  {callSite}]  {message}"
			);

		public static void LogInformationPrefixed(this ILogger logger, string message, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logInformationPrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, message, null);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}

		private static readonly Action<ILogger, DateTime, int, string, string, Exception?> _logTracePrefixed =
			LoggerMessage.Define<DateTime, int, string, string>(
				LogLevel.Trace,
				LoggingHelper.DefaultEventId,
				"[{timestamp} ({managedThreadId}] (Trace)  {callSite}]  {message}"
			);

		public static void LogTracePrefixed(this ILogger logger, string message, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logTracePrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, message, null);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}

		private static readonly Action<ILogger, DateTime, int, string, string, Exception?> _logWarningPrefixed =
			LoggerMessage.Define<DateTime, int, string, string>(
				LogLevel.Warning,
				LoggingHelper.DefaultEventId,
				"[{timestamp} ({managedThreadId}] (Warning)  {callSite}]  {message}"
			);

		public static void LogWarningPrefixed(this ILogger logger, string message, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logWarningPrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, message, null);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}

		public static void LogWarningPrefixed(this ILogger logger, Exception exceptionToLog, [CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		{
			try
			{
				var callSite = StringHelper.GetCallSiteString(callerFilePath, callerMemberName);
				_logWarningPrefixed(logger, DateTime.Now, Thread.CurrentThread.ManagedThreadId, callSite, $"An exception occurred:\n\n{exceptionToLog}\n", exceptionToLog);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
				// TODO: Anything ?
			}
		}
	}
}
