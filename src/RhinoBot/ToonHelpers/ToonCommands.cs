using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RhinoBot.ToonHelpers
{
	public class ToonCommands : CommandBase
	{
		public ToonCommands(RhinoBot bot, bool logDebugEnabled)
			: base(bot, logDebugEnabled)
		{
		}

		public async Task ExecuteActionAsync(ToonAction? toonAction, CancellationToken cancellationToken)
		{
			if (toonAction == null)
			{
				LogDebug("Toon action is null!");
				return;
			}

			if (!string.IsNullOrEmpty(toonAction.Id))
			{
				LogDebug($"Executing toon action: {toonAction.Id}");
			}

			if (toonAction.DelayBeforeExecuting > 0)
			{
				await Task.Delay(toonAction.DelayBeforeExecuting, cancellationToken);
			}

			var targetToonNames = await GetToonNamesForActionAsync(toonAction, cancellationToken);
			if (targetToonNames?.Any() != true)
			{
				LogDebug($"{nameof(ToonCommands)}.{nameof(ExecuteActionAsync)}(..) did not find any toon names to execute the action for!");
				return;
			}

			switch (toonAction.ActionType)
			{
				case ToonActionType.DoCommand:
					if (string.IsNullOrWhiteSpace(toonAction.ActionValue))
					{
						LogDebug($"{nameof(ToonCommands)}.{nameof(ExecuteActionAsync)}(..) was a called with a null {nameof(ToonAction.ActionValue)} with {nameof(ToonActionType)} value of {toonAction.ActionType}!");
						break;
					}

					if (toonAction.IdentifierType == ToonIdentifierType.CurrentGroup)
					{
						// just send a /bcaa
						var bcaaCommand = $"/noparse /bcaa /{toonAction.ActionValue}";
						LogDebug($"All Toons In Current Group - Executing DoCommand: {bcaaCommand}");
						Bot.Mq2.DoCommand(bcaaCommand);
						break;
					}

					foreach (var targetToonName in targetToonNames)
					{
						if (string.IsNullOrWhiteSpace(targetToonName))
						{
							LogDebug("targetToonName is null/empty/whitespace!");
							continue;
						}


						LogDebug($"({targetToonName}) - Executing DoCommand: {toonAction.ActionValue}");
						var remoteCommand = targetToonName == Bot.ControlToonName
							? toonAction.ActionValue
							: $"/noparse /bct {targetToonName} /{toonAction.ActionValue}";

						LogDebug($"({targetToonName}) - Executing DoCommand: {toonAction.ActionValue}");
						Bot.Mq2.DoCommand(toonAction.ActionValue);
					}

					break;

				case ToonActionType.None:
				default:
					LogDebug($"{nameof(ToonCommands)}.{nameof(ExecuteActionAsync)}(..) was a called with an invalid/unsupported {nameof(ToonActionType)} value of {toonAction.ActionType}!");
					break;
			}

			if (toonAction.DelayAfterExecuting > 0)
			{
				await Task.Delay(toonAction.DelayAfterExecuting, cancellationToken);
			}
		}

		public async Task<string> GetRemoteToonTargetIdAsync(string remoteToonName, CancellationToken cancellationToken)
		{
			var targetId = await Bot
				.ParseVariablesOnRemoteToonAsync(remoteToonName, "MyTargetId", "${Target.ID}", cancellationToken);

			return targetId == null || targetId == "NULL"
				? string.Empty
				: targetId;
		}

		public async Task<IReadOnlyCollection<string>?> GetToonNamesForActionAsync(ToonAction toonAction, CancellationToken cancellationToken)
		{
			if (toonAction == null)
			{
				LogDebug($"{nameof(ToonCommands)}.{nameof(GetToonNamesForActionAsync)}(..) was called with a null toonAction!");
				return null;
			}

			switch (toonAction.IdentifierType)
			{
				case ToonIdentifierType.None:
					LogDebug($"{nameof(ToonCommands)}.{nameof(GetToonNamesForActionAsync)}(..) was called with a {nameof(ToonIdentifierType)} value of {nameof(ToonIdentifierType.None)}!");
					return null;

				case ToonIdentifierType.CurrentGroup:
					return Bot.GroupCommands.GetCurrentGroupMemberNames();

				case ToonIdentifierType.CurrentToon:
					return new List<string>() { Bot.ControlToonName };

				case ToonIdentifierType.GroupMemberIndex:
					if (string.IsNullOrWhiteSpace(toonAction.IdentifierValue))
					{
						LogDebug($"{nameof(ToonCommands)}.{nameof(GetToonNamesForActionAsync)}(..) has a null/empty/whitespace {nameof(ToonAction.IdentifierValue)} for an identifier type of {toonAction.IdentifierType}!");
						return null;
					}

					if (!int.TryParse(toonAction.IdentifierValue, out var groupMemberIndex))
					{
						LogDebug($"{nameof(ToonCommands)}.{nameof(GetToonNamesForActionAsync)}(..) was unable to parse the groupMemberIndex integer value from the  {nameof(ToonAction.IdentifierValue)}!");
						return null;
					}

					var groupMemberName = Bot.Tlo.Group?.Member[groupMemberIndex]?.Name;
					return groupMemberName == null
						? null
						: new List<string>() { groupMemberName };

				case ToonIdentifierType.GroupName:
					await Task.Delay(100, cancellationToken);

					// TODO: Implement logic to get group type info for groups in a raid / remote groups?
					LogDebug($"{nameof(ToonCommands)}.{nameof(GetToonNamesForActionAsync)}(..) is not yet implemented for the {nameof(ToonIdentifierType)} value of {nameof(ToonIdentifierType.GroupName)}!");
					return null;

				case ToonIdentifierType.ToonId:
					if (string.IsNullOrWhiteSpace(toonAction.IdentifierValue))
					{
						LogDebug($"{nameof(ToonCommands)}.{nameof(GetToonNamesForActionAsync)}(..) has a null/empty/whitespace {nameof(ToonAction.IdentifierValue)} for an identifier type of {toonAction.IdentifierType}!");
						return null;
					}

					var toonName = Bot.Tlo.Spawn[$"pc {toonAction.IdentifierValue}"]?.Name;
					if (string.IsNullOrWhiteSpace(toonName))
					{
						LogDebug($"{nameof(ToonCommands)}.{nameof(GetToonNamesForActionAsync)}(..) fa!");
						return null;
					}

					return new List<string>() { toonName };

				case ToonIdentifierType.ToonName:
					if (string.IsNullOrWhiteSpace(toonAction.IdentifierValue))
					{
						LogDebug($"{nameof(ToonCommands)}.{nameof(GetToonNamesForActionAsync)}(..) has a null/empty/whitespace {nameof(ToonAction.IdentifierValue)} for an identifier type of {toonAction.IdentifierType}!");
						return null;
					}

					return new List<string>() { toonAction.IdentifierValue };

				default:
					LogDebug($"{nameof(ToonCommands)}.{nameof(GetToonNamesForActionAsync)}(..) was an invalid/unsupported {nameof(ToonIdentifierType)} value of {toonAction.IdentifierType}!");
					return null;
			}
		}
	}
}
