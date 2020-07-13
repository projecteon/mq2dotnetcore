using Microsoft.Extensions.Logging;
using MQ2DotNetCore.Interop;
using System;
using System.Linq;
using System.Text;

namespace MQ2DotNetCore.Logging
{
	public class MQ2Logger : ILogger
	{
		private static readonly char[] NEWLINE_CHARACTERS = new[] { '\r', '\n' };

		private readonly string _categoryName;
		private readonly MQ2LoggerProvider _provider;

		public MQ2Logger(
			string categoryName,
			MQ2LoggerProvider provider
		)
		{
			_categoryName = categoryName;
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		public IDisposable? BeginScope<TState>(TState state)
		{
			// TODO: Return a NullScope object in lieu of an actual null object ?
			return _provider.ScopeProvider?.Push(state);
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			// The aggregate logger should handle filter switching
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			try
			{
				var logLevelColor = GetLogLevelTextColor(logLevel);
				var isLogLevelColorWhite = logLevelColor == "\aw";

				var builder = new StringBuilder();
				builder.Append(logLevelColor);

				var logLevelSubstring = $"({logLevel}) ";
				builder.Append(logLevelSubstring);

				if (!isLogLevelColorWhite)
				{
					builder.Append("\aw");
				}

				builder.Append("(Category: ").Append(_categoryName).Append(") ");

				var scopeProvider = _provider.ScopeProvider;
				if (scopeProvider != null)
				{
					scopeProvider.ForEachScope((scope, stringBuilder) => stringBuilder.Append(" => ").Append(scope), builder);

					builder.AppendLine(":");
				}
				else
				{
					builder.Append(": ");
				}

				var coreMessage = formatter?.Invoke(state, exception)
					.Replace(logLevelSubstring, string.Empty, StringComparison.Ordinal)
					.Replace("\\", "\\\\");

				if (coreMessage != null)
				{
					builder.AppendLine(coreMessage);
				}

				if (exception != null)
				{
					builder.AppendLine(exception.ToString());
				}

				var message = builder.ToString();

				var startIndex = 0;
				string nextSubstring;
				string? currentColor = null;
				while (startIndex < message.Length)
				{
					if (NEWLINE_CHARACTERS.Contains(message[startIndex]))
					{
						++startIndex;
						continue;
					}

					var newlineIndex = message.IndexOfAny(NEWLINE_CHARACTERS, startIndex);
					var substringLength = newlineIndex > -1
						? Math.Min(newlineIndex - startIndex, 2040)
						: Math.Min(message.Length - startIndex, 2040);

					if (substringLength < 1)
					{
						++startIndex;
						continue;
					}

					nextSubstring = currentColor != null
						? currentColor + message.Substring(startIndex, substringLength)
						: message.Substring(startIndex, substringLength);

					MQ2Main.NativeMethods.MQ2WriteChatfSafe(nextSubstring);

					startIndex += substringLength;
					if (startIndex < message.Length)
					{
						var colorIndex = nextSubstring.LastIndexOf('\a');
						if (colorIndex > -1)
						{
							currentColor = nextSubstring.Substring(colorIndex, 2);
						}
					}
				}
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
#pragma warning disable CS0168 // Variable is declared but never used
			catch (Exception exc)
#pragma warning restore CS0168 // Variable is declared but never used
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			{
#if DEBUG
				Console.Write(exc);
#endif
			}
		}

		public static string GetLogLevelTextColor(LogLevel logLevel)
		{
			switch (logLevel)
			{
				case LogLevel.Critical:
				case LogLevel.Error:
					return "\ar";

				case LogLevel.Warning:
					return "\ay";

				case LogLevel.Information:
					return "\ag";

				case LogLevel.Debug:
					return "\ab";

				case LogLevel.Trace:
				default:
					return "\aw";
			}
		}
	}
}
