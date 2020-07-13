using Microsoft.Extensions.Logging;
using MQ2DotNetCore.Base;
using MQ2DotNetCore.Interop;
using MQ2DotNetCore.Logging;
using MQ2DotNetCore.MQ2Api;
using MQ2DotNetCore.MQ2Api.DataTypes;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore
{
	public static class LoaderEntryPoint
	{
		private static SafeLibraryHandle? _loaderLibraryHandle;

		private static readonly ILoggerFactory? _loggerFactory;
		private static readonly ILogger? _logger;
		private static readonly MQ2CommandRegistry _mq2CommandRegistry;
		private static readonly ILogger<MQ2Dependencies>? _mq2DependenciesLogger;
		private static readonly MQ2 _mq2Instance;
		private static readonly MQ2SynchronizationContext _mq2SynchronizationContext;
		private static readonly MQ2DotNetCoreOptions _options;
		private static readonly MQ2TypeFactory _rootTypeFactory;
		private static readonly SubmoduleRegistry _submoduleRegistry;

		static LoaderEntryPoint()
		{
			var configuration = ConfigurationHelper.GetConfiguration();
			_options = new MQ2DotNetCoreOptions(configuration);

			_loggerFactory = ConfigurationHelper.CreateLoggerFactory(configuration, _options);
			_logger = _loggerFactory?.CreateLogger(nameof(LoaderEntryPoint));

			_mq2SynchronizationContext = new MQ2SynchronizationContext();

			_mq2DependenciesLogger = _loggerFactory?.CreateLogger<MQ2Dependencies>();

			var mq2NativeHelperLogger = _loggerFactory?.CreateLogger<MQ2NativeHelper>();
			var mq2NativeHelper = new MQ2NativeHelper(mq2NativeHelperLogger);

			_mq2Instance = new MQ2(mq2NativeHelper);

			var commandTaskWrapperLogger = _loggerFactory?.CreateLogger<MQ2AsyncCommandTaskWrapper>();
			var commandRegistryLogger = _loggerFactory?.CreateLogger<MQ2CommandRegistry>();
			_mq2CommandRegistry = new MQ2CommandRegistry(commandTaskWrapperLogger, commandRegistryLogger, _mq2Instance, _mq2SynchronizationContext);

			var typeFactoryLogger = _loggerFactory?.CreateLogger<MQ2TypeFactory>();
			_rootTypeFactory = new MQ2TypeFactory(typeFactoryLogger, mq2NativeHelper);
			_rootTypeFactory.RegisterTypesInAssembly(MQ2DotNetCoreAssemblyInformation.MQ2DotNetCoreAssembly);

			var submoduleRegistryLogger = _loggerFactory?.CreateLogger<SubmoduleRegistry>();
			var submoduleAssemblyLoadContextLogger = _loggerFactory?.CreateLogger<SubmoduleAssemblyLoadContext>();
			var submoduleProgramWrapperLogger = _loggerFactory?.CreateLogger<SubmoduleProgramWrapper>();
			_submoduleRegistry = new SubmoduleRegistry(submoduleRegistryLogger, _mq2Instance, submoduleAssemblyLoadContextLogger, submoduleProgramWrapperLogger);
		}

		public static int InitializePlugin(IntPtr arg, int argLength)
		{
			try
			{
				_logger?.LogInformationPrefixed("The InitializePlugin(..) method is executing...");
				_logger?.LogDebugPrefixed($"AssemblyName: {MQ2DotNetCoreAssemblyInformation.AssemblyName.Name},   AssemblyVersion: {MQ2DotNetCoreAssemblyInformation.Version}");

				TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;

				// MQ2DotNetCoreLoader.dll exports function pointers that it then calls when the corresponding plugin function is called
				// Here we set set the exported function pointers to our managed function delegates in this class

				// TODO: Consider passing/parsing the loader dll path through parameters
				_logger?.LogDebugPrefixed($"Loader DLL Path: {MQ2DotNetCoreLoader.AbsoluteDllPath}");
				_loaderLibraryHandle = Kernel32.NativeMethods.LoadLibrary(MQ2DotNetCoreLoader.AbsoluteDllPath);
				if (_loaderLibraryHandle == null)
				{
					_logger?.LogErrorPrefixed($"{nameof(_loaderLibraryHandle)} is null");
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

				var missingDependencyPaths =
					Directory.EnumerateFiles(
						MQ2DotNetCoreAssemblyInformation.AssemblyDirectory,
						"*.dll",
						SearchOption.TopDirectoryOnly
					)
					.Where(path =>!path.EndsWith("JetBrains.Annotations.dll", StringComparison.OrdinalIgnoreCase)
						&& !path.EndsWith("MQ2DotNetCore.dll", StringComparison.OrdinalIgnoreCase)
						&& !SubmoduleAssemblyLoadContext.MQ2DotNetCoreDependencies.Any(dependencyAssembly => path.Equals(dependencyAssembly.Location, StringComparison.OrdinalIgnoreCase))
					)
					.ToList();

				if (missingDependencyPaths.Count > 0)
				{
					_logger?.LogErrorPrefixed($"{nameof(SubmoduleAssemblyLoadContext)}.{nameof(SubmoduleAssemblyLoadContext.MQ2DotNetCoreDependencies)} is missing entries for the following dlls: \n{string.Join("\n", missingDependencyPaths)}\n");
				}


				_logger?.LogDebugPrefixed($"Done registering delegates to the loader dll's exported function pointer addresses!");

				_logger?.LogDebugPrefixed("Attempting to register the primary commands...");

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

				_logger?.LogDebugPrefixed("Done registering the primary commands.");

				return 0;
			}
			catch (Exception exc)
			{
				_logger?.LogCriticalPrefixed(exc);
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

				_logger?.LogErrorPrefixed(message);

				eventArgs?.SetObserved();

				_mq2Instance.WriteChatSafe($"{nameof(HandleUnobservedTaskException)} was called. See the log file for more information.");
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}

		private static void NetCancelCommandTask(string[] commandArguments)
		{
			try
			{
				if (commandArguments == null || commandArguments.Length < 1)
				{
					_mq2Instance.WriteChatProgram("Usage: /netcorecanceltask <commandName|*>");
					return;
				}

				var commandNameToStop = commandArguments[0];
				var canceledTaskCount = commandNameToStop == "*"
					? _mq2CommandRegistry.CancelAllAsyncCommandTasks()
					: _mq2CommandRegistry.CancelAsyncCommandTask(commandNameToStop);

				_logger?.LogInformationPrefixed($"Canceled {canceledTaskCount} async command tasks for command: {commandNameToStop}");
				_mq2Instance.WriteChatSafe($"Canceled {canceledTaskCount} async command tasks for command: {commandNameToStop}");
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}

		private static void NetEndCommand(string[] commandArguments)
		{
			try
			{
				if (commandArguments == null || commandArguments.Length < 1)
				{
					_mq2Instance.WriteChatProgram("Usage: /netcoreend <programName|*>");
					return;
				}


				var programNameToStop = commandArguments[0];
				if (programNameToStop == "*")
				{
					var wereAllStoppedSuccessfully = _submoduleRegistry.StopAllPrograms();
					if (wereAllStoppedSuccessfully)
					{
						_logger?.LogDebugPrefixed($"All programs stopped and unloaded successfully");
						_mq2Instance.WriteChatProgram($"All programs stopped and unloaded successfully");
					}
					else
					{
						_logger?.LogWarningPrefixed("Failed to stop/unload one or more programs!");
						_mq2Instance.WriteChatProgramWarning($"Failed to stop/unload one or more programs!");
					}

					return;
				}

				var shouldSkipCancel = commandArguments.Skip(1).Any(commandArgument => commandArgument == "skipcancel");
				if (shouldSkipCancel)
				{
					var wasStopped = _submoduleRegistry.StopProgram(programNameToStop);
					if (wasStopped)
					{
						_logger?.LogDebugPrefixed($"{programNameToStop} program stopped and unloaded successfully");
						_mq2Instance.WriteChatProgram($"{programNameToStop} program stopped and unloaded successfully");
					}
					else
					{
						_logger?.LogWarningPrefixed($"Failed to stop/unload {programNameToStop} program!");
						_mq2Instance.WriteChatProgramWarning($"Failed to stop/unload {programNameToStop} program!");
					}
				}

				_mq2SynchronizationContext.SetExecuteAndRestore(() =>
				{
					_logger?.LogDebugPrefixed($"Scheduling {nameof(SubmoduleRegistry)}.{nameof(SubmoduleRegistry.TryStopProgramAsync)}(..) call to run on synchronization context for program name: {programNameToStop}");
					Task? tryCancelTask = null;
					Task<Task>? wrapperTask = null;
					try
					{
						tryCancelTask = _submoduleRegistry.TryStopProgramAsync(programNameToStop);
						wrapperTask = Task.Factory.StartNew(
							async () => await tryCancelTask,
							CancellationToken.None,
							TaskCreationOptions.None,
							TaskScheduler.FromCurrentSynchronizationContext()
						);
					}
					catch (Exception exc)
					{
						_logger?.LogErrorPrefixed($"Exception attempting to schedule the TryStopProgramAsync task:\n\n{exc}\n");
						CleanupHelper.TryDispose(tryCancelTask, _logger);
						CleanupHelper.TryDispose(wrapperTask, _logger);

						// Try stopping the program without cancelling
						_submoduleRegistry.StopProgram(programNameToStop);
					}
				});
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}

		private static void NetListCommand(string[] commandArguments)
		{
			try
			{
				_submoduleRegistry.PrintRunningPrograms();

				_mq2CommandRegistry.PrintRegisteredCommands();

				_mq2CommandRegistry.PrintRunningCommands();
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}

		private static void NetRunCommand(string[] commandArguments)
		{
			try
			{
				if (commandArguments == null || commandArguments.Length == 0)
				{
					_mq2Instance.WriteChatProgram("Usage: /netcorerun <program> [<arg1> <arg2> ...]");
					return;
				}

				var submoduleProgramName = commandArguments[0];
				_mq2SynchronizationContext.SetExecuteAndRestore(() =>
				{
					try
					{
						// give the submodule it's own type factory to register type's against, if they wish
						var submoduleTypeFactory = new MQ2TypeFactory(_rootTypeFactory);

						var submoduleCommandRegistry = new MQ2SubmoduleCommandRegistry(_mq2CommandRegistry, submoduleProgramName);
						var submoduleEventRegistry = new MQ2SubmoduleEventRegistry(submoduleProgramName);

						var submoduleDependencies = new MQ2Dependencies(
							new ChatUtilities(submoduleEventRegistry),
							submoduleCommandRegistry,
							submoduleEventRegistry,
							_mq2DependenciesLogger,
							_mq2Instance,
							_mq2SynchronizationContext,
							submoduleTypeFactory,
							new MQ2Spawns(submoduleTypeFactory),
							submoduleProgramName,
							new MQ2Tlo(submoduleTypeFactory)
						);

						var wasStarted = _submoduleRegistry.StartProgram(submoduleProgramName, commandArguments, submoduleDependencies);
						if (wasStarted)
						{
							_logger?.LogDebugPrefixed($"{commandArguments[0]} program started successfully");
							_mq2Instance.WriteChatProgram($"{commandArguments[0]} program started successfully");
						}
						else
						{
							_logger?.LogWarningPrefixed($"Failed to start {commandArguments[0]} program!");
							_mq2Instance.WriteChatProgramWarning($"Failed to start {commandArguments[0]} program!");
						}
					}
					catch (Exception exc)
					{
						_logger?.LogErrorPrefixed(exc);
						_mq2Instance.WriteChatGeneralError($"{nameof(NetRunCommand)} threw an exception: {exc}");
					}
				});
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
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
#pragma warning disable RCS1057 // Add empty line between declarations.
		private static readonly fMQGroundItem _handleAddGroundItem = HandleAddGroundItem;
		private static void HandleAddGroundItem(IntPtr newGroundItemPointer)
		{
			try
			{
#if DEBUG
				_logger?.LogTracePrefixed("Method was called");
#endif
				_submoduleRegistry.ExecuteForEachSubmodule(
					(submoduleWrapper) => NotifyAddGroundItem(submoduleWrapper, newGroundItemPointer));
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}

		private static void NotifyAddGroundItem(SubmoduleProgramWrapper submodule, IntPtr newGroundItemPointer)
		{
			var newGroundItem = new GroundType(submodule.MQ2Dependencies.GetTypeFactory(), newGroundItemPointer);

			submodule.MQ2Dependencies.GetEventRegistry().NotifyAddGroundItem(newGroundItem);
		}



		private static readonly fMQSpawn _handleAddSpawn = HandleAddSpawn;
		private static void HandleAddSpawn(IntPtr newSpawnPointer)
		{
			try
			{
#if DEBUG
				_logger?.LogTracePrefixed("Method was called");
#endif
				_submoduleRegistry.ExecuteForEachSubmodule(
					(submoduleWrapper) => NotifyAddSpawn(submoduleWrapper, newSpawnPointer));
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}

		private static void NotifyAddSpawn(SubmoduleProgramWrapper submodule, IntPtr newSpawnPointer)
		{
			var newSpawn = new SpawnType(submodule.MQ2Dependencies.GetTypeFactory(), newSpawnPointer);

			submodule.MQ2Dependencies.GetEventRegistry().NotifyAddSpawn(newSpawn);
		}



		private static readonly fMQBeginZone _handleBeginZone = HandleBeginZone;
		private static void HandleBeginZone()
		{
			try
			{
#if DEBUG
				_logger?.LogTracePrefixed("Method was called");
#endif
				_submoduleRegistry.ExecuteForEachSubmodule(NotifyBeginZone);
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
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
				_logger?.LogTracePrefixed("Method was called");
#endif
				_submoduleRegistry.ExecuteForEachSubmodule(NotifyCleanUI);
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
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
					_logger?.LogTracePrefixed($"HandleDrawHUD fired. [DrawHUDCount: {drawHudCount}]");
				}

				if (drawHudCount > 1_000_000)
				{
					Interlocked.Exchange(ref drawHudCount, 0);
				}
#endif

				_submoduleRegistry.ExecuteForEachSubmodule(NotifyDrawHUD);
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
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
				_logger?.LogTracePrefixed("Method was called");
#endif

				_submoduleRegistry.ExecuteForEachSubmodule(NotifyEndZone);
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
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
				_logger?.LogTracePrefixed("Method was called");
#endif
				var chatLineEventArgs = new ChatLineEventArgs(chatLine, color);

				_submoduleRegistry.ExecuteForEachSubmodule(
					(submoduleWrapper) => NotifyIncomingChat(submoduleWrapper, chatLineEventArgs));
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);

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
				_logger?.LogErrorPrefixed(exc);
			}

			try
			{
				submodule.MQ2Dependencies.GetEventRegistry().NotifyChatAny(chatLineEventArgs);
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
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
					var removedProgramCount = _submoduleRegistry.ProcessRunningProgramTasks();
				}

				if ((pulseCount % 350) == 0)
				{
					_mq2CommandRegistry.ProcessAsyncCommandTasks();
				}

#if DEBUG
				if ((pulseCount % 10_000) == 0)
				{
					_logger?.LogDebugPrefixed($"(Pulse {pulseCount}) GC.GetTotalMemory(): {GC.GetTotalMemory(true)}");
				}
				else if ((pulseCount % 1_000) == 0)
				{
					_logger?.LogDebugPrefixed($"(Pulse {pulseCount}) GC.GetTotalMemory(): {GC.GetTotalMemory(false)}");
				}
#endif

				if (pulseCount > 1_000_000)
				{
#if DEBUG
					_logger?.LogTracePrefixed("Resetting pulse count to 1");
#endif

					Interlocked.Exchange(ref pulseCount, 1);
				}
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}



		private static readonly fMQReloadUI _handleReloadUI = HandleReloadUI;
		private static void HandleReloadUI()
		{
			try
			{
#if DEBUG
				_logger?.LogTracePrefixed("Method was called");
#endif
				_submoduleRegistry.ExecuteForEachSubmodule(NotifyReloadUI);
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
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
				_logger?.LogTracePrefixed("Method was called");
#endif
				_submoduleRegistry.ExecuteForEachSubmodule(
					(submoduleWrapper) => NotifyRemoveGroundItem(submoduleWrapper, removedGroundItemPointer));
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}

		private static void NotifyRemoveGroundItem(SubmoduleProgramWrapper submodule, IntPtr removedGroundItemPointer)
		{
			var removedGroundItem = new GroundType(submodule.MQ2Dependencies.GetTypeFactory(), removedGroundItemPointer);

			submodule.MQ2Dependencies.GetEventRegistry().NotifyRemoveGroundItem(removedGroundItem);
		}



		private static readonly fMQSpawn _handleRemoveSpawn = HandleRemoveSpawn;
		private static void HandleRemoveSpawn(IntPtr removedSpawnPointer)
		{
			try
			{
#if DEBUG
				_logger?.LogTracePrefixed("Method was called");
#endif

				_submoduleRegistry.ExecuteForEachSubmodule(
					(submoduleWrapper) => NotifyRemoveSpawn(submoduleWrapper, removedSpawnPointer));
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}

		private static void NotifyRemoveSpawn(SubmoduleProgramWrapper submodule, IntPtr removedSpawnPointer)
		{
			var removedSpawn = new SpawnType(submodule.MQ2Dependencies.GetTypeFactory(), removedSpawnPointer);

			submodule.MQ2Dependencies.GetEventRegistry().NotifyRemoveSpawn(removedSpawn);
		}




		private static readonly fMQSetGameState _handleSetGameState = HandleSetGameState;
		private static void HandleSetGameState(uint gameStateValue)
		{
			try
			{
#if DEBUG
				_logger?.LogTracePrefixed("Method was called");
#endif
				var gameState = Enum.IsDefined(typeof(GameState), gameStateValue)
					? (GameState)gameStateValue
					: GameState.Unknown;

				_submoduleRegistry.ExecuteForEachSubmodule(
					(submodule) => NotifySetGameState(submodule, gameState));
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
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
				_logger?.LogInformationPrefixed("Method was called!");

				_logger?.LogInformationPrefixed($"Disposing of the {nameof(SubmoduleRegistry)}...");
				CleanupHelper.TryDispose(_submoduleRegistry, _logger);

				_logger?.LogInformationPrefixed($"Disposing of the {nameof(MQ2CommandRegistry)}...");
				CleanupHelper.TryDispose(_mq2CommandRegistry, _logger);
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}



		private static readonly fMQWriteChatColor _handleWriteChatColor = HandleWriteChatColor;
		private static uint HandleWriteChatColor(string chatLine, uint color, uint filter)
		{
			try
			{
#if DEBUG
				_logger?.LogTracePrefixed("Method was called");
#endif
				var chatLineEventArgs = new ChatLineEventArgs(chatLine, color, filter);

				_submoduleRegistry.ExecuteForEachSubmodule(
					(submodule) => NotifyWriteChatColor(submodule, chatLineEventArgs));
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
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
				_logger?.LogErrorPrefixed(exc);
			}

			try
			{
				submodule.MQ2Dependencies.GetEventRegistry().NotifyChatAny(chatLineEventArgs);
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}



		private static readonly fMQZoned _handleZoned = HandleZoned;
		private static void HandleZoned()
		{
			try
			{
#if DEBUG
				_logger?.LogTracePrefixed("Method was called");
#endif
				_submoduleRegistry.ExecuteForEachSubmodule(NotifyZoned);
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}
		}

		private static void NotifyZoned(SubmoduleProgramWrapper submodule)
			=> submodule.MQ2Dependencies.GetEventRegistry().NotifyZoned(EventArgs.Empty);
#pragma warning restore RCS1057 // Add empty line between declarations.
	}
}
