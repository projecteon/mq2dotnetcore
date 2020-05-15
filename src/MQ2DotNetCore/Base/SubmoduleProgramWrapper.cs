using MQ2DotNetCore.Logging;
using MQ2DotNetCore.MQ2Api;
using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.Base
{
	internal class SubmoduleProgramWrapper : IDisposable
	{
		private bool _isDisposed = false;

		public SubmoduleProgramWrapper(
			AssemblyLoadContext assemblyLoadContext,
			CancellationTokenSource cancellationTokenSource,
			MQ2Dependencies mq2Dependencies,
			string name,
			IMQ2Program programInstance,
			DateTime startTime,
			Task task
		)
		{
			AssemblyLoadContext = assemblyLoadContext;
			CancellationTokenSource = cancellationTokenSource;
			MQ2Dependencies = mq2Dependencies;
			Name = name;
			ProgramInstance = programInstance;
			StartTime = startTime;
			Task = task;
		}

		public AssemblyLoadContext? AssemblyLoadContext { get; private set; }
		public CancellationTokenSource? CancellationTokenSource { get; private set; }
		public bool HasCancelled { get; private set; }
		public MQ2Dependencies MQ2Dependencies { get; private set; }
		public string Name { get; private set; }
		public IMQ2Program? ProgramInstance { get; private set; }
		public DateTime StartTime { get; private set; }
		public Task? Task { get; private set; }

		/// <inheritdoc />
		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			CleanupHelper.TryCancel(CancellationTokenSource);
			CleanupHelper.TryDispose(CancellationTokenSource);
			CleanupHelper.TryDispose(Task);

			if (ProgramInstance is IDisposable disposableProgramInstance)
			{
				CleanupHelper.TryDispose(disposableProgramInstance);
			}

			CleanupHelper.TryDispose(MQ2Dependencies);

			CleanupHelper.TryUnload(AssemblyLoadContext);
			AssemblyLoadContext = null;

			_isDisposed = true;
		}

		public async Task<TaskStatus> TryCancelAsync()
		{
			CleanupHelper.TryCancel(CancellationTokenSource);
			await Task.Delay(500);

			FileLoggingHelper.LogDebug($"Task status after first delay is {Task?.Status.ToString() ?? "(null)"}");

			if (!CleanupHelper.IsTaskStopped(Task))
			{
				await Task.Delay(1000);
				FileLoggingHelper.LogDebug($"Task status after second delay is {Task?.Status.ToString() ?? "(null)"}");
			}

			HasCancelled = true;

			return Task?.Status ?? TaskStatus.RanToCompletion;
		}
	}
}
