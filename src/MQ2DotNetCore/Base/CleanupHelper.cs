using MQ2DotNetCore.Logging;
using System;
using System.Runtime.Loader;

namespace MQ2DotNetCore.Base
{
	public static class CleanupHelper
	{
		public static bool TryDispose(IDisposable? disposable)
		{
			try
			{
				disposable?.Dispose();
				return true;
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"{nameof(CleanupHelper)}.{nameof(TryDispose)}(..) encountered an unexpected exception while trying to dispose the object of type {disposable?.GetType().FullName ?? "(NULL)"}!\n\n{exc.ToString()}");
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
				FileLoggingHelper.LogError($"{nameof(CleanupHelper)}.{nameof(TryUnload)}(..) encountered an unexpected exception while trying unload the assembly load context ({assemblyLoadContext?.Name})!\n\n{exc.ToString()}");
				return false;
			}
		}
	}
}
