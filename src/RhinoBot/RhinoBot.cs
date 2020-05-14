using MQ2DotNetCore;
using MQ2DotNetCore.Logging;
using MQ2DotNetCore.MQ2Api;
using Nito.AsyncEx;
using RhinoBot.Base;
using RhinoBot.Configs;
using RhinoBot.GroupHelpers;
using RhinoBot.LocationHelpers;
using RhinoBot.MissionHelpers;
using RhinoBot.ToonHelpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RhinoBot
{
	public class RhinoBot : IMQ2Program
	{
		private string _controlToonName = null!;
		private MQ2Dependencies _mq2Dependencies = null!; // nullability hack, Initialize will always set this

		// Exposed mq2 dependencies directrly for convenience
		public ChatUtilities Chat => _mq2Dependencies.GetChat();
		public string ControlToonName => _controlToonName;
		public MQ2SubmoduleEventRegistry EventRegistry => _mq2Dependencies.GetEventRegistry();
		public MQ2 Mq2 => _mq2Dependencies.GetMQ2();
		public MQ2Spawns Spawns => _mq2Dependencies.GetSpawns();
		public MQ2Tlo Tlo => _mq2Dependencies.GetTlo();


		// Bot singletons
		public readonly ConfigLoader Configs;
		public readonly GroupCommands GroupCommands;
		public readonly LocationCommands LocationCommands;
		public readonly MissionCommands MissionCommands;
		public readonly ToonCommands ToonCommands;

		public RhinoBot()
		{
			Configs = new ConfigLoader(this, true);
			GroupCommands = new GroupCommands(this, true);
			LocationCommands = new LocationCommands(this, true);
			MissionCommands = new MissionCommands(this, true);
			ToonCommands = new ToonCommands(this, true);
		}


		private void Initialize(MQ2Dependencies mq2Dependencies)
		{
			_mq2Dependencies = mq2Dependencies ?? throw new ArgumentNullException(nameof(mq2Dependencies));

			var controlToonName = mq2Dependencies.GetTlo().Me?.Name;
			if (string.IsNullOrWhiteSpace(controlToonName))
			{
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
				throw new ArgumentNullException("cannot be null, empty, or whitespace.", "_mq2Dependencies.GetTlo().Me.Name");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
			}

			_controlToonName = controlToonName;
		}

		public async Task RunAsync(string[] commandArguments, MQ2Dependencies mq2Dependencies, CancellationToken cancellationToken)
		{
			try
			{
				Initialize(mq2Dependencies);

				Mq2.WriteChatSafe($"\ag[{nameof(RhinoBot)}]\aw {nameof(RunAsync)}(..) is executing...");

				//Events.OnChatMQ2 += (s, e) =>
				//{
				//	if (e == "[MQ2] Hello")
				//		Mq2.WriteChat("Hello yourself");
				//};

				// All commands should be registered / unregistered here so we won't bother exposing the command registry outside of
				// this method
				var commandRegistry = _mq2Dependencies.GetCommandRegistry();
				commandRegistry.AddAsyncCommand("/formgroup", GroupCommands.FormGroupAsync);
				commandRegistry.AddAsyncCommand("/navto", LocationCommands.NavigateToLocationAsync);
				commandRegistry.AddCommand("/reloadconfigs", Configs.ReloadAll);
				commandRegistry.AddAsyncCommand("/runmission", MissionCommands.RunMissionAsync);

				while (cancellationToken != null && !cancellationToken.IsCancellationRequested)
				{
					await Task.Delay(500, cancellationToken);
				}

				commandRegistry.TryRemoveCommand("/formgroup");
				commandRegistry.TryRemoveCommand("/navto");
				commandRegistry.TryRemoveCommand("/reloadconfigs");
				commandRegistry.TryRemoveCommand("/runmission");

				Mq2.WriteChatSafe($"\ag[{nameof(RhinoBot)}]\aw {nameof(RunAsync)}(..) is exiting.");
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError(exc);
			}
		}

		public static string GetConfigFilePath(string configFileName)
			=> Path.Combine(Path.GetDirectoryName(RhinoBotConstants.AssemblyLocation) ?? string.Empty, "Configs", configFileName);

		public async Task<string?> ParseVariablesOnRemoteToonAsync(
				string toonName,
				string variablePrefix,
				string variableValuesExpression,
				CancellationToken cancellationToken
			)
		{
			if (toonName == ControlToonName)
			{
				var parseResult = Mq2.Parse(variableValuesExpression);

				//Mq2.DoCommand($"/noparse /echo Parse result for {variableValuesExpression} is {parseResult}");

				return parseResult;
			}

			// TODO: Try configuring DanNet and make this faster (use observables?)

			string? variablesResult = null;
			bool wasFound = false;
			try
			{
				//Mq2.WriteChatSafe("Starting wait for MQ2 chat task");

				//var logFilePath = Path.Combine(Path.GetDirectoryName(RhinoBotConstants.AssemblyLocation), "debug.log");

				Predicate<string> doesChatLineMatch = (chatLine =>
				{
					if (wasFound)
					{
						return true;
					}

					var chatLinePrefix = $"/echo {variablePrefix}=";
					var valueStartIndex = chatLine.IndexOf(chatLinePrefix);
					if (valueStartIndex < 0)
					{
						//File.AppendAllText(logFilePath, $"Chat line not a match:\n\t{chatLine}\n\t{chatLinePrefix}\n\n");
						return false;
					}

					valueStartIndex += chatLinePrefix.Length;
					if (chatLine.Length > valueStartIndex)
					{
						variablesResult = chatLine.Substring(valueStartIndex);
					}

					//File.AppendAllText(logFilePath, $"Chat line prefix matched, values: {variablesResult}\n\n");
					wasFound = true;
					return true;
				});

				var waitForChatTask = Chat.WaitForMQ2(doesChatLineMatch, 3000, cancellationToken);

				// no parse on our toon, tell the remote toon to echo the value(s) back to us and we'll parse it out of the chat line
				var echoCommand = $"/noparse /bct {toonName} //bct {ControlToonName} /echo {variablePrefix}={variableValuesExpression}";
				Mq2.DoCommand(echoCommand);


				var wasFound2 = await waitForChatTask;
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogWarning($"Exception thrown:\n\n{exc}\n");
				//Mq2.WriteChatSafe($"Exception: {StringHelper.EscapeForMQ2Chat(exc.ToString())}");
			}

			//Mq2.WriteChatSafe($"Parse variable chat line (was found: {wasFound}):  {variablesResult}");

			return variablesResult;
		}

		public Task RunAsync(string[] commandArguments, CancellationToken token)
		{
			throw new NotImplementedException();
		}
	}
}
