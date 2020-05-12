using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.Base
{
	internal sealed class MQ2AsyncCommandTaskWrapper : IDisposable
	{
		public MQ2AsyncCommandTaskWrapper(
			CancellationTokenSource cancellationTokenSource,
			string commandName,
			DateTime startTime,
			Task task
		)
		{
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
			CleanupHelper.TryCancel(CancellationTokenSource);
		}

		public void Dispose()
		{
			CleanupHelper.TryCancel(CancellationTokenSource);
			CleanupHelper.TryDispose(CancellationTokenSource);
			CleanupHelper.TryDispose(Task);
		}
	}
}
