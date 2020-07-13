using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.Base
{
	internal sealed class MQ2AsyncCommandTaskWrapper : IDisposable
	{
		private readonly ILogger<MQ2AsyncCommandTaskWrapper>? _logger;

		public MQ2AsyncCommandTaskWrapper(
			CancellationTokenSource cancellationTokenSource,
			string commandName,
			ILogger<MQ2AsyncCommandTaskWrapper>? logger,
			DateTime startTime,
			Task task
		)
		{
			_logger = logger;

			CancellationTokenSource = cancellationTokenSource;
			CommandName = commandName;
			HasCancelled = false;
			StartTime = startTime;
			Task = task;
		}

		public CancellationTokenSource CancellationTokenSource { get; }
		public string CommandName { get; }
		public bool HasCancelled { get; private set; }
		public DateTime StartTime { get; }
		public Task Task { get; }

		public void Cancel()
		{
			HasCancelled = true;
			CleanupHelper.TryCancel(CancellationTokenSource, _logger);
		}

		public void Dispose()
		{
			CleanupHelper.TryCancel(CancellationTokenSource, _logger);
			CleanupHelper.TryDispose(CancellationTokenSource, _logger);
			CleanupHelper.TryDispose(Task, _logger);
		}
	}
}
