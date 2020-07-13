using Microsoft.Extensions.Logging;
using MQ2DotNetCore.Logging;
using MQ2DotNetCore.MQ2Api;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.Base
{
	internal sealed class SubmoduleRegistry : CriticalFinalizerObject, IDisposable
	{
		private bool _isDisposed = false;
		private readonly object _lock = new object();
		private readonly ILogger<SubmoduleRegistry>? _logger;
		private MQ2 _mq2Instance;

		private readonly ConcurrentDictionary<string, SubmoduleProgramWrapper> _programsDictionary
			= new ConcurrentDictionary<string, SubmoduleProgramWrapper>();

		private readonly ILogger<SubmoduleAssemblyLoadContext>? _submoduleAssemblyLoadContextLogger;
		private readonly ILogger<SubmoduleProgramWrapper>? _submoduleProgramWrapperLogger;

		internal SubmoduleRegistry(
			ILogger<SubmoduleRegistry>? logger,
			MQ2 mq2Instance,
			ILogger<SubmoduleAssemblyLoadContext>? submoduleAssemblyLoadContextLogger,
			ILogger<SubmoduleProgramWrapper>? submoduleProgramWrapperLogger
		)
		{
			_mq2Instance = mq2Instance ?? throw new ArgumentNullException(nameof(mq2Instance));

			_logger = logger;
			_submoduleAssemblyLoadContextLogger = submoduleAssemblyLoadContextLogger;
			_submoduleProgramWrapperLogger = submoduleProgramWrapperLogger;
		}

		// Finalizer
		~SubmoduleRegistry()
		{
			UnloadAll();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			UnloadAll();
			GC.SuppressFinalize(this);
			_isDisposed = true;
		}

		internal void ExecuteForEachSubmodule(Action<SubmoduleProgramWrapper> actionToInvoke)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			if (actionToInvoke == null)
			{
				throw new ArgumentNullException(nameof(actionToInvoke));
			}

			var submoduleNames = _programsDictionary.Keys.ToArray();
			foreach (var submoduleName in submoduleNames)
			{
				try
				{
					if (!_programsDictionary.TryGetValue(submoduleName, out var submoduleWrapper)
						|| submoduleName == null)
					{
						_logger?.LogWarningPrefixed($"Unable to retrieve the submodule wrapper for: {submoduleName}");
						continue;
					}

					actionToInvoke.Invoke(submoduleWrapper);
				}
				catch (Exception exc)
				{
					_logger?.LogErrorPrefixed(exc);
				}
			}
		}

		internal void PrintRunningPrograms()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			var allProgramNames = _programsDictionary.Keys.ToArray();

			_logger?.LogDebugPrefixed($"All program names: {string.Join(", ", allProgramNames)}");

			foreach (var programName in allProgramNames)
			{
				try
				{
					if (!_programsDictionary.TryGetValue(programName, out var submoduleProgramWrapper)
						|| submoduleProgramWrapper == null)
					{
						_logger?.LogWarningPrefixed($"Failed to get program wrapper for name: {programName}");
						continue;
					}

					if (CleanupHelper.IsTaskStopped(submoduleProgramWrapper.Task))
					{
						if (!_programsDictionary.TryRemove(programName, out _))
						{
							_logger?.LogWarningPrefixed($"Failed to remove the completed program wrapper for name: {programName}");
						}

						_logger?.LogDebugPrefixed($"Attempting to dispose submodule program wrapper with stopped task: {submoduleProgramWrapper}");

						CleanupHelper.TryDispose(submoduleProgramWrapper, _logger);
						continue;
					}

					var ellapsedMilliseconds = (DateTime.Now - submoduleProgramWrapper.StartTime).TotalMilliseconds;
					_logger?.LogDebugPrefixed($"{programName} is currently running. [Ellapised Time: {ellapsedMilliseconds} ms ]");
					_mq2Instance.WriteChatSafe($"{nameof(PrintRunningPrograms)}: {programName} is currently running. [Ellapised Time: {ellapsedMilliseconds} ms ]");
				}
				catch (Exception exc)
				{
					_logger?.LogErrorPrefixed($"(programName: {programName}) encountered an exception:\n\n{exc}\n");
				}
			}
		}

		internal int ProcessRunningProgramTasks()
		{
			var removedTaskCount = 0;

			try
			{
				var allProgramNames = _programsDictionary.Keys.ToArray();

#if DEBUG
				_logger?.LogTracePrefixed($"Processing running program tasks.  All program names: {string.Join(", ", allProgramNames)}");
#endif

				foreach (var programName in allProgramNames)
				{
					try
					{
						if (!_programsDictionary.TryGetValue(programName, out var submoduleProgramWrapper)
							|| submoduleProgramWrapper == null)
						{
							_logger?.LogWarningPrefixed($"Failed to get program wrapper for name: {programName}");
							continue;
						}

						var isProgramTaskStopped = CleanupHelper.IsTaskStopped(submoduleProgramWrapper.Task);
						if (isProgramTaskStopped || submoduleProgramWrapper.HasCancelled)
						{
							if (isProgramTaskStopped)
							{
								_logger?.LogWarningPrefixed($"{programName} has been cancelled but it's Task has not stopped! Attempting to forcibly cancel and unload...");
							}

							if (!_programsDictionary.TryRemove(programName, out _))
							{
								_logger?.LogWarningPrefixed($"Failed to remove the completed program wrapper for name: {programName}");
							}

#if DEBUG
							_logger?.LogTracePrefixed($"Attempting to dispose submodule program wrapper with stopped task: {submoduleProgramWrapper}");
#endif

							CleanupHelper.TryDispose(submoduleProgramWrapper, _logger);
							++removedTaskCount;
							continue;
						}

#if DEBUG
						_logger?.LogTracePrefixed($"{programName} is running...");
#endif

					}
					catch (Exception exc)
					{
						_logger?.LogErrorPrefixed($"(programName: {programName}) encountered an exception:\n\n{exc}\n");
					}
				}
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
			}

			return removedTaskCount;
		}

		internal bool StartProgram(string submoduleProgramName, string[] commandArguments, MQ2Dependencies mq2Dependencies)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			SubmoduleAssemblyLoadContext? assemblyLoadContext = null;
			CancellationTokenSource? cancellationTokenSource = null;
			IMQ2Program? submoduleProgramInstance = null;
			Task? submoduleProgramTask = null;
			Task<Task>? submoduleProgramWrapperTask = null;
			try
			{
				if (string.IsNullOrWhiteSpace(submoduleProgramName))
				{
					throw new ArgumentNullException(nameof(submoduleProgramName), "cannot be null, empty, or whitespace.");
				}

				if (_programsDictionary.ContainsKey(submoduleProgramName))
				{
					_logger?.LogInformationPrefixed($"A submodule program instance is already loaded/running with the name: {submoduleProgramName}");
					return false;
				}

				lock (_lock)
				{
					if (_programsDictionary.ContainsKey(submoduleProgramName))
					{
						_logger?.LogInformationPrefixed($"A submodule program instance is already loaded/running with the name: {submoduleProgramName}");
						return false;
					}


					var submoduleFilePath = Path.Combine(MQ2DotNetCoreAssemblyInformation.AssemblyDirectory, "Programs", submoduleProgramName, $"{submoduleProgramName}.dll");
					if (!File.Exists(submoduleFilePath))
					{
						_logger?.LogWarningPrefixed($"Submodule program file not found: {submoduleFilePath}");
						return false;
					}

					assemblyLoadContext = new SubmoduleAssemblyLoadContext(_submoduleAssemblyLoadContextLogger, submoduleProgramName, submoduleFilePath);

					_logger?.LogDebug($"Loading submodule assembly from: {submoduleFilePath}");

					var submoduleAssembly = assemblyLoadContext.LoadFromAssemblyPath(submoduleFilePath);
					//TryLoadDefaultAssemblies(assemblyLoadContext, submoduleProgramName);

					_logger?.LogDebug("Looking for IMQ2Program implementation types in loaded submodule assembly");

					var submoduleTypes = submoduleAssembly.GetTypes();
					var submoduleProgramTypes = submoduleTypes
						.Where(nextType => typeof(IMQ2Program).IsAssignableFrom(nextType))
						.ToList();

					_logger?.LogInformationPrefixed($"Found {submoduleProgramTypes.Count} {nameof(IMQ2Program)} types in the {submoduleProgramName} assembly: {string.Join(", ", submoduleProgramTypes.Select(type => type.FullName))}");

					var submoduleProgramClassType = submoduleProgramTypes
						.FirstOrDefault(nextType => nextType.Name.Contains(submoduleProgramName));

					if (submoduleProgramClassType == null && submoduleProgramTypes.Count > 0)
					{
						_logger?.LogWarningPrefixed($"Did not find a program type with name: {submoduleProgramName}, falling back to the first {nameof(IMQ2Program)} type found.");
						submoduleProgramClassType = submoduleProgramTypes.FirstOrDefault();
					}

					if (submoduleProgramClassType == null)
					{
						throw new InvalidOperationException($"Failed to locate type {submoduleProgramName} within the assembly!");
					}

					if (!submoduleProgramClassType.IsClass
						|| submoduleProgramClassType.IsAbstract
						|| !typeof(IMQ2Program).IsAssignableFrom(submoduleProgramClassType))
					{
						throw new InvalidOperationException($"Submodule program type {submoduleProgramClassType.FullName} must be a non-abstract class that implements the {nameof(IMQ2Program)} interface!");
					}

					var submoduleProgramConstructor = submoduleProgramClassType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
						.FirstOrDefault(constructor => constructor.GetParameters().Length == 0);

					if (submoduleProgramConstructor == null)
					{
						throw new InvalidOperationException($"Submodule program type {submoduleProgramClassType.FullName} must have a public default constructor (a constructor that does not require parameters)!");
					}

					submoduleProgramInstance = (IMQ2Program)submoduleProgramConstructor.Invoke(null);

#pragma warning disable CA2000 // Dispose objects before losing scope
					cancellationTokenSource = new CancellationTokenSource();

					var startTime = DateTime.Now;

					// TODO: We currently run all the programs using the task scheduler from our synchronization context
					// It would be nice if we could write all of the MQ2 api's that we provide as async methods that run
					// on the sync context and run the program logic that's not an MQ2 call through run on task pool
					// threads. 
					submoduleProgramTask = submoduleProgramInstance.RunAsync(commandArguments, mq2Dependencies, cancellationTokenSource.Token);
					submoduleProgramWrapperTask = Task.Factory.StartNew(
						async () => await submoduleProgramTask,
						cancellationTokenSource.Token,
						TaskCreationOptions.None,
						TaskScheduler.FromCurrentSynchronizationContext()
					);

					var wrapper = new SubmoduleProgramWrapper(
						assemblyLoadContext,
						cancellationTokenSource,
						_submoduleProgramWrapperLogger,
						mq2Dependencies,
						submoduleProgramName,
						submoduleProgramInstance,
						startTime,
						submoduleProgramTask
					);

					if (!_programsDictionary.TryAdd(submoduleProgramName, wrapper))
					{
						_logger?.LogErrorPrefixed($"Failed to add submodule program wrapper to the dictionary: {submoduleProgramName}");
					}
#pragma warning restore CA2000 // Dispose objects before losing scope

					return true;
				}
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed($"Unexpected exception while trying to start submodule program: {submoduleProgramName}\n\n{exc}\n");

				CleanupHelper.TryCancel(cancellationTokenSource, _logger);
				CleanupHelper.TryDispose(cancellationTokenSource, _logger);
				CleanupHelper.TryDispose(submoduleProgramTask, _logger);
				CleanupHelper.TryDispose(submoduleProgramWrapperTask, _logger);

				if (submoduleProgramInstance is IDisposable disposableSubmoduleProgramInstance)
				{
					CleanupHelper.TryDispose(disposableSubmoduleProgramInstance, _logger);
				}

				// Do our best to cleanup if an exception is thrown
				CleanupHelper.TryUnload(assemblyLoadContext, _logger);

				return false;
			}
		}

		internal bool StopAllPrograms()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			_logger?.LogInformationPrefixed($"Stopping (disposing wrapper) of all submodule programs...");

			var stoppedAllSuccessfully = true;
			foreach (var submoduleProgramName in _programsDictionary.Keys)
			{
				stoppedAllSuccessfully &= StopProgram(submoduleProgramName);
			}

			return stoppedAllSuccessfully;
		}

		internal bool StopProgram(string submoduleProgramName)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			try
			{
				if (string.IsNullOrWhiteSpace(submoduleProgramName))
				{
					throw new ArgumentNullException(nameof(submoduleProgramName), "cannot be null, empty, or whitespace.");
				}

				lock (_lock)
				{
					if (!_programsDictionary.TryRemove(submoduleProgramName, out var submoduleProgramWrapper)
						|| submoduleProgramWrapper == null)
					{
						_logger?.LogInformationPrefixed($"(inside lock) A submodule program instance is not currently loaded/running with the name: {submoduleProgramName}");
						return false;
					}

					_logger?.LogInformationPrefixed($"Stopping (disposing wrapper) of submodule with program name: {submoduleProgramName}");
					submoduleProgramWrapper.Dispose();

					return true;
				}
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed($"Unexpected exception while trying to stop submodule program: {submoduleProgramName}\n\n{exc}\n");
				return false;
			}
		}


		//		internal void TryLoadDefaultAssemblies(SubmoduleAssemblyLoadContext submoduleAssemblyLoadContext, string submoduleProgramName)
		//		{
		//#if DEBUg
		//			var defaultAssembliesBuilder = new StringBuilder("AssemblyLoadContext.Default.Assemblies: \n");
		//			foreach (var nextAssembly in AssemblyLoadContext.Default.Assemblies)
		//			{
		//				defaultAssembliesBuilder.Append('\n');
		//				defaultAssembliesBuilder.Append(nextAssembly.GetName().ToString());

		//				try
		//				{
		//					defaultAssembliesBuilder.Append("    [Location: ").Append(nextAssembly.Location).Append(']');
		//				}
		//#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
		//				catch (Exception)
		//#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
		//				{
		//					// Can ignore
		//				}
		//			}

		//			defaultAssembliesBuilder.Append('\n');

		//			_logger?.LogTracePrefixed(defaultAssembliesBuilder.ToString());
		//#endif


		//			foreach (var defaultAssemblyPath in _defaultAssemblies)
		//			{
		//				if (string.IsNullOrWhiteSpace(defaultAssemblyPath))
		//				{
		//					continue;
		//				}

		//				var defaultAssemblyName = Path.GetFileNameWithoutExtension(defaultAssemblyPath);
		//				if (submoduleAssemblyLoadContext.Assemblies
		//					.Any(loadedAssembly => loadedAssembly.FullName?.Contains(defaultAssemblyName, StringComparison.OrdinalIgnoreCase) == true))
		//				{
		//					_logger.LogDebug($"{defaultAssemblyName} already loaded, skipping");
		//					continue;
		//				}

		//				try
		//				{
		//					_logger?.LogDebugPrefixed($"Attempting to load default assembly {defaultAssemblyPath} into the {nameof(SubmoduleAssemblyLoadContext)} for {submoduleProgramName}");
		//					submoduleAssemblyLoadContext.LoadFromAssemblyName
		//				}
		//				catch (Exception exc)
		//				{
		//					_logger?.LogWarningPrefixed(exc);
		//				}
		//			}
		//		}

		internal async Task<TaskStatus?> TryStopProgramAsync(string submoduleProgramName)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			try
			{
				await Task.Yield();

				if (!_programsDictionary.TryGetValue(submoduleProgramName, out var submoduleProgramWrapper))
				{
					_logger?.LogWarningPrefixed($"Failed to find/retrieve submodule program wrapper for name: {submoduleProgramName}");
					return null;
				}

				var taskCancelStatus = await submoduleProgramWrapper.TryCancelAsync();
				_logger?.LogDebugPrefixed($"TryCancelAsync task status: {taskCancelStatus}");

				StopProgram(submoduleProgramName);
				_logger?.LogDebugPrefixed($"Done stopping program: {submoduleProgramName}");
				_mq2Instance.WriteChatSafe($"Done stopping program: {submoduleProgramName}");

				return taskCancelStatus;
			}
			catch (Exception exc)
			{
				_logger?.LogErrorPrefixed(exc);
				return null;
			}
		}

		private void UnloadAll()
		{
			if (_programsDictionary == null)
			{
				return;
			}

			foreach (var submoduleProgramWrapper in _programsDictionary.Values)
			{
				try
				{
					submoduleProgramWrapper.Dispose();
				}
				catch (Exception)
				{
					// Not going to try to log here since this happens in critical finalizer code
				}
			}

			_programsDictionary.Clear();
		}
	}
}
