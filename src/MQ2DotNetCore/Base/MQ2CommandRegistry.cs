using MQ2DotNetCore.Interop;
using MQ2DotNetCore.Logging;
using MQ2DotNetCore.MQ2Api;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.Base
{
	public sealed class MQ2CommandRegistry : CriticalFinalizerObject, IDisposable
	{
		private readonly ConcurrentDictionary<string, List<MQ2AsyncCommandTaskWrapper>> _asyncCommandTasks =
			new ConcurrentDictionary<string, List<MQ2AsyncCommandTaskWrapper>>();

		// Hold onto a reference for the fEQCommand delegate in managed code so that it doesn't get garbage collected
		private readonly ConcurrentDictionary<string, MQ2Main.NativeMethods.fEQCommand> _commandsDictionary =
			new ConcurrentDictionary<string, MQ2Main.NativeMethods.fEQCommand>();

		private bool _isDisposed;

		private readonly MQ2SynchronizationContext _mq2SynchronizationContext;

		private readonly ConcurrentDictionary<string, List<string>> _submoduleCommandsDictionary =
			new ConcurrentDictionary<string, List<string>>();

		private readonly ConcurrentDictionary<string, CancellationTokenSource> _submoduleCancellationTokenSourceDictionary =
			new ConcurrentDictionary<string, CancellationTokenSource>();

		private readonly ConcurrentDictionary<string, DateTime> _synchronousCommandInProgressDictionary =
			new ConcurrentDictionary<string, DateTime>();

		internal MQ2CommandRegistry(MQ2SynchronizationContext mq2SynchronizationContext)
		{
			_mq2SynchronizationContext = mq2SynchronizationContext;
		}

		/// <inheritdoc />
		~MQ2CommandRegistry()
		{
			RemoveAllCommands();
			RemoveAllTasks();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			RemoveAllCommands();
			RemoveAllTasks();
			GC.SuppressFinalize(this);
			_isDisposed = true;
		}

		/// <summary>
		/// Delegate for an asynchronous command handler.
		/// </summary>
		/// <param name="commandArguments">The command arguments.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the async task.</param>
		public delegate Task AsyncCommand(string[] commandArguments, CancellationToken cancellationToken);

		/// <summary>
		/// Delegate for a synchronous command handler
		/// </summary>
		/// <param name="commandArguments">The command arguments.</param>
		public delegate void Command(string[] commandArguments);

		/// <summary>
		/// Add a new asynchronous command
		/// </summary>
		/// <param name="submoduleName">The name of the submodule to group the command under.</param>
		/// <param name="commandName">The /command name.</param>
		/// <param name="commandHandler">The delegate that will process execution of the /command.</param>
		/// <param name="eq"></param>
		/// <param name="parse"></param>
		/// <param name="inGame"></param>
		internal void AddAsyncCommand(string submoduleName, string commandName, AsyncCommand asyncCommandHandler, bool eq = false, bool parse = true, bool inGame = false)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(MQ2CommandRegistry));
			}

			FileLoggingHelper.LogInformation($"Attempting to register (asynchronous) command: {commandName}");

			AddCommandInternal(submoduleName, commandName, (characterSpawnIntPtr, commandArgumentsBuffer) =>
			{
				try
				{
					// Note: The characterSpawnIntPtr can/will change (e.g. character transitions to a new zone) so we don't want to hold
					// onto the value when the command starts. Instead we'll use the interop MQ2.GetCharacterSpawnIntPointer(..) method
					// to get the pointer on demand.;

					FileLoggingHelper.LogInformation($"Executing (asynchronous) command ({commandName}) with arguments: {commandArgumentsBuffer}");

					var arguments = StringHelper.SplitArguments(commandArgumentsBuffer).ToArray();

					// Like plugin API callbacks, command handlers get executed with our sync context set
					_mq2SynchronizationContext.SetExecuteAndRestore(() =>
					{
						CancellationTokenSource? asyncCommandCancellationTokenSource = null;
						CancellationTokenSource? linkedCancellationTokenSource = null;
						Task? asyncCommandTask = null;
						Task<Task>? wrapperTask = null;
						try
						{
							asyncCommandCancellationTokenSource = new CancellationTokenSource();
							var submoduleCancellationTokenSource = GetSubmoduleCancellationTokenSource(submoduleName);
							linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
								asyncCommandCancellationTokenSource.Token,
								submoduleCancellationTokenSource.Token
							);

							// When an async command is executed, instead of calling the handler directly, a task is created to await the handler
							// As I understand it, this basically means the handler will be posted as a continuation to MQ2SynchronizationContext,
							// and run on the next DoEvents()
							var startTime = DateTime.Now;
							asyncCommandTask = asyncCommandHandler(arguments, linkedCancellationTokenSource.Token);
							wrapperTask = Task.Factory.StartNew(
								async () => await asyncCommandTask,
								linkedCancellationTokenSource.Token,
								TaskCreationOptions.None,
								TaskScheduler.FromCurrentSynchronizationContext()
							);

							var asyncCommandWrapper = new MQ2AsyncCommandTaskWrapper(
								asyncCommandCancellationTokenSource,
								commandName,
								startTime,
								asyncCommandTask
							);

							AddAsyncCommandWrapperToDictionary(submoduleName, asyncCommandWrapper);
						}
						catch (Exception innerException)
						{
							CleanupHelper.TryCancel(asyncCommandCancellationTokenSource);
							CleanupHelper.TryDispose(asyncCommandCancellationTokenSource);
							CleanupHelper.TryDispose(linkedCancellationTokenSource);
							CleanupHelper.TryDispose(asyncCommandTask);
							CleanupHelper.TryDispose(wrapperTask);

							FileLoggingHelper.LogError($"{nameof(MQ2SynchronizationContext.SetExecuteAndRestore)}(..) encountered an exception:\n\n{innerException}\n");

							// This won't catch exceptions from the command handler, since that will get called from MQ2SynchronizationContext.DoEvents
							MQ2.WriteChatGeneralError($"Exception in {commandName}:");
							MQ2.WriteChatGeneralError(innerException.ToString());
						}
					});

				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError($"Exception in (async) {commandName}:\n\n{exc}\n");
					MQ2.WriteChatGeneralError($"Exception in (async) {commandName}: {exc}");
				}
			});
		}

		private void AddAsyncCommandWrapperToDictionary(string submoduleName, MQ2AsyncCommandTaskWrapper asyncCommandTaskWrapper)
		{
			try
			{
				if (_asyncCommandTasks.TryGetValue(submoduleName, out var wrapperListForSubmodule)
					&& wrapperListForSubmodule != null)
				{
#if DEBUG
					FileLoggingHelper.LogTrace($"Adding async command task wrapper for command {asyncCommandTaskWrapper.CommandName} to list for submodule: {submoduleName}");
#endif
					wrapperListForSubmodule.Add(asyncCommandTaskWrapper);
					return;
				}

				var newWrapperListForSubmodule = new List<MQ2AsyncCommandTaskWrapper>
				{
					asyncCommandTaskWrapper
				};

				if (_asyncCommandTasks.TryAdd(submoduleName, newWrapperListForSubmodule))
				{
					FileLoggingHelper.LogDebug($"Added async command task wrapper for command {asyncCommandTaskWrapper.CommandName} to a new list for submodule: {submoduleName}");
					return;
				}

				FileLoggingHelper.LogError($"Failed to add async command task wrapper for command {asyncCommandTaskWrapper.CommandName} to a new list for submodule: {submoduleName}");
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"Exception for submodule {submoduleName} and command {asyncCommandTaskWrapper?.CommandName}:\n\n{exc}\n");
			}
		}


		/// <summary>
		/// Add a new synchronous command to the registry. The commands are grouped by submodule name so that all of the commands
		/// in a submodule group can be cancelled/removed when a submodule is unloaded.
		/// </summary>
		/// <param name="submoduleName">The name of the submodule to group the command under.</param>
		/// <param name="commandName">The /command name.</param>
		/// <param name="commandHandler">The delegate that will process execution of the /command.</param>
		/// <param name="eq"></param>
		/// <param name="parse"></param>
		/// <param name="inGame"></param>
		internal void AddCommand(string submoduleName, string commandName, Command commandHandler, bool eq = false, bool parse = true, bool inGame = false)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(MQ2CommandRegistry));
			}

			FileLoggingHelper.LogInformation($"Attempting to register (synchronous) command: {commandName}");

			AddCommandInternal(submoduleName, commandName, (characterSpawnIntPtr, commandArgumentsBuffer) =>
			{
				try
				{
					// Note: The characterSpawnIntPtr can/will change (e.g. character transitions to a new zone) so we don't want to hold
					// onto the value when the command starts. Instead we'll use the interop MQ2.GetCharacterSpawnIntPointer(..) method
					// to get the pointer on demand.;

					FileLoggingHelper.LogInformation($"Executing (synchronous) command ({commandName}) with arguments: {commandArgumentsBuffer}");

					var arguments = StringHelper.SplitArguments(commandArgumentsBuffer).ToArray();
					_mq2SynchronizationContext.SetExecuteAndRestore(() =>
					{
						if (_synchronousCommandInProgressDictionary.TryGetValue(commandName, out var commandStartTime))
						{
							var message = $"Command ({commandName}) is currently in progress. Only one instance of a sync command may execute at a time. [In Progress Command Started At: {commandStartTime}]";
							FileLoggingHelper.LogWarning(message);
							MQ2.WriteChatGeneralError(message);
							return;
						}

						if (!_synchronousCommandInProgressDictionary.TryAdd(commandName, DateTime.Now))
						{
							var message = $"Unable to add command ({commandName}) start time to the dictionary. Only one instance of a sync command may execute at a time.";
							FileLoggingHelper.LogWarning(message);
							MQ2.WriteChatGeneralError(message);
							return;
						}

						try
						{
							commandHandler(arguments);
						}
						finally
						{
							var stopTime = DateTime.Now;
							if (_synchronousCommandInProgressDictionary.TryRemove(commandName, out var startTime))
							{
								var ellapsedTime = stopTime - startTime;
								FileLoggingHelper.LogInformation($"Synchronous command {commandName} stopped at {stopTime}. [Ellapsed Time: {ellapsedTime.TotalMilliseconds} ms]");
							}
							else
							{
								FileLoggingHelper.LogWarning($"Synchronous command {commandName} stopped at {stopTime}. [No start time available]");
							}
						}
					});
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError($"Exception in {commandName}:\n\n{exc}\n");
					MQ2.WriteChatGeneralError($"Exception in {commandName}: {exc}");
				}
			});
		}

		/// <summary>
		/// Adds a new command
		/// Note: this function will ensure the delegate is not garbage collected prior to RemoveCommand being called
		/// </summary>
		private void AddCommandInternal(string submoduleName, string commandName, MQ2Main.NativeMethods.fEQCommand commandHandler, bool eq = false, bool parse = true, bool inGame = false)
		{
			if (string.IsNullOrWhiteSpace(submoduleName))
			{
				throw new ArgumentNullException(nameof(submoduleName), "cannot be null, empty, or whitespace.");
			}

			if (!_commandsDictionary.TryAdd(commandName, commandHandler))
			{
				FileLoggingHelper.LogWarning($"{nameof(_commandsDictionary)}.TryAdd(..) was unsuccessful for command name: {commandName}");

				var duplicateKeyException = new InvalidOperationException($"A command is already registered for the specified command name");
				duplicateKeyException.Data["CommandName"] = commandName;
				throw duplicateKeyException;
			}

			AddCommandToSubmoduleDictionary(commandName, submoduleName);

			FileLoggingHelper.LogDebug($"Marshalling command handler delegate to native interop method for command name: {commandName}");
			MQ2Main.NativeMethods.MQ2AddCommand(commandName, commandHandler, eq, parse, inGame);
		}

		private void AddCommandToSubmoduleDictionary(string commandName, string submoduleName)
		{
			if (_submoduleCommandsDictionary.TryGetValue(submoduleName, out var submoduleCommandsList)
				&& submoduleCommandsList != null)
			{
				// Keep the command names grouped by submodule so we can remove all commands for a submodule
				// when it gets unloaded
#if DEBUG
				FileLoggingHelper.LogTrace($"Adding {commandName} to commands list in submodule dictionary for {submoduleName}");
#endif

				submoduleCommandsList.Add(commandName);
			}
			else
			{
#if DEBUG
				FileLoggingHelper.LogTrace($"Adding {commandName} to a new commands list in submodule dictionary for {submoduleName}");
#endif

				var newSubmoduleCommandsList = new List<string> { commandName };
				if (!_submoduleCommandsDictionary.TryAdd(submoduleName, newSubmoduleCommandsList))
				{
					// Failed to add list, try to remove the command from the dictionary and throw an error!
					_commandsDictionary.TryRemove(commandName, out _);
					var invalidOperationException = new InvalidOperationException("Unable to add submodule command list to the command lookup dictionary!");
					invalidOperationException.Data["SubmoduleName"] = submoduleName;
					invalidOperationException.Data["CommandName"] = commandName;
					throw invalidOperationException;
				}
			}
		}

		internal int CancelAllAsyncCommandTasks()
		{
			int cancelledTaskCount = 0;
			try
			{
				FileLoggingHelper.LogDebug($"Attempting to cancel all async command tasks...");

				foreach (var submoduleName in _asyncCommandTasks.Keys)
				{
					if (!_asyncCommandTasks.TryGetValue(submoduleName, out var asyncCommandTaskWrappersList)
						|| asyncCommandTaskWrappersList == null)
					{
						continue;
					}

					foreach (var asyncCommandTaskWrapper in asyncCommandTaskWrappersList)
					{
						try
						{
#if DEBUG
							FileLoggingHelper.LogTrace($"Cancelling task... [Submodule: {submoduleName}] [CommandName: {asyncCommandTaskWrapper.CommandName}]");
#endif

							asyncCommandTaskWrapper?.Cancel();
							++cancelledTaskCount;
						}
						catch (Exception cancelException)
						{
							FileLoggingHelper.LogError(cancelException);
						}
					}
				}

				if (cancelledTaskCount > 0)
				{
					try
					{
#if DEBUG
						FileLoggingHelper.LogTrace($"Running the sync context's DoEvents(true) method to give the cancelled tasks a chance to finish up...");
#endif
						_mq2SynchronizationContext.DoEvents(true);
					}
					catch (Exception doEventsException)
					{
						FileLoggingHelper.LogError(doEventsException);
					}
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}

			return cancelledTaskCount;
		}

		internal int CancelAsyncCommandTask(string nameToCancel)
		{
			int cancelledTaskCount = 0;
			try
			{
				var normalizedCommandNameToCancel = nameToCancel.StartsWith("/")
					? nameToCancel
					: $"/{nameToCancel}";

				FileLoggingHelper.LogDebug($"Attempting to cancel async command task(s) for command name: {normalizedCommandNameToCancel}...");

				foreach (var submoduleName in _asyncCommandTasks.Keys)
				{
					if (!_asyncCommandTasks.TryGetValue(submoduleName, out var asyncCommandTaskWrappersList)
						|| asyncCommandTaskWrappersList == null)
					{
						FileLoggingHelper.LogWarning($"Failed to get async command wrappers list for submodule: {submoduleName}...");
						continue;
					}

#if DEBUG
					FileLoggingHelper.LogTrace($"Async command task wrappers list ({submoduleName}) has {asyncCommandTaskWrappersList.Count} task wrapper instances");
#endif

					foreach (var asyncCommandTaskWrapper in asyncCommandTaskWrappersList)
					{
						try
						{
							if (asyncCommandTaskWrapper.CommandName != normalizedCommandNameToCancel)
							{
#if DEBUG
								FileLoggingHelper.LogTrace($"Async command name {asyncCommandTaskWrapper.CommandName} does not match {normalizedCommandNameToCancel}...");
#endif
								continue;
							}

#if DEBUG
							FileLoggingHelper.LogTrace($"Cancelling task... [Submodule: {submoduleName}] [CommandName: {asyncCommandTaskWrapper.CommandName}]");
#endif

							asyncCommandTaskWrapper?.Cancel();
							++cancelledTaskCount;

						}
						catch (Exception cancelException)
						{
							FileLoggingHelper.LogError(cancelException);
						}
					}

					if (cancelledTaskCount > 0)
					{
						try
						{
#if DEBUG
							FileLoggingHelper.LogTrace($"Running the sync context's DoEvents(true) method to give the cancelled tasks a chance to finish up...");
#endif

							_mq2SynchronizationContext.DoEvents(true);
						}
						catch (Exception doEventsException)
						{
							FileLoggingHelper.LogError(doEventsException);
						}
					}
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}

			return cancelledTaskCount;
		}

		private CancellationTokenSource GetSubmoduleCancellationTokenSource(string submoduleName)
		{
			if (_submoduleCancellationTokenSourceDictionary.TryGetValue(submoduleName, out var submoduleCancellationTokenSource))
			{
				return submoduleCancellationTokenSource;
			}

			CancellationTokenSource? newSubmoduleCancellationTokenSource = null;
			try
			{
				newSubmoduleCancellationTokenSource = new CancellationTokenSource();
				if (_submoduleCancellationTokenSourceDictionary.TryAdd(submoduleName, newSubmoduleCancellationTokenSource))
				{
					return newSubmoduleCancellationTokenSource;
				}

				throw new InvalidOperationException($"Unable to add new {nameof(CancellationTokenSource)} to the dictionary for submodule: {submoduleName}");
			}
			catch (Exception exc)
			{
				CleanupHelper.TryDispose(newSubmoduleCancellationTokenSource);
				FileLoggingHelper.LogError(exc);
				throw;
			}
		}

		internal void PrintRegisteredCommands()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(MQ2CommandRegistry));
			}

			try
			{
				var allRegisteredCommands = _commandsDictionary.Keys.ToArray();
				FileLoggingHelper.LogInformation($"All registered commands in {nameof(_commandsDictionary)}: {string.Join(", ", allRegisteredCommands)}");

				var allSubmoduleNames = _submoduleCommandsDictionary.Keys.ToArray();
				FileLoggingHelper.LogInformation($"All submodule names in {nameof(_submoduleCommandsDictionary)}: {string.Join(", ", allSubmoduleNames)}");

				foreach (var nextSubmoduleName in allSubmoduleNames)
				{
					if (!_submoduleCommandsDictionary.TryGetValue(nextSubmoduleName, out var submoduleCommandList))
					{
						FileLoggingHelper.LogWarning($"Failed to get submodule command list for submodule name: {nextSubmoduleName}");
						continue;
					}


					FileLoggingHelper.LogInformation($"All registered commands in submodule ({nextSubmoduleName}) list: {string.Join(", ", submoduleCommandList)}");
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		internal void PrintRunningCommands()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(MQ2CommandRegistry));
			}

			foreach (var commandName in _synchronousCommandInProgressDictionary.Keys)
			{
				try
				{
					if (!_synchronousCommandInProgressDictionary.TryGetValue(commandName, out var commandStartTime))
					{
						FileLoggingHelper.LogWarning($"Failed to get synchronous command start time from the dictionary: {commandName}");
						MQ2.Instance.WriteChatSafe($"  Synchronous command {commandName} is running.");
						continue;
					}

					var ellapsedMilliseconds = (DateTime.Now - commandStartTime).TotalMilliseconds;
					FileLoggingHelper.LogDebug($"Synchronous command {commandName} is running. [StartTime: {commandStartTime}] [Ellapsed: {ellapsedMilliseconds} ms ]");
					MQ2.Instance.WriteChatSafe($"  Synchronous command {commandName} is running. [StartTime: {commandStartTime}] [Ellapsed: {ellapsedMilliseconds} ms ]");
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError(exc);
				}
			}

			var processedAsyncCommandTasks = ProcessAsyncCommandTasks();

			foreach (var submoduleName in _asyncCommandTasks.Keys)
			{
				try
				{
					if (!_asyncCommandTasks.TryGetValue(submoduleName, out var submoduleWrapperList)
						|| submoduleWrapperList == null)
					{
						FileLoggingHelper.LogWarning($"Failed to get the submodule wrapper list for submodule name: {submoduleName}");
						continue;
					}

					var commandNameCount = new Dictionary<string, int>();
					foreach (var asyncCommandTaskWrapper in submoduleWrapperList)
					{
						var nextCommandName = asyncCommandTaskWrapper.CommandName;
						if (!commandNameCount.ContainsKey(nextCommandName))
						{
							commandNameCount[nextCommandName] = 0;
						}

						commandNameCount[nextCommandName] += 1;
						var commandNameWithIndex = $"{nextCommandName} (#{commandNameCount[nextCommandName]})";

						var ellapsedMilliseconds = (DateTime.Now - asyncCommandTaskWrapper.StartTime).TotalMilliseconds;
						var message = $"Async command {commandNameWithIndex} task is in progress. [StartTime: {asyncCommandTaskWrapper.StartTime}] [Ellapsed: {ellapsedMilliseconds} ms ] [TaskStatus: {asyncCommandTaskWrapper.Task.Status}]";
						FileLoggingHelper.LogDebug(message);
						MQ2.Instance.WriteChatSafe(message);
					}
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError(exc);
				}
			}
		}

		internal int ProcessAsyncCommandTasks()
		{
#if DEBUG
			FileLoggingHelper.LogTrace("Processing async command tasks...");
#endif

			var removedTaskCount = 0;
			foreach (var submoduleName in _asyncCommandTasks.Keys)
			{
				try
				{
					if (!_asyncCommandTasks.TryGetValue(submoduleName, out var submoduleWrapperList)
						|| submoduleWrapperList == null)
					{
						FileLoggingHelper.LogWarning($"Failed to get the submodule wrapper list for submodule name: {submoduleName}");
						continue;
					}

					var tasksToRemove = submoduleWrapperList.Where(asyncCommandTask => CleanupHelper.IsTaskStopped(asyncCommandTask.Task)).ToList();
					foreach (var asyncCommandTaskWrapper in tasksToRemove)
					{
						var taskStatus = asyncCommandTaskWrapper.Task.Status;
						submoduleWrapperList.Remove(asyncCommandTaskWrapper);

						++removedTaskCount;

#if DEBUG
						FileLoggingHelper.LogTrace($"Removed a completed task. [SubmoduleName: {submoduleName}] [CommandName: {asyncCommandTaskWrapper.CommandName}] [TaskStatus: {taskStatus}] [StartTime: {asyncCommandTaskWrapper.StartTime}]");
#endif

					}
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError(exc);
				}
			}

			return removedTaskCount;
		}

		private int RemoveAllCommands()
		{
			var count = 0;

			foreach (var command in _commandsDictionary.Keys)
			{
				try
				{
					MQ2Main.NativeMethods.MQ2RemoveCommand(command);
				}
				catch (Exception)
				{
					// Won't try logging here because this happens in critical finalizer code
				}

				count++;
			}

			_commandsDictionary.Clear();

			return count;
		}

		private void RemoveAllTasks()
		{
			try
			{
				foreach (var cancellationTokenSource in _submoduleCancellationTokenSourceDictionary.Values)
				{
					CleanupHelper.TryCancel(cancellationTokenSource);
					CleanupHelper.TryDispose(cancellationTokenSource);
				}
				_submoduleCancellationTokenSourceDictionary.Clear();

				foreach (var submoduleList in _asyncCommandTasks.Values)
				{
					foreach (var taskWrapper in submoduleList)
					{
						CleanupHelper.TryDispose(taskWrapper);
					}

					submoduleList.Clear();
				}
				_asyncCommandTasks.Clear();

				_submoduleCommandsDictionary.Clear();
				_synchronousCommandInProgressDictionary.Clear();
			}
			catch (Exception)
			{

			}
		}

		internal int RemoveCommandsForSubmodule(string submoduleName)
		{
			if (_submoduleCancellationTokenSourceDictionary.TryRemove(submoduleName, out var submoduleCancellationTokenSource))
			{
				FileLoggingHelper.LogInformation($"Cancelling all submodule async command tasks for: {submoduleName}");
				CleanupHelper.TryCancel(submoduleCancellationTokenSource);
				CleanupHelper.TryDispose(submoduleCancellationTokenSource);
			}

			if (!_submoduleCommandsDictionary.TryRemove(submoduleName, out var submoduleCommandsList)
				|| submoduleCommandsList == null)
			{
				FileLoggingHelper.LogError($"Unable to locate a list of submodule commands to remove for the submodule name: {submoduleName}");
				return 0;
			}

			var removedCount = 0;
			foreach (var submoduleCommandName in submoduleCommandsList)
			{
				try
				{
					TryRemoveCommand(submoduleCommandName, submoduleName);
					++removedCount;
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError($"Exception occurred attempting to unregister the command ({submoduleCommandName}) for submodule: {submoduleName}\n\n{exc}\n");
				}
			}

			return removedCount;
		}

		/// <summary>
		/// Removes a command, and removes the stored reference to the delegate if it was added from this plugin
		/// </summary>
		/// <param name="commandName">The name of the command to remove, including the slash e.g. "/echo"</param>
		/// <param name="submoduleName>The name of the submodule to remove the command from.</param>
		internal bool TryRemoveCommand(string commandName, string submoduleName)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(MQ2CommandRegistry));
			}

			FileLoggingHelper.LogInformation($"Attempting uregister command with name: {commandName}");

			if (_commandsDictionary.TryRemove(commandName, out var removedCommand))
			{
				if (!TryRemoveCommandNameFromSubmoduleDictionary(commandName, submoduleName))
				{
					FileLoggingHelper.LogWarning($"{nameof(TryRemoveCommandNameFromSubmoduleDictionary)}(..) returned false!");
				}

				FileLoggingHelper.LogInformation($"Executing MQ2RemoveCommand native interop call for command name: {commandName}");
				return MQ2Main.NativeMethods.MQ2RemoveCommand(commandName);
			}

			return false;
		}

		private bool TryRemoveCommandNameFromSubmoduleDictionary(string commandName, string submoduleName)
		{
			var wasRemovedFromSubmoduleDictionary = false;
			try
			{
				var allSubmoduleNames = _submoduleCommandsDictionary.Keys.ToArray();

#if DEBUG
				FileLoggingHelper.LogTrace($"All submodule names in {nameof(_submoduleCommandsDictionary)}: {string.Join(", ", allSubmoduleNames)}");
#endif

				foreach (var nextSubmoduleName in allSubmoduleNames)
				{
					if (!_submoduleCommandsDictionary.TryGetValue(nextSubmoduleName, out var submoduleCommandList))
					{
#if DEBUG
						FileLoggingHelper.LogTrace($"Failed to get submodule command list for submodule name: {nextSubmoduleName}");
#endif

						continue;
					}

					var wasRemoved = submoduleCommandList.Remove(commandName);
					if (!wasRemoved)
					{
#if DEBUG
						FileLoggingHelper.LogTrace($"Submodule command list did not contain ({commandName}), all commands in list: {string.Join(", ", submoduleCommandList)}");
#endif

						continue;
					}

					if (nextSubmoduleName == submoduleName)
					{
						wasRemovedFromSubmoduleDictionary = true;
						continue;
					}

					FileLoggingHelper.LogWarning($"Command was removed from a submodule list with a different name than expected. [Expected: {submoduleName}] [Actual: {nextSubmoduleName}]");
				}
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}

			return wasRemovedFromSubmoduleDictionary;
		}
	}

}
