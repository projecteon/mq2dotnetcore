using MQ2DotNetCore.Base;
using MQ2DotNetCore.Interop;
using MQ2DotNetCore.Logging;
using MQ2DotNetCore.MQ2Api;
using MQ2DotNetCore.MQ2Api.DataTypes;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore
{
	public static class LoaderEntryPoint
	{
		private static SafeLibraryHandle? _loaderLibraryHandle;

		private static readonly MQ2CommandRegistry _mq2CommandRegistry;
		private static readonly MQ2SynchronizationContext _mq2SynchronizationContext;

		static LoaderEntryPoint()
		{
			_mq2SynchronizationContext = new MQ2SynchronizationContext();
			_mq2CommandRegistry = new MQ2CommandRegistry(_mq2SynchronizationContext);
		}

		public static int InitializePlugin(IntPtr arg, int argLength)
		{
			try
			{
				FileLoggingHelper.LogInformation("The InitializePlugin(..) method is executing...");
				FileLoggingHelper.LogDebug($"AssemblyName: {MQ2DotNetCoreAssemblyInformation.AssemblyName.Name},   AssemblyVersion: {MQ2DotNetCoreAssemblyInformation.Version}");

				TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;

				// MQ2DotNetCoreLoader.dll exports function pointers that it then calls when the corresponding plugin function is called
				// Here we set set the exported function pointers to our managed function delegates in this class

				// TODO: Consider passing/parsing the loader dll path through parameters
				FileLoggingHelper.LogDebug($"Loader DLL Path: {MQ2DotNetCoreLoader.AbsoluteDllPath}");
				_loaderLibraryHandle = Kernel32.NativeMethods.LoadLibrary(MQ2DotNetCoreLoader.AbsoluteDllPath);
				if (_loaderLibraryHandle == null)
				{
					FileLoggingHelper.LogError($"{nameof(_loaderLibraryHandle)} is null");
					return 1;
				}

				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfShutdownPlugin"), Marshal.GetFunctionPointerForDelegate(_handleShutdownPlugin));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnCleanUI"), Marshal.GetFunctionPointerForDelegate(_handleCleanUI));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnReloadUI"), Marshal.GetFunctionPointerForDelegate(_handleReloadUI));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnDrawHUD"), Marshal.GetFunctionPointerForDelegate(_handleDrawHUD));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfSetGameState"), Marshal.GetFunctionPointerForDelegate(_handleSetGameState));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnPulse"), Marshal.GetFunctionPointerForDelegate(_handlePulse));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnIncomingChat"), Marshal.GetFunctionPointerForDelegate(_handleIncomingChat));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnWriteChatColor"), Marshal.GetFunctionPointerForDelegate(_handleWriteChatColor));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnAddSpawn"), Marshal.GetFunctionPointerForDelegate(_handleAddSpawn));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnRemoveSpawn"), Marshal.GetFunctionPointerForDelegate(_handleRemoveSpawn));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnAddGroundItem"), Marshal.GetFunctionPointerForDelegate(_handleAddGroundItem));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnRemoveGroundItem"), Marshal.GetFunctionPointerForDelegate(_handleRemoveGroundItem));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfBeginZone"), Marshal.GetFunctionPointerForDelegate(_handleBeginZone));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfEndZone"), Marshal.GetFunctionPointerForDelegate(_handleEndZone));
				Marshal.WriteIntPtr(Kernel32.NativeMethods.GetProcAddress(_loaderLibraryHandle, "g_pfOnZoned"), Marshal.GetFunctionPointerForDelegate(_handleZoned));

				FileLoggingHelper.LogDebug($"Done registering delegates to the loader dll's exported function pointer addresses!");

				FileLoggingHelper.LogDebug("Attempting to register the primary commands...");

				// TODO: Possible option to shorten commands, detect if the legacy
				// MQ2DotNet plugin dll files are present / MQ2 ini entry for MQ2DotNet is present
				// and only register the commands as /netcoreXXX if we need to disambiguate
				// otherwise just use /netXXX to keep the command length shorter

				// And command to run/end .net programs
				_mq2CommandRegistry.AddCommand(nameof(LoaderEntryPoint), "/netcorerun", NetRunCommand);
				_mq2CommandRegistry.AddCommand(nameof(LoaderEntryPoint), "/netcoreend", NetEndCommand);

				_mq2CommandRegistry.AddCommand(nameof(LoaderEntryPoint), "/netcorelist", NetListCommand);

				_mq2CommandRegistry.AddCommand(nameof(LoaderEntryPoint), "/netcorecanceltask", NetCancelCommandTask);


				// TODO: Support csx script file compilation and execution like what MQ2DotNet has
				// The compilation is pretty straight forward, just need to use the Roslyn scripting
				// package Microsoft.CodeAnalysis.CSharp.Scripting. Not sure if it is possible to
				// use a separate collectible AssemblyLoadContext for each script execution though...
				// The scripting Create(..) methods accept an InteractiveAssemblyLoader parameter
				// but the documentation on how what the loader is or how to use it is pretty
				// sparse.


				// TODO: Support plugins the way MQ2DotNet did? I can't see a clear use case for it at this point.
				// Any program that runs using /netcorerun can subscribe to the plugin method events other than
				// OnPulse, and the async continuations run during OnPulse anyway so an event handler for that isn't
				// really necessary...

				FileLoggingHelper.LogDebug("Done registering the primary commands.");

				return 0;
			}
			catch (Exception exc)
			{
				try
				{
					FileLoggingHelper.LogCritical($"InitializePlugin(..) encountered an exception:\n\n{exc}\n");
				}
				catch (Exception)
				{

				}
			}

			return 1;
		}

		public static void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs eventArgs)
		{
			try
			{
				var message = $"{nameof(HandleUnobservedTaskException)} was called!";
				if (sender != null)
				{
					message += $"\n\tSenderType: {sender.GetType()?.FullName}\n\tSender: {sender}";
				}

				if (eventArgs?.Exception != null)
				{
					message += $"\n\tUnobserved Exception:\n\n{eventArgs.Exception}\n\n";
				}

				FileLoggingHelper.LogError(message);

				eventArgs?.SetObserved();

				MQ2.Instance.WriteChatSafe($"{nameof(HandleUnobservedTaskException)} was called. See the log file for more information.");
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NetCancelCommandTask(string[] commandArguments)
		{
			try
			{
				if (commandArguments == null || commandArguments.Length < 1)
				{
					MQ2.WriteChatProgram("Usage: /netcorecanceltask <commandName|*>");
					return;
				}

				var commandNameToStop = commandArguments[0];
				var canceledTaskCount = commandNameToStop == "*"
					? _mq2CommandRegistry.CancelAllAsyncCommandTasks()
					: _mq2CommandRegistry.CancelAsyncCommandTask(commandNameToStop);

				FileLoggingHelper.LogInformation($"Canceled {canceledTaskCount} async command tasks for command: {commandNameToStop}");
				MQ2.Instance.WriteChatSafe($"Canceled {canceledTaskCount} async command tasks for command: {commandNameToStop}");
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NetEndCommand(string[] commandArguments)
		{
			try
			{
				if (commandArguments == null || commandArguments.Length < 1)
				{
					MQ2.WriteChatProgram("Usage: /netcoreend <programName|*>");
					return;
				}


				var programNameToStop = commandArguments[0];
				if (programNameToStop == "*")
				{
					var wereAllStoppedSuccessfully = SubmoduleRegistry.Instance.StopAllPrograms();
					if (wereAllStoppedSuccessfully)
					{
						FileLoggingHelper.LogDebug($"All programs stopped and unloaded successfully");
						MQ2.WriteChatProgram($"All programs stopped and unloaded successfully");
					}
					else
					{
						FileLoggingHelper.LogWarning($"Failed to stop/unload one or more programs!");
						MQ2.WriteChatProgramWarning($"Failed to stop/unload one or more programs!");
					}

					return;
				}

				var shouldSkipCancel = commandArguments.Skip(1).Any(commandArgument => commandArgument == "skipcancel");
				if (shouldSkipCancel)
				{
					var wasStopped = SubmoduleRegistry.Instance.StopProgram(programNameToStop);
					if (wasStopped)
					{
						FileLoggingHelper.LogDebug($"{programNameToStop} program stopped and unloaded successfully");
						MQ2.WriteChatProgram($"{programNameToStop} program stopped and unloaded successfully");
					}
					else
					{
						FileLoggingHelper.LogWarning($"Failed to stop/unload {programNameToStop} program!");
						MQ2.WriteChatProgramWarning($"Failed to stop/unload {programNameToStop} program!");
					}
				}

				_mq2SynchronizationContext.SetExecuteAndRestore(() =>
				{
					FileLoggingHelper.LogDebug($"Scheduling {nameof(SubmoduleRegistry)}.{nameof(SubmoduleRegistry.TryStopProgramAsync)}(..) call to run on synchronization context for program name: {programNameToStop}");
					Task? tryCancelTask = null;
					Task<Task>? wrapperTask = null;
					try
					{
						tryCancelTask = SubmoduleRegistry.Instance.TryStopProgramAsync(programNameToStop);
						wrapperTask = Task.Factory.StartNew(
							async () => await tryCancelTask,
							CancellationToken.None,
							TaskCreationOptions.None,
							TaskScheduler.FromCurrentSynchronizationContext()
						);
					}
					catch (Exception exc)
					{
						FileLoggingHelper.LogError($"Exception attempting to schedule the TryStopProgramAsync task:\n\n{exc}\n");
						CleanupHelper.TryDispose(tryCancelTask);
						CleanupHelper.TryDispose(wrapperTask);

						// Try stopping the program without cancelling
						SubmoduleRegistry.Instance.StopProgram(programNameToStop);
					}
				});
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NetListCommand(string[] commandArguments)
		{
			try
			{
				SubmoduleRegistry.Instance.PrintRunningPrograms();

				_mq2CommandRegistry.PrintRegisteredCommands();

				_mq2CommandRegistry.PrintRunningCommands();
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NetRunCommand(string[] commandArguments)
		{
			try
			{
				if (commandArguments == null || commandArguments.Length == 0)
				{
					MQ2.WriteChatProgram("Usage: /netcorerun <program> [<arg1> <arg2> ...]");
					return;
				}

				var submoduleProgramName = commandArguments[0];
				_mq2SynchronizationContext.SetExecuteAndRestore(() =>
				{
					try
					{
						// give the submodule it's own type factory to register type's against, if they wish
						var submoduleTypeFactory = new MQ2TypeFactory(MQ2TypeFactory.RootFactory);

						var submoduleCommandRegistry = new MQ2SubmoduleCommandRegistry(_mq2CommandRegistry, submoduleProgramName);
						var submoduleEventRegistry = new MQ2SubmoduleEventRegistry(submoduleProgramName);

						var submoduleDependencies = new MQ2Dependencies(
							new ChatUtilities(submoduleEventRegistry),
							submoduleCommandRegistry,
							submoduleEventRegistry,
							MQ2.Instance,
							_mq2SynchronizationContext,
							submoduleTypeFactory,
							new MQ2Spawns(submoduleTypeFactory),
							submoduleProgramName,
							new MQ2Tlo(submoduleTypeFactory)
						);

						var wasStarted = SubmoduleRegistry.Instance.StartProgram(submoduleProgramName, commandArguments, submoduleDependencies);
						if (wasStarted)
						{
							FileLoggingHelper.LogDebug($"{commandArguments[0]} program started successfully");
							MQ2.WriteChatProgram($"{commandArguments[0]} program started successfully");
						}
						else
						{
							FileLoggingHelper.LogWarning($"Failed to start {commandArguments[0]} program!");
							MQ2.WriteChatProgramWarning($"Failed to start {commandArguments[0]} program!");
						}
					}
					catch (Exception exc)
					{
						FileLoggingHelper.LogError(exc);
						MQ2.WriteChatGeneralError($"{nameof(NetRunCommand)} threw an exception: {exc}");
					}
				});
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}



		// MQ2DotNetCoreLoader.dll Delegate Types
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQBeginZone();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQCleanUI();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQDrawHUD();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQEndZone();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQGroundItem(IntPtr groundItem);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate uint fMQIncomingChat([MarshalAs(UnmanagedType.LPStr)]string chatLine, uint color);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQPulse();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQReloadUI();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQSetGameState(uint gameState);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQSpawn(IntPtr spawn);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQShutdownPlugin();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate uint fMQWriteChatColor([MarshalAs(UnmanagedType.LPStr)]string line, uint color, uint filter);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void fMQZoned();




		// Delegates methods to register with the MQ2DotNetCoreLoader.dll
		private static readonly fMQGroundItem _handleAddGroundItem = HandleAddGroundItem;
		private static void HandleAddGroundItem(IntPtr newGroundItemPointer)
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				var newGroundItem = new GroundType(MQ2TypeFactory.RootFactory, newGroundItemPointer);

				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(
					(submoduleWrapper) => NotifyAddGroundItem(submoduleWrapper, newGroundItem));
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifyAddGroundItem(SubmoduleProgramWrapper submodule, GroundType newGroundItem)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyAddGroundItem(newGroundItem);



		private static readonly fMQSpawn _handleAddSpawn = HandleAddSpawn;
		private static void HandleAddSpawn(IntPtr newSpawnPointer)
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				var newSpawn = new SpawnType(MQ2TypeFactory.RootFactory, newSpawnPointer);

				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(
					(submoduleWrapper) => NotifyAddSpawn(submoduleWrapper, newSpawn));
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifyAddSpawn(SubmoduleProgramWrapper submodule, SpawnType newSpawn)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyAddSpawn(newSpawn);



		private static readonly fMQBeginZone _handleBeginZone = HandleBeginZone;
		private static void HandleBeginZone()
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(NotifyBeginZone);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifyBeginZone(SubmoduleProgramWrapper submodule)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyBeginZone(EventArgs.Empty);



		private static readonly fMQCleanUI _handleCleanUI = HandleCleanUI;
		private static void HandleCleanUI()
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(NotifyCleanUI);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifyCleanUI(SubmoduleProgramWrapper submodule)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyCleanUI(EventArgs.Empty);



		private static readonly fMQDrawHUD _handleDrawHUD = HandleDrawHUD;
		private static long drawHudCount = 0;
		private static void HandleDrawHUD()
		{
			try
			{

#if DEBUG
				Interlocked.Increment(ref drawHudCount);

				if ((drawHudCount % 10_000) == 0)
				{
					FileLoggingHelper.LogTrace($"HandleDrawHUD fired. [DrawHUDCount: {drawHudCount}]");
				}

				if (drawHudCount > 1_000_000)
				{
					Interlocked.Exchange(ref drawHudCount, 0);
				}
#endif

				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(NotifyDrawHUD);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifyDrawHUD(SubmoduleProgramWrapper submodule)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyDrawHUD(EventArgs.Empty);



		private static readonly fMQEndZone _handleEndZone = HandleEndZone;
		private static void HandleEndZone()
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(NotifyEndZone);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifyEndZone(SubmoduleProgramWrapper submodule)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyEndZone(EventArgs.Empty);



		private static readonly fMQIncomingChat _handleIncomingChat = HandleIncomingChat;
		private static uint HandleIncomingChat(string chatLine, uint color)
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				var chatLineEventArgs = new ChatLineEventArgs(chatLine, color);

				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(
					(submoduleWrapper) => NotifyIncomingChat(submoduleWrapper, chatLineEventArgs));
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);

			}

			return 0;
		}

		private static void NotifyIncomingChat(SubmoduleProgramWrapper submodule, ChatLineEventArgs chatLineEventArgs)
		{
			try
			{
				submodule.MQ2Dependencies.GetEventRegistry().NotifyChatEQ(chatLineEventArgs);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}

			try
			{
				submodule.MQ2Dependencies.GetEventRegistry().NotifyChatAny(chatLineEventArgs);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}



		private static readonly fMQPulse _handlePulse = HandlePulse;
		private static long pulseCount = 1;
		private static void HandlePulse()
		{
			try
			{
				_mq2SynchronizationContext.DoEvents(true);

				Interlocked.Increment(ref pulseCount);

				if ((pulseCount % 250) == 0)
				{
					var removedProgramCount = SubmoduleRegistry.Instance.ProcessRunningProgramTasks();
				}

				if ((pulseCount % 350) == 0)
				{
					_mq2CommandRegistry.ProcessAsyncCommandTasks();
				}

#if DEBUG
				if ((pulseCount % 10_000) == 0)
				{
					FileLoggingHelper.LogDebug($"(Pulse {pulseCount}) GC.GetTotalMemory(): {GC.GetTotalMemory(true)}");
				}
				else if ((pulseCount % 1_000) == 0)
				{
					FileLoggingHelper.LogDebug($"(Pulse {pulseCount}) GC.GetTotalMemory(): {GC.GetTotalMemory(false)}");
				}
#endif

				if (pulseCount > 1_000_000)
				{
#if DEBUG
					FileLoggingHelper.LogTrace("Resetting pulse count to 1");
#endif

					Interlocked.Exchange(ref pulseCount, 1);
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}



		private static readonly fMQReloadUI _handleReloadUI = HandleReloadUI;
		private static void HandleReloadUI()
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(NotifyReloadUI);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifyReloadUI(SubmoduleProgramWrapper submodule)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyReloadUI(EventArgs.Empty);



		private static readonly fMQGroundItem _handleRemoveGroundItem = HandleRemoveGroundItem;
		private static void HandleRemoveGroundItem(IntPtr removedGroundItemPointer)
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				var removedGroundItem = new GroundType(MQ2TypeFactory.RootFactory, removedGroundItemPointer);

				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(
					(submoduleWrapper) => NotifyRemoveGroundItem(submoduleWrapper, removedGroundItem));
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifyRemoveGroundItem(SubmoduleProgramWrapper submodule, GroundType removedGroundItem)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyRemoveGroundItem(removedGroundItem);



		private static readonly fMQSpawn _handleRemoveSpawn = HandleRemoveSpawn;
		private static void HandleRemoveSpawn(IntPtr removedSpawnPointer)
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				var removedSpawn = new SpawnType(MQ2TypeFactory.RootFactory, removedSpawnPointer);

				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(
					(submoduleWrapper) => NotifyRemoveSpawn(submoduleWrapper, removedSpawn));
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifyRemoveSpawn(SubmoduleProgramWrapper submodule, SpawnType removedSpawn)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyRemoveSpawn(removedSpawn);




		private static readonly fMQSetGameState _handleSetGameState = HandleSetGameState;
		private static void HandleSetGameState(uint gameStateValue)
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				var gameState = Enum.IsDefined(typeof(GameState), gameStateValue)
					? (GameState)gameStateValue
					: GameState.Unknown;

				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(
					(submodule) => NotifySetGameState(submodule, gameState));
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifySetGameState(SubmoduleProgramWrapper submodule, GameState gameState)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifySetGameState(gameState);



		private static readonly fMQShutdownPlugin _handleShutdownPlugin = HandleShutdownPlugin;
		private static void HandleShutdownPlugin()
		{
			try
			{
				// Keep Initialize and Shutdown at LogInformation(..) level
				FileLoggingHelper.LogInformation("Method was called!");

				FileLoggingHelper.LogInformation($"Disposing of the {nameof(SubmoduleRegistry)}...");
				CleanupHelper.TryDispose(SubmoduleRegistry.Instance);

				FileLoggingHelper.LogInformation($"Disposing of the {nameof(MQ2CommandRegistry)}...");
				CleanupHelper.TryDispose(_mq2CommandRegistry);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}



		private static readonly fMQWriteChatColor _handleWriteChatColor = HandleWriteChatColor;
		private static uint HandleWriteChatColor(string chatLine, uint color, uint filter)
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				var chatLineEventArgs = new ChatLineEventArgs(chatLine, color, filter);

				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(
					(submodule) => NotifyWriteChatColor(submodule, chatLineEventArgs));
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}

			return 0;
		}

		private static void NotifyWriteChatColor(SubmoduleProgramWrapper submodule, ChatLineEventArgs chatLineEventArgs)
		{
			try
			{
				submodule.MQ2Dependencies.GetEventRegistry().NotifyChatMQ2(chatLineEventArgs);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}

			try
			{
				submodule.MQ2Dependencies.GetEventRegistry().NotifyChatAny(chatLineEventArgs);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}



		private static readonly fMQZoned _handleZoned = HandleZoned;
		private static void HandleZoned()
		{
			try
			{
#if DEBUG
				FileLoggingHelper.LogTrace("Method was called");
#endif
				SubmoduleRegistry.Instance.ExecuteForEachSubmodule(NotifyZoned);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		private static void NotifyZoned(SubmoduleProgramWrapper submodule)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyZoned(EventArgs.Empty);
	}
}
