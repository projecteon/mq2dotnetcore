using JetBrains.Annotations;
using MQ2DotNet.MQ2API;
using MQ2DotNet.Program;
using MQ2DotNet.Services;
using MQ2DotNetCore;
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
		public readonly MQ2ChatWindow Mq2;
		//public readonly Chat Chat;
		public readonly Commands Commands;
		//public readonly Events Events;
		public readonly Spawns Spawns;
		public readonly TLO Tlo;

		public readonly string ControlToonName;

		public readonly ConfigLoader Configs;

		public readonly GroupCommands GroupCommands;
		public readonly LocationCommands LocationCommands;
		public readonly MissionCommands MissionCommands;
		public readonly ToonCommands ToonCommands;

		public RhinoBot()
		{
			Mq2 = mq2;
			//Chat = chat;
			Commands = commands;
			//Events = events;
			Spawns = spawns;
			Tlo = tlo;

			ControlToonName = tlo.Me.Name;

			Configs = new ConfigLoader(this, false);

			GroupCommands = new GroupCommands(this, false);
			LocationCommands = new LocationCommands(this, true);
			MissionCommands = new MissionCommands(this, true);
			ToonCommands = new ToonCommands(this, true);
		}

		public async Task Main(string[] args, CancellationToken cancellationToken)
		{
			Mq2.WriteChatSafe($"\ag[{nameof(RhinoBot)}]\aw {nameof(Main)}(..) is executing...");

			//Events.OnChatMQ2 += (s, e) =>
			//{
			//	if (e == "[MQ2] Hello")
			//		Mq2.WriteChat("Hello yourself");
			//};

			Commands.AddAsyncCommand("/formgroup", async commandArgs => await GroupCommands.FormGroupAsync(commandArgs).ConfigureAwait(false));
			Commands.AddAsyncCommand("/navto", async commandArgs => await LocationCommands.NavigateToLocationAsync(commandArgs).ConfigureAwait(false));
			Commands.AddCommand("/reloadconfigs", Configs.ReloadAll);
			Commands.AddAsyncCommand("/runmission", MissionCommands.RunMissionAsync);

			while (cancellationToken != null && !cancellationToken.IsCancellationRequested)
			{
				await Task.Delay(500, cancellationToken);
			}

			Commands.RemoveCommand("/formgroup");
			Commands.RemoveCommand("/navto");
			Commands.RemoveCommand("/reloadconfigs");
			Commands.RemoveCommand("/runmission");

			Mq2.WriteChatSafe($"\ag[{nameof(RhinoBot)}]\aw {nameof(Main)}(..) is exiting.");
		}

		public static string GetConfigFilePath(string configFileName)
			=> Path.Combine(Path.GetDirectoryName(RhinoBotConstants.AssemblyLocation), "Configs", configFileName);

		public async Task<string> ParseVariablesOnRemoteToonAsync(string toonName, string variablePrefix, string variableValuesExpression)
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

				TimeSpan timeout = TimeSpan.FromSeconds(3);

				using (var timeoutCancellationTokenSource = new CancellationTokenSource(timeout))
				{
					Predicate<string> doesChatLineMatch = (chatLine =>
					{
						if (wasFound)
						{
							timeoutCancellationTokenSource.Cancel();
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
						timeoutCancellationTokenSource.Cancel();
						return true;
					});

					//var waitForOutputTask = Chat.WaitForMQ2(doesChatLineMatch, 2000);
					var waitForOutputTask = Task.CompletedTask;

					// no parse on our toon, tell the remote toon to echo the value(s) back to us and we'll parse it out of the chat line
					var echoCommand = $"/noparse /bct {toonName} //bct {ControlToonName} /echo {variablePrefix}={variableValuesExpression}";

					Mq2.DoCommand(echoCommand);


					await waitForOutputTask
						.WaitAsync(timeoutCancellationTokenSource.Token)
						.ConfigureAwait(false);
				}
			}
			catch (Exception exc)
			{
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
