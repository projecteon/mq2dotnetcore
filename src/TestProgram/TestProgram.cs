using MQ2DotNetCore;
using MQ2DotNetCore.Logging;
using MQ2DotNetCore.MQ2Api;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TestProgram
{
	public class TestProgram : IMQ2Program
	{
		private MQ2Dependencies _mq2Dependencies = null!; // nullability hack, will always be initialized in RunAsync

		public async Task RunAsync(string[] commandArguments, MQ2Dependencies mq2Dependencies, CancellationToken cancellationToken)
		{
			try
			{
				if (mq2Dependencies == null)
				{
					throw new ArgumentNullException(nameof(mq2Dependencies));
				}

				_mq2Dependencies = mq2Dependencies;

				mq2Dependencies.GetCommandRegistry().AddAsyncCommand("/testcommandasync", TestCommandAsync);
				mq2Dependencies.GetCommandRegistry().AddAsyncCommand("/testapis", TestApisCommandAsync);

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

		public async Task TestApisCommandAsync(string[] commandArguments, CancellationToken cancellationToken)
		{
			try
			{
				FileLoggingHelper.LogInformation("Testing MQ2 Api native calls");
				var mq2 = _mq2Dependencies.GetMQ2();
				mq2.WriteChatSafe("Testing MQ2 Api native calls");

				FileLoggingHelper.LogInformation("Testing GetTlo().Me?.Name}");
				var playerName = _mq2Dependencies.GetTlo().Me?.Name;

				var message = $"{{Me.Name}}: {playerName}";
				FileLoggingHelper.LogInformation(message);
				mq2.WriteChatSafe(message);

				await Task.Delay(500, cancellationToken);

				FileLoggingHelper.LogInformation("Testing GetTlo().Target?.ID}");
				var targetId = _mq2Dependencies.GetTlo().Target?.ID?.ToString() ?? "(null)";

				message = $"{{Target.ID}}: {targetId}";
				FileLoggingHelper.LogInformation(message);
				mq2.WriteChatSafe(message);

				await Task.Delay(500, cancellationToken);

				FileLoggingHelper.LogInformation("Testing DoCommand()");
				mq2.DoCommand("/echo Hi! I'm {Me.Name}");

				await Task.Delay(500, cancellationToken);

				FileLoggingHelper.LogInformation("Testing wait for chat input with timeout (success)");
				try
				{
					var waitForChatTask1 = _mq2Dependencies.GetChat().WaitForMQ2(
						(chatLine) => chatLine?.Contains("Hello World") == true,
						10_000,
						cancellationToken
					);

					mq2.DoCommand("/echo Hello World!");
					var wasSuccessful = await waitForChatTask1;

					message = $"WaitForChat was successful: {wasSuccessful}";
					FileLoggingHelper.LogInformation(message);
					mq2.WriteChatSafe(message);
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError(exc);
					mq2.WriteChatSafe($"Exception waiting for chat input (success): {exc}");
				}

				await Task.Delay(500, cancellationToken);

				FileLoggingHelper.LogInformation("Testing wait for chat input with timeout (timeout)");
				try
				{
					var waitForChatTask1 = _mq2Dependencies.GetChat().WaitForMQ2(
						(chatLine) => chatLine?.Contains("String that we're not going to actually type in") == true,
						5_000,
						cancellationToken
					);

					mq2.DoCommand("/echo Hello World!");
					var wasSuccessful = await waitForChatTask1;

					message = $"WaitForChat was successful: {wasSuccessful}";
					FileLoggingHelper.LogInformation(message);
					mq2.WriteChatSafe(message);
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError(exc);
					mq2.WriteChatSafe($"Exception waiting for chat input (timeout): {exc}");
				}
			}
			catch (TaskCanceledException)
			{
				throw;
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
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
