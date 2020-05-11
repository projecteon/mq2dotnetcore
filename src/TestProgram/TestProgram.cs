using MQ2DotNetCore;
using MQ2DotNetCore.MQ2Api;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestProgram
{
	public class TestProgram : IMQ2Program
	{
		public async Task RunAsync(string[] commandArguments, MQ2Dependencies mq2Dependencies, CancellationToken cancellationToken)
		{
			var randomId = new Random().Next();

			MQ2DotNetCore.Logging.FileLoggingHelper
				.LogInformation($"Executing! [RandomId: {randomId}] [CommandArguments: {string.Join(", ", commandArguments)}]");

			// 100 loops == ~20 seconds
			for (var loopIndex = 0; loopIndex < 1500; ++loopIndex)
			{
				await Task.Delay(200);

				// These should all log the same managed thread id
				MQ2DotNetCore.Logging.FileLoggingHelper
					.LogInformation($"Delayed for 200ms. [RandomId: {randomId}] [CommandArguments: {string.Join(", ", commandArguments)}]");
			}

			for (var loopIndex2 = 0; loopIndex2 < 100; ++loopIndex2)
			{
				await Task.Delay(200).ConfigureAwait(false);

				// ConfigureAwait(false) will probably mean these can run the continuation on thread pool threads
				MQ2DotNetCore.Logging.FileLoggingHelper
					.LogInformation($"Delayed for 200ms with ConfigureAwait(false). [RandomId: {randomId}] [CommandArguments: {string.Join(", ", commandArguments)}]");
			}

		}
	}
}
