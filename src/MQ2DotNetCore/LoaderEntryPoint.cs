using MQ2DotNetCore.Base;
using MQ2DotNetCore.Interop;
using MQ2DotNetCore.Logging;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace MQ2DotNetCore
{
	public static class LoaderEntryPoint
	{
		public static int InitializePlugin(IntPtr arg, int argLength)
		{
			try
			{
				FileLoggingHelper.LogInformation("The InitializePlugin(..) method is executing...");

				// MQ2DotNetCoreLoader.dll exports function pointers that it then calls when the corresponding plugin function is called
				// Here we set set the exported function pointers to our managed function delegates in this class

				// TODO: Consider passing/parsing the loader dll path through parameters
				var loaderDllPath = Path.Combine(AssemblyInformation.AssemblyDirectory, @"..\MQ2DotNetLoader.dll");
				FileLoggingHelper.LogDebug($"Loader DLL Path: {loaderDllPath}");

				var mq2DotNetCoreLoaderLibraryHandle = Kernel32.NativeMethods.LoadLibrary(loaderDllPath);
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfShutdownPlugin"), Marshal.GetFunctionPointerForDelegate(_shutdownPlugin));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnCleanUI"), Marshal.GetFunctionPointerForDelegate(_handleCleanUI));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnReloadUI"), Marshal.GetFunctionPointerForDelegate(_handleReloadUI));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnDrawHUD"), Marshal.GetFunctionPointerForDelegate(_handleDrawHUD));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfSetGameState"), Marshal.GetFunctionPointerForDelegate(_setGameState));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnPulse"), Marshal.GetFunctionPointerForDelegate(_handlePulse));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnIncomingChat"), Marshal.GetFunctionPointerForDelegate(_handleIncomingChat));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnWriteChatColor"), Marshal.GetFunctionPointerForDelegate(_handleWriteChatColor));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnAddSpawn"), Marshal.GetFunctionPointerForDelegate(_handleAddSpawn));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnRemoveSpawn"), Marshal.GetFunctionPointerForDelegate(_handleRemoveSpawn));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnAddGroundItem"), Marshal.GetFunctionPointerForDelegate(_handleAddGroundItem));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnRemoveGroundItem"), Marshal.GetFunctionPointerForDelegate(_handleRemoveGroundItem));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfBeginZone"), Marshal.GetFunctionPointerForDelegate(_handleBeginZone));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfEndZone"), Marshal.GetFunctionPointerForDelegate(_handleEndZone));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(mq2DotNetCoreLoaderLibraryHandle, "g_pfOnZoned"), Marshal.GetFunctionPointerForDelegate(_handleZoned));

				FileLoggingHelper.LogDebug($"Done registering delegates to the loader dll's exported function pointer addresses!");

				return 0;
			}
			catch (Exception exc)
			{
				try
				{
					FileLoggingHelper.LogCritical($"The InitializePlugin(..) encountered an exception:\n\n{exc.ToString()}");
				}
				catch (Exception)
				{

				}
			}

			return 1;
		}


		// Delegates to register with the MQ2DotNetCoreLoader.dll
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQBeginZone();

		private static readonly fMQBeginZone _handleBeginZone = HandleBeginZone;

		private static void HandleBeginZone()
		{
			FileLoggingHelper.LogDebug($"{nameof(HandleBeginZone)}(..) was called");
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQCleanUI();

		private static readonly fMQCleanUI _handleCleanUI = HandleCleanUI;

		private static void HandleCleanUI()
		{
			FileLoggingHelper.LogDebug($"{nameof(HandleCleanUI)}(..) was called");
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQDrawHUD();

		private static readonly fMQDrawHUD _handleDrawHUD = HandleDrawHUD;

		private static void HandleDrawHUD()
		{
			FileLoggingHelper.LogTrace($"{nameof(HandleDrawHUD)}(..) was called");
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQEndZone();

		private static readonly fMQEndZone _handleEndZone = HandleEndZone;

		private static void HandleEndZone()
		{
			FileLoggingHelper.LogDebug($"{nameof(HandleEndZone)}(..) was called");
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQGroundItem(IntPtr groundItem);

		private static readonly fMQGroundItem _handleAddGroundItem = HandleAddGroundItem;

		private static void HandleAddGroundItem(IntPtr newGroundItem)
		{
			FileLoggingHelper.LogTrace($"{nameof(HandleAddGroundItem)}(..) was called");
		}



		private static readonly fMQGroundItem _handleRemoveGroundItem = HandleRemoveGroundItem;

		private static void HandleRemoveGroundItem(IntPtr groundItem)
		{
			FileLoggingHelper.LogTrace($"{nameof(HandleRemoveGroundItem)}(..) was called");
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate uint fMQIncomingChat([MarshalAs(UnmanagedType.LPStr)]string line, uint color);

		private static readonly fMQIncomingChat _handleIncomingChat = HandleIncomingChat;

		private static uint HandleIncomingChat(string line, uint color)
		{
			FileLoggingHelper.LogDebug($"{nameof(HandleIncomingChat)}(..) was called");
			return 0;
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQPulse();

		private static readonly fMQPulse _handlePulse = HandlePulse;

		private static long pulseCount = 0;
		private static void HandlePulse()
		{
			if ((pulseCount % 10_000) == 0)
			{
				FileLoggingHelper.LogTrace($"{nameof(HandlePulse)}(..) was called");
			}

			var newPulseCount = Interlocked.Increment(ref pulseCount);
			if (newPulseCount > (long.MaxValue / 2))
			{
				Interlocked.Exchange(ref pulseCount, 0);
			}
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQReloadUI();

		private static readonly fMQReloadUI _handleReloadUI = HandleReloadUI;

		private static void HandleReloadUI()
		{
			FileLoggingHelper.LogDebug($"{nameof(HandleReloadUI)}(..) was called");
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQSetGameState(uint gameState);

		private static readonly fMQSetGameState _setGameState = SetGameState;

		private static void SetGameState(uint gameState)
		{
			FileLoggingHelper.LogDebug($"{nameof(SetGameState)}(..) was called with a game state value of: {gameState}");
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]

		private delegate void fMQShutdownPlugin();

		private static readonly fMQShutdownPlugin _shutdownPlugin = ShutdownPlugin;

		private static void ShutdownPlugin()
		{
			// Keep Initialize and Shutdown at LogInformation(..) level
			FileLoggingHelper.LogInformation($"{nameof(ShutdownPlugin)}(..) was called!");
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQSpawn(IntPtr spawn);

		private static readonly fMQSpawn _handleAddSpawn = HandleAddSpawn;

		private static void HandleAddSpawn(IntPtr spawn)
		{
			FileLoggingHelper.LogTrace($"{nameof(HandleAddSpawn)}(..) was called!");
		}

		private static readonly fMQSpawn _handleRemoveSpawn = HandleRemoveSpawn;

		private static void HandleRemoveSpawn(IntPtr spawn)
		{
			FileLoggingHelper.LogTrace($"{nameof(HandleRemoveSpawn)}(..) was called!");
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate uint fMQWriteChatColor([MarshalAs(UnmanagedType.LPStr)]string line, uint color, uint filter);

		private static readonly fMQWriteChatColor _handleWriteChatColor = HandleWriteChatColor;

		private static uint HandleWriteChatColor(string line, uint color, uint filter)
		{
			FileLoggingHelper.LogTrace($"{nameof(HandleWriteChatColor)}(..) was called!");
			return 0;
		}



		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQZoned();

		private static readonly fMQZoned _handleZoned = HandleZoned;

		private static void HandleZoned()
		{
			FileLoggingHelper.LogDebug($"{nameof(HandleZoned)}(..) was called!");
		}
	}
}
