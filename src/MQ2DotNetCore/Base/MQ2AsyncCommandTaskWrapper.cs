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
			Task<Task> wrapperTask
		)
		{
			CancellationTokenSource = cancellationTokenSource;
			CommandName = commandName;
			WrapperTask = wrapperTask;
		}

		public CancellationTokenSource CancellationTokenSource { get; }
		public string CommandName { get; }
		public DateTime StartTime { get; }
		public Task<Task> WrapperTask { get; }

		public void Dispose()
		{
			CleanupHelper.TryCancel(CancellationTokenSource);
			CleanupHelper.TryDispose(CancellationTokenSource);
		}
	}
}
