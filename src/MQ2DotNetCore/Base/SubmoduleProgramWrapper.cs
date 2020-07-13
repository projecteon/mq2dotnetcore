using Microsoft.Extensions.Logging;
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
		private ILogger<SubmoduleProgramWrapper>? _logger;

		public SubmoduleProgramWrapper(
			AssemblyLoadContext assemblyLoadContext,
			CancellationTokenSource cancellationTokenSource,
			ILogger<SubmoduleProgramWrapper>? logger,
			MQ2Dependencies mq2Dependencies,
			string name,
			IMQ2Program programInstance,
			DateTime startTime,
			Task task
		)
		{
			_logger = logger;

			AssemblyLoadContext = assemblyLoadContext;
			CancellationTokenSource = cancellationTokenSource;
			MQ2Dependencies = mq2Dependencies;
			Name = name;
			ProgramInstance = programInstance;
			StartTime = startTime;
			Task = task;
		}

		public AssemblyLoadContext AssemblyLoadContext { get; private set; }
		public CancellationTokenSource CancellationTokenSource { get; private set; }
		public bool HasCancelled { get; private set; }
		public MQ2Dependencies MQ2Dependencies { get; private set; }
		public string Name { get; }
		public IMQ2Program ProgramInstance { get; private set; }
		public DateTime StartTime { get; private set; }
		public Task Task { get; private set; }

		/// <inheritdoc />
		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			CleanupHelper.TryCancel(CancellationTokenSource, _logger);
			CleanupHelper.TryDispose(CancellationTokenSource, _logger);
			CleanupHelper.TryDispose(Task, _logger);

			if (ProgramInstance is IDisposable disposableProgramInstance)
			{
				CleanupHelper.TryDispose(disposableProgramInstance, _logger);
			}

			CleanupHelper.TryDispose(MQ2Dependencies, _logger);

			CleanupHelper.TryUnload(AssemblyLoadContext, _logger);

			// Force nulling them out once the object's been disposed
			AssemblyLoadContext = null!;
			CancellationTokenSource = null!;
			ProgramInstance = null!;
			MQ2Dependencies = null!;
			Task = null!;

			_isDisposed = true;
			_logger = null;
		}

		public async Task<TaskStatus> TryCancelAsync()
		{
			CleanupHelper.TryCancel(CancellationTokenSource, _logger);
			await Task.Delay(500);

			_logger?.LogDebugPrefixed($"Task status after first delay is {Task?.Status.ToString() ?? "(null)"}");

			if (!CleanupHelper.IsTaskStopped(Task))
			{
				await Task.Delay(1000);
				_logger?.LogDebugPrefixed($"Task status after second delay is {Task?.Status.ToString() ?? "(null)"}");
			}

			HasCancelled = true;

			return Task?.Status ?? TaskStatus.RanToCompletion;
		}
	}
}
