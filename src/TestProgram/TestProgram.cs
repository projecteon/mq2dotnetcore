using MQ2DotNetCore;
using MQ2DotNetCore.Logging;
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
			try
			{
				if (mq2Dependencies == null)
				{
					throw new ArgumentNullException(nameof(mq2Dependencies));
				}

				mq2Dependencies.GetCommandRegistry().AddAsyncCommand("/testcommandasync", TestCommandAsync);

				while (cancellationToken != null && !cancellationToken.IsCancellationRequested)
				{
					try
					{
						await Task.Delay(5000, cancellationToken);
					}
					catch (TaskCanceledException)
					{
#if DEBUG
						FileLoggingHelper.LogTrace("Task has been cancelled");
#endif

						break;
					}
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
				mq2Dependencies?.GetMQ2().WriteChatSafe($"{nameof(TestProgram)}.{nameof(RunAsync)} threw an exception: {exc}");
			}
		}

		public static async Task TestCommandAsync(string[] commandArguments, CancellationToken cancellationToken)
		{
			int? randomId = null;
			try
			{
				randomId = new Random().Next();

				MQ2DotNetCore.Logging.FileLoggingHelper
					.LogInformation($"Executing! [RandomId: {randomId}] [CommandArguments: {string.Join(", ", commandArguments)}]");

				// 100 loops == ~20 seconds
				for (var loopIndex = 0; loopIndex < 1500; ++loopIndex)
				{
					await Task.Delay(200, cancellationToken);

					// These should all log the same managed thread id
					if (loopIndex % 100 == 0)
					{
						MQ2DotNetCore.Logging.FileLoggingHelper
							.LogInformation($"TestCommandAsync(..) [LoopIndex1: {loopIndex}] [RandomId: {randomId}] [CommandArguments: {string.Join(", ", commandArguments)}]");
					}
				}
			}
			catch (TaskCanceledException)
			{
				FileLoggingHelper.LogDebug($"{nameof(TestCommandAsync)} has been cancelled! [RandomId: {randomId}] [CommandArguments: {string.Join(", ", commandArguments)}]");
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}
	}
}
