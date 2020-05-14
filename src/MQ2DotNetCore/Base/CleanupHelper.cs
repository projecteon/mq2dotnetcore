using MQ2DotNetCore.Logging;
using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.Base
{
	public static class CleanupHelper
	{
		public static void DisposedCheck(bool isDisposed, string objectName)
		{
			if (isDisposed)
			{
				throw new ObjectDisposedException(objectName);
			}
		}

		public static bool IsTaskStopped(Task? task)
		{
			if (task == null)
			{
				return true;
			}

			return task.Status == TaskStatus.RanToCompletion
				|| task.Status == TaskStatus.Canceled
				|| task.Status == TaskStatus.Faulted;
		}

		public static bool TryCancel(CancellationTokenSource? cancellationTokenSource)
		{
			try
			{
				cancellationTokenSource?.Cancel();
				return true;
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"Unexpected exception while trying signal the cancellation token source!\n\n{exc}\n");
				return false;
			}
		}

		public static bool TryDispose(IDisposable? disposable)
		{
			try
			{
				disposable?.Dispose();
				return true;
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"Unexpected exception while trying to dispose the object of type {disposable?.GetType().FullName ?? "(NULL)"}!\n\n{exc}\n");
				return false;
			}
		}

		public static bool TryUnload(AssemblyLoadContext? assemblyLoadContext)
		{
			try
			{
				assemblyLoadContext?.Unload();
				return true;
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"Unexpected exception while trying unload the assembly load context ({assemblyLoadContext?.Name})!\n\n{exc}\n");
				return false;
			}
		}
	}
}
