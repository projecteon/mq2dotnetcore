using MQ2DotNet.MQ2API;
using MQ2DotNetCore.Base;
using MQ2DotNetCore.Interop;
using MQ2DotNetCore.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.MQ2Api
{
	public sealed class MQ2CommandRegistry : CriticalFinalizerObject, IDisposable
	{
		// Hold onto a reference for the fEQCommand delegate in managed code so that it doesn't get garbage collected
		private readonly ConcurrentDictionary<string, MQ2Main.NativeMethods.fEQCommand> _commandsDictionary =
			new ConcurrentDictionary<string, MQ2Main.NativeMethods.fEQCommand>();

		private readonly ConcurrentDictionary<string, CancellationTokenSource> _submoduleCancellationTokenSourceDictionary =
			new ConcurrentDictionary<string, CancellationTokenSource>();

		private readonly ConcurrentDictionary<string, List<string>> _submoduleCommandsDictionary =
			new ConcurrentDictionary<string, List<string>>();

		private readonly ConcurrentDictionary<string, List<MQ2AsyncCommandTaskWrapper>> _submoduleInProgressCommandsDictionary
			= new ConcurrentDictionary<string, List<MQ2AsyncCommandTaskWrapper>>();

		private bool _isDisposed;

		internal MQ2CommandRegistry()
		{
			// TODO: Determine if there is any reason for the complexity of the synchronization context that
			// they were using...
		}

		/// <inheritdoc />
		~MQ2CommandRegistry()
		{
			RemoveAllCommands();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			RemoveAllCommands();
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

					FileLoggingHelper.LogInformation($"Executing (synchronous) command ({commandName}) with arguments: {commandArgumentsBuffer}");

					// TODO: Determine why the characterSpawnIntPtr is being thrown away here, and why it's being
					// fetched manually using GetCharSpawn()...
					LogCharacterSpawnPointerValues(characterSpawnIntPtr);

					var arguments = StringHelper.SplitArguments(commandArgumentsBuffer).ToArray();

					// TODO: Determine if there is any need for a sync context, for now we'll just queue it using the default
					// task schedule (ThreadPool) skip awaiting it, and let the global event handler log any unhandled task

					//CancellationTokenSource cancellationTokenSourceForSubmodule;
					//if (_submoduleCancellationTokenSourceDictionary.TryGetValue(submoduleName, out var cancellationTokenSource))
					//{
					//	cancellationTokenSourceForSubmodule = cancellationTokenSource;
					//}
					//else
					//{
					//	cancellationTokenSourceForSubmodule = new CancellationTokenSource();
					//	if (!_submoduleCancellationTokenSourceDictionary.TryAdd(submoduleName, cancellationTokenSourceForSubmodule))
					//	{
					//		FileLoggingHelper.LogError($"Unable to add the cancellation token source to the dictionary for the submodule: {submoduleName}");
					//		return;
					//	}
					//}


					var threadpoolTask = Task.Run(() => asyncCommandHandler(arguments, CancellationToken.None));

					//Task.Factory.StartNew(async () => await task, CancellationToken.None, TaskCreationOptions.None,
					//			TaskScheduler.FromCurrentSynchronizationContext());
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError($"Exception in {commandName}:\n\n{exc.ToString()}\n");
					//MQ2.WriteChatGeneralError($"Exception in {command}: {e}");
				}
			});
		}

		private static void LogCharacterSpawnPointerValues(IntPtr characterSpawnIntPtr)
		{
			try
			{
				FileLoggingHelper.LogInformation($"Character Spawn Int Pointer From Parameter: {characterSpawnIntPtr}");
				try
				{
					var readOnce = Marshal.ReadIntPtr(characterSpawnIntPtr);
					FileLoggingHelper.LogInformation($"Character Spawn Int Pointer From Parameter (Read Once): {readOnce}");

					var readTwice = Marshal.ReadIntPtr(readOnce);
					FileLoggingHelper.LogInformation($"Character Spawn Int Pointer From Parameter (Read Twice): {readTwice}");
				}
				catch (Exception readPointerException)
				{
					FileLoggingHelper.LogWarning($"{nameof(LogCharacterSpawnPointerValues)} threw an exception while attempting to read the int pointer:\n\n{readPointerException.ToString()}\n");
				}

				var characterSpawnIntegerPointer2 = MQ2.GetCharacterSpawnIntPointer();
				FileLoggingHelper.LogInformation($"Character Spawn Int Pointer From GetCharacterSpawnIntPointer(..): {characterSpawnIntPtr}");
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"{nameof(LogCharacterSpawnPointerValues)} threw an exception:\n\n{exc.ToString()}\n");
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
					// TODO: Determine why the characterSpawnIntPtr is being thrown away here, and why it's being
					// fetched manually using GetCharSpawn()...

					FileLoggingHelper.LogInformation($"Executing (synchronous) command ({commandName}) with arguments: {commandArgumentsBuffer}");

					// TODO: Determine if there is any need for a sync context
					var arguments = StringHelper.SplitArguments(commandArgumentsBuffer).ToArray();
					commandHandler(arguments);
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError($"Exception in {commandName}:\n\n{exc.ToString()}\n");
					//MQ2.WriteChatGeneralError($"Exception in {command}: {e}");
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


			if (_submoduleCommandsDictionary.TryGetValue(submoduleName, out var submoduleCommandsList)
				&& submoduleCommandsList != null)
			{
				// Keep the command names grouped by submodule so we can remove all commands for a submodule
				// when it gets unloaded
				submoduleCommandsList.Add(commandName);
			}
			else
			{
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

			FileLoggingHelper.LogDebug($"Marshalling command handler delegate to native interop method for command name: {commandName}");
			MQ2Main.NativeMethods.MQ2AddCommand(commandName, commandHandler, eq, parse, inGame);
		}

		private int RemoveCommandsForSubmodule(string submoduleName)
		{
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
					MQ2Main.NativeMethods.MQ2RemoveCommand(submoduleCommandName);
					++removedCount;
				}
				catch (Exception exc)
				{
					FileLoggingHelper.LogError($"Exception occurred attempting to unregister the command ({submoduleCommandName}) for submodule: {submoduleName}\n\n{exc.ToString()}");
				}
			}


			return removedCount;
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
				catch (Exception exc)
				{
					// Won't try logging here because this happens in critical finalizer code
				}

				count++;
			}

			_commandsDictionary.Clear();

			return count;
		}

		/// <summary>
		/// Removes a command, and removes the stored reference to the delegate if it was added from this plugin
		/// </summary>
		/// <param name="commandName">The name of the command to remove, including the slash e.g. "/echo"</param>
		public bool TryRemoveCommand(string commandName)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(MQ2CommandRegistry));
			}

			FileLoggingHelper.LogInformation($"Attempting uregister command with name: {commandName}");

			if (_commandsDictionary.TryRemove(commandName, out var removedCommand))
			{
				FileLoggingHelper.LogInformation($"Executing MQ2RemoveCommand native interop call for command name: {commandName}");
				return MQ2Main.NativeMethods.MQ2RemoveCommand(commandName);
			}

			return false;
		}
	}
}
