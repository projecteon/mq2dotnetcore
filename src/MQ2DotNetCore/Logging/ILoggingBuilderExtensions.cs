using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace MQ2DotNetCore.Logging
{
	public static class ILoggingBuilderExtensions
	{
		public static ILoggingBuilder AddMQ2(this ILoggingBuilder loggingBuilder)
		{
			if (loggingBuilder == null)
			{
				throw new ArgumentNullException(nameof(loggingBuilder));
			}

			loggingBuilder.Services.AddSingleton<ILoggerProvider, MQ2LoggerProvider>();

			return loggingBuilder;
		}
	}
}
