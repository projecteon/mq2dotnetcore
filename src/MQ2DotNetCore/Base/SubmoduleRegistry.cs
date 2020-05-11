using MQ2DotNetCore.Logging;
using MQ2DotNetCore.MQ2Api;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.Base
{
	internal sealed class SubmoduleRegistry : CriticalFinalizerObject, IDisposable
	{
		internal static readonly SubmoduleRegistry Instance = new SubmoduleRegistry();

		private bool _isDisposed = false;
		private readonly object _lock = new object();

		private readonly ConcurrentDictionary<string, SubmoduleProgramWrapper> _programsDictionary
			= new ConcurrentDictionary<string, SubmoduleProgramWrapper>();

		private SubmoduleRegistry()
		{

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

		internal void PrintRunningPrograms()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			var allProgramNames = _programsDictionary.Keys.ToArray();
			foreach (var programName in allProgramNames)
			{
				try
				{
					if (!_programsDictionary.TryGetValue(programName, out var submoduleProgramWrapper)
						|| submoduleProgramWrapper == null)
					{
						FileLoggingHelper.LogWarning($"Failed to get program wrapper for name: {programName}");
						continue;
					}

					if (CleanupHelper.IsTaskStopped(submoduleProgramWrapper.Task))
					{
						if (!_programsDictionary.TryRemove(programName, out _))
						{
							FileLoggingHelper.LogWarning($"Failed to remove the completed program wrapper for name: {programName}");
						}

						CleanupHelper.TryDispose(submoduleProgramWrapper);
						continue;
					}

					if (CleanupHelper.IsTaskStopped(submoduleProgramWrapper.WrapperTask))
					{
						FileLoggingHelper.LogWarning($"The wrapper task is finished but the inner task is not for program name: {programName}");
						continue;
					}

					var ellapsedMilliseconds = (DateTime.Now - submoduleProgramWrapper.StartTime).TotalMilliseconds;
					FileLoggingHelper.LogDebug($"{programName} is currently running. [Ellapised Time: {ellapsedMilliseconds} ms ]");
					MQ2ChatWindow.Instance.WriteChatSafe($"{nameof(PrintRunningPrograms)}: {programName} is currently running. [Ellapised Time: {ellapsedMilliseconds} ms ]");
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError($"(programName: {programName}) encountered an exception:\n\n{exc}\n");
				}
			}
		}

		internal bool StartProgram(string submoduleProgramName, string[] commandArguments, MQ2Dependencies mq2Dependencies)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			AssemblyLoadContext? assemblyLoadContext = null;
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
					FileLoggingHelper.LogInformation($"A submodule program instance is already loaded/running with the name: {submoduleProgramName}");
					return false;
				}

				lock (_lock)
				{
					if (_programsDictionary.ContainsKey(submoduleProgramName))
					{
						FileLoggingHelper.LogInformation($"A submodule program instance is already loaded/running with the name: {submoduleProgramName}");
						return false;
					}


					var submoduleFilePath = Path.Combine(AssemblyInformation.AssemblyDirectory, "Programs", submoduleProgramName, $"{submoduleProgramName}.dll");
					if (!File.Exists(submoduleFilePath))
					{
						FileLoggingHelper.LogWarning($"Submodule program file not found: {submoduleFilePath}");
						return false;
					}

					assemblyLoadContext = new SubmoduleAssemblyLoadContext(submoduleProgramName, submoduleFilePath);
					var submoduleAssembly = assemblyLoadContext.LoadFromAssemblyPath(submoduleFilePath);

					var submoduleTypes = submoduleAssembly.GetTypes();
					var submoduleProgramTypes = submoduleTypes
						.Where(nextType => typeof(IMQ2Program).IsAssignableFrom(nextType))
						.ToList();

					FileLoggingHelper.LogInformation($"Found {submoduleProgramTypes.Count} {nameof(IMQ2Program)} types in the {submoduleProgramName} assembly: {string.Join(", ", submoduleProgramTypes.Select(type => type.FullName))}");

					var submoduleProgramClassType = submoduleProgramTypes
						.FirstOrDefault(nextType => nextType.Name.Contains(submoduleProgramName));

					if (submoduleProgramClassType == null && submoduleProgramTypes.Count > 0)
					{
						FileLoggingHelper.LogWarning($"Did not find a program type with name: {submoduleProgramName}, falling back to the first {nameof(IMQ2Program)} type found.");
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
						submoduleProgramName,
						submoduleProgramInstance,
						startTime,
						submoduleProgramTask,
						submoduleProgramWrapperTask
					);

					if (!_programsDictionary.TryAdd(submoduleProgramName, wrapper))
					{
						FileLoggingHelper.LogError($"Failed to add submodule program wrapper to the dictionary: {submoduleProgramName}");
					}
#pragma warning restore CA2000 // Dispose objects before losing scope

					return true;
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"Unexpected exception while trying to start submodule program: {submoduleProgramName}\n\n{exc}\n");

				CleanupHelper.TryCancel(cancellationTokenSource);
				CleanupHelper.TryDispose(cancellationTokenSource);
				CleanupHelper.TryDispose(submoduleProgramTask);
				CleanupHelper.TryDispose(submoduleProgramWrapperTask);

				if (submoduleProgramInstance is IDisposable disposableSubmoduleProgramInstance)
				{
					CleanupHelper.TryDispose(disposableSubmoduleProgramInstance);
				}

				// Do our best to cleanup if an exception is thrown
				CleanupHelper.TryUnload(assemblyLoadContext);

				return false;
			}
		}

		internal bool StopAllPrograms()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			FileLoggingHelper.LogInformation($"Stopping (disposing wrapper) of all submodule programs...");

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

				if (!_programsDictionary.ContainsKey(submoduleProgramName))
				{
					FileLoggingHelper.LogInformation($"A submodule program instance is not currently loaded/running with the name: {submoduleProgramName}");
					return false;
				}

				lock (_lock)
				{
					if (!_programsDictionary.Remove(submoduleProgramName, out var submoduleProgramWrapper)
						|| submoduleProgramWrapper == null)
					{
						FileLoggingHelper.LogInformation($"A submodule program instance is not currently loaded/running with the name: {submoduleProgramName}");
						return false;
					}

					FileLoggingHelper.LogInformation($"Stopping (disposing wrapper) of submodule with program name: {submoduleProgramName}");
					submoduleProgramWrapper.Dispose();

					return true;
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"Unexpected exception while trying to stop submodule program: {submoduleProgramName}\n\n{exc}\n");
				return false;
			}
		}

		internal async Task<TaskStatus?> TryStopProgramAsync(string submoduleProgramName)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(SubmoduleRegistry));
			}

			try
			{
				if (!_programsDictionary.TryGetValue(submoduleProgramName, out var submoduleProgramWrapper))
				{
					FileLoggingHelper.LogWarning($"Failed to find/retrieve submodule program wrapper for name: {submoduleProgramName}");
					return null;
				}

				var taskCancelStatus = await submoduleProgramWrapper.TryCancelAsync();
				FileLoggingHelper.LogDebug($"TryCancelAsync task status: {taskCancelStatus}");

				StopProgram(submoduleProgramName);
				FileLoggingHelper.LogDebug($"Done stopping program: {submoduleProgramName}");
				MQ2ChatWindow.Instance.WriteChatSafe($"Done stopping program: {submoduleProgramName}");

				return taskCancelStatus;
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
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

		private class SubmoduleProgramWrapper : IDisposable
		{
			private bool _isDisposed = false;

			public SubmoduleProgramWrapper(
				AssemblyLoadContext assemblyLoadContext,
				CancellationTokenSource cancellationTokenSource,
				string name,
				IMQ2Program programInstance,
				DateTime startTime,
				Task task,
				Task<Task> wrapperTask
			)
			{
				AssemblyLoadContext = assemblyLoadContext;
				CancellationTokenSource = cancellationTokenSource;
				Name = name;
				ProgramInstance = programInstance;
				StartTime = startTime;
				Task = task;
				WrapperTask = wrapperTask;
			}

			public AssemblyLoadContext? AssemblyLoadContext { get; private set; }
			public CancellationTokenSource? CancellationTokenSource { get; private set; }
			public string Name { get; private set; }
			public IMQ2Program? ProgramInstance { get; private set; }
			public DateTime StartTime { get; private set; }
			public Task? Task { get; private set; }
			public Task<Task>? WrapperTask { get; private set; }

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
				CleanupHelper.TryDispose(WrapperTask);

				if (ProgramInstance is IDisposable disposableProgramInstance)
				{
					CleanupHelper.TryDispose(disposableProgramInstance);
				}

				CleanupHelper.TryUnload(AssemblyLoadContext);
				AssemblyLoadContext = null;

				_isDisposed = true;
			}

			public async Task<TaskStatus> TryCancelAsync()
			{
				CleanupHelper.TryCancel(CancellationTokenSource);
				await Task.Delay(500);

				FileLoggingHelper.LogDebug($"Task status after first delay is {Task?.Status.ToString() ?? "(null)"}");
				FileLoggingHelper.LogDebug($"WrapperTask status after first delay is {WrapperTask?.Status.ToString() ?? "(null)"}");

				if (!CleanupHelper.IsTaskStopped(Task))
				{
					await Task.Delay(1000);
					FileLoggingHelper.LogDebug($"Task status after second delay is {Task?.Status.ToString() ?? "(null)"}");
					FileLoggingHelper.LogDebug($"WrapperTask status after second delay is {WrapperTask?.Status.ToString() ?? "(null)"}");
				}

				return Task?.Status ?? TaskStatus.RanToCompletion;
			}
		}
	}
}
