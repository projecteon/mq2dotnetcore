using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

		private ILogger<TestProgram>? _logger = null;
		private MQ2Dependencies _mq2Dependencies = null!; // nullability hack, will always be initialized in RunAsync

		public async Task RunAsync(string[] commandArguments, MQ2Dependencies mq2Dependencies, CancellationToken cancellationToken)
		{
			ILoggerFactory? loggerFactory = null;
			try
			{
				var configuration = MQ2DotNetCore.Base.ConfigurationHelper.GetConfiguration();
				var mq2DotNetCoreOptions = new MQ2DotNetCore.Base.MQ2DotNetCoreOptions(configuration);
				loggerFactory = MQ2DotNetCore.Base.ConfigurationHelper.CreateLoggerFactory(configuration, mq2DotNetCoreOptions);
				_logger = loggerFactory?.CreateLogger<TestProgram>();

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
						_logger?.LogTracePrefixed("Task has been cancelled");
#endif

						break;
					}
				}
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
				mq2Dependencies?.GetMQ2().WriteChatSafe($"{nameof(TestProgram)}.{nameof(RunAsync)} threw an exception: {exc}");
			}
			finally
			{
				loggerFactory?.Dispose();
			}
		}

		public async Task TestApisCommandAsync(string[] commandArguments, CancellationToken cancellationToken)
		{
			try
			{
				_logger?.LogInformationPrefixed("Testing MQ2 Api native calls");
				var mq2 = _mq2Dependencies.GetMQ2();
				mq2.WriteChatSafe("Testing MQ2 Api native calls");

				_logger?.LogInformationPrefixed("Testing GetTlo().Me?.Name}");
				var playerName = _mq2Dependencies.GetTlo().Me?.Name;

				var message = $"{{Me.Name}}: {playerName}";
				_logger?.LogInformationPrefixed(message);
				mq2.WriteChatSafe(message);

				await Task.Delay(500, cancellationToken);

				_logger?.LogInformationPrefixed("Testing GetTlo().Target?.ID}");
				var targetId = _mq2Dependencies.GetTlo().Target?.ID?.ToString() ?? "(null)";

				message = $"{{Target.ID}}: {targetId}";
				_logger?.LogInformationPrefixed(message);
				mq2.WriteChatSafe(message);

				await Task.Delay(500, cancellationToken);

				_logger?.LogInformationPrefixed("Testing DoCommand()");
				mq2.DoCommand("/echo Hi! I'm {Me.Name}");

				await Task.Delay(500, cancellationToken);

				_logger?.LogInformationPrefixed("Testing wait for chat input with timeout (success)");
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
					_logger?.LogInformationPrefixed(message);
					mq2.WriteChatSafe(message);
				}
				catch (Exception exc)
				{
					_logger?.LogErrorPrefixed(exc);
					mq2.WriteChatSafe($"Exception waiting for chat input (success): {exc}");
				}

				await Task.Delay(500, cancellationToken);

				_logger?.LogInformationPrefixed("Testing wait for chat input with timeout (timeout)");
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
					_logger?.LogInformationPrefixed(message);
					mq2.WriteChatSafe(message);
				}
				catch (Exception exc)
				{
					_logger?.LogErrorPrefixed(exc);
					mq2.WriteChatSafe($"Exception waiting for chat input (timeout): {exc}");
				}
			}
			catch (TaskCanceledException)
			{
				throw;
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}

		public async Task TestCommandAsync(string[] commandArguments, CancellationToken cancellationToken)
		{
			int? randomId = null;
			try
			{
				randomId = new Random().Next();

				_logger?.LogInformationPrefixed($"Executing! [RandomId: {randomId}] [CommandArguments: {string.Join(", ", commandArguments)}]");

				// 100 loops == ~20 seconds
				for (var loopIndex = 0; loopIndex < 1500; ++loopIndex)
				{
					await Task.Delay(200, cancellationToken);

					// These should all log the same managed thread id
					if (loopIndex % 100 == 0)
					{
						_logger?.LogInformationPrefixed($"TestCommandAsync(..) [LoopIndex1: {loopIndex}] [RandomId: {randomId}] [CommandArguments: {string.Join(", ", commandArguments)}]");
					}
				}
			}
			catch (TaskCanceledException)
			{
				_logger?.LogDebugPrefixed($"{nameof(TestCommandAsync)} has been cancelled! [RandomId: {randomId}] [CommandArguments: {string.Join(", ", commandArguments)}]");
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}
	}
}
