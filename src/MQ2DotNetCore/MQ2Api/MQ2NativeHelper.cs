using Microsoft.Extensions.Logging;
using MQ2DotNetCore.Interop;
using MQ2DotNetCore.Logging;
using System;
using System.Runtime.InteropServices;
using static MQ2DotNetCore.Interop.MQ2Main.NativeMethods;

namespace MQ2DotNetCore.MQ2Api
{
	public sealed class MQ2NativeHelper
	{
		private readonly ILogger<MQ2NativeHelper>? _logger;
		private readonly SafeLibraryHandle? _mq2MainLibraryHandle;

		internal MQ2NativeHelper(ILogger<MQ2NativeHelper>? logger)
		{
			_logger = logger;

			try
			{
				_mq2MainLibraryHandle = Kernel32.NativeMethods.LoadLibrary(MQ2Main.DLL);
			}
			catch (Exception exc)
			{
				_logger?.LogCriticalPrefixed($"Failed to load library {MQ2Main.DLL}!\n\n{exc}\n");
			}
		}

		// Marshal doesn't want to return this struct (since it's non-blittable thanks to the delegate & string) so gotta do it manually
		internal MQ2DataItem FindMQ2Data(string szName)
			=> Marshal.PtrToStructure<MQ2DataItem>(MQ2Main.NativeMethods.FindMQ2DataIntPtr(szName));

		/// <summary>
		/// Helper method to retrieve the local player's character spawn int pointer. The command callbacks are passed a version of this
		/// but the value can change overtime (e.g. if the character zones) so it's better to fetch this on demand.
		/// </summary>
		/// <returns>The character spawn int pointer value.</returns>
		public IntPtr GetCharacterSpawnIntPointer()
		{
			if (_mq2MainLibraryHandle == null)
			{
				return IntPtr.Zero;
			}

			try
			{
				var ppLocalPlayer = Kernel32.NativeMethods.GetProcAddress(_mq2MainLibraryHandle, "ppLocalPlayer");
				var ppPlayer = Marshal.ReadIntPtr(ppLocalPlayer);
				return Marshal.ReadIntPtr(ppPlayer);
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
				return IntPtr.Zero;
			}
		}

		private string? _mq2IniPath;
		public string? GetMQ2IniPath()
		{
			if (_mq2IniPath != null)
			{
				return _mq2IniPath;
			}

			if (_mq2MainLibraryHandle == null)
			{
				return null;
			}

			var mq2InitPath = Marshal.PtrToStringAnsi(Kernel32.NativeMethods.GetProcAddress(_mq2MainLibraryHandle, "gszINIPath"));
			_mq2IniPath = mq2InitPath;
			return _mq2IniPath;
		}

		public IntPtr GetSpawnManagerIntPointer()
		{
			if (_mq2MainLibraryHandle == null)
			{
				return IntPtr.Zero;
			}

			var ppSpawnManager = Marshal.ReadIntPtr(Kernel32.NativeMethods.GetProcAddress(_mq2MainLibraryHandle, "ppSpawnManager"));
			return Marshal.ReadIntPtr(ppSpawnManager);
		}
	}
}
