using MQ2DotNetCore.MQ2Api.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RhinoBot.GroupHelpers
{
	public class GroupCommands : CommandBase
	{

		public GroupCommands(RhinoBot bot, bool logDebugEnabled)
			: base(bot, logDebugEnabled)
		{
		}

		public async Task FormGroupAsync(string[] commandArguments, CancellationToken cancellationToken)
		{
			Bot.Mq2.WriteChatSafe($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..) is executing...");
			try
			{
				await Task.Delay(500, cancellationToken);

				var groupName = commandArguments?.FirstOrDefault() ?? "Default";
				if (string.IsNullOrEmpty(groupName))
				{
					Bot.Mq2.WriteChatSafe($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..): group name cannot be an empty string!");
					return;
				}

				LogDebug($"Group name: {groupName}");

				var groupsConfiguration = Bot.Configs.GetGroupsConfiguration();
				if (groupsConfiguration == null)
				{
					Bot.Mq2.WriteChatSafe($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..): group configuration is null!");
					return;
				}

				LogDebug($"Groups in configuration count: {groupsConfiguration.Groups?.Count}");

				var groupSettings = groupsConfiguration.Groups.FirstOrDefault(group => group.Name == groupName);
				if (groupSettings == null)
				{
					Bot.Mq2.WriteChatSafe($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..): did not find a group matching the name: {groupName}!");
					return;
				}

				LogDebug($"Validating group settings: {groupSettings.Name}");

				if (!IsValidGroupSettings(groupSettings))
				{
					return;
				}

				LogDebug($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..): inviting group members...");
				var firstGroupMemberName = groupSettings.Members.First();
				foreach (var nextGroupMemberName in groupSettings.Members.Skip(1))
				{
					InviteToGroup(firstGroupMemberName, nextGroupMemberName);
					await Task.Delay(100, cancellationToken);
				}

				// Wait for them to pickup the invites
				await Task.Delay(2000, cancellationToken);

				LogDebug($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..): accepting invites...");
				foreach (var nextGroupMemberName in groupSettings.Members.Skip(1))
				{
					LogDebug($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..): accepting invite on {nextGroupMemberName}");

					var joinedGroup = await TryAcceptInviteAsync(nextGroupMemberName, cancellationToken);
					if (!joinedGroup)
					{
						Bot.Mq2.WriteChatSafe($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..): Failed to accept the invite on {nextGroupMemberName}!");
						return;
					}
				}

				await Task.Delay(500, cancellationToken);

				if (!string.IsNullOrEmpty(groupSettings.Leader))
				{
					LogDebug($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..): setting group leader to {groupSettings.Leader}...");
					SetGroupLeader(firstGroupMemberName, groupSettings.Leader);
				}

				await Task.Delay(500, cancellationToken);
			}
			catch (Exception exc)
			{
				Bot.Mq2.WriteChatSafe($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..) encountered an unhandled exception: {exc.ToString()}");
			}
			finally
			{
				Bot.Mq2.WriteChatSafe($"{nameof(GroupCommands)}.{nameof(FormGroupAsync)}(..) is done executing.");
			}
		}

		public IReadOnlyCollection<string> GetCurrentGroupMemberNames()
		{
			return GetGroupMemberNames(Bot.Tlo.Group);
		}

		public IReadOnlyCollection<string> GetGroupMemberNames(GroupType? group)
		{
			var groupMemberCount = group?.Members ?? 0;
			if (groupMemberCount < 1)
			{
				return Array.Empty<string>();
			}

			var groupMemberNames = new List<string>();
			for (int groupMemberIndex = 1; groupMemberIndex <= groupMemberCount; ++groupMemberIndex)
			{
				var nextGroupMemberName = Bot.Tlo.Group?.Member[groupMemberIndex]?.Name;
				if (!string.IsNullOrWhiteSpace(nextGroupMemberName))
				{
					groupMemberNames.Add(nextGroupMemberName);
				}
			}

			return groupMemberNames;
		}

		public bool IsValidGroupSettings(GroupSettings groupSettings)
		{
			var validationMessage = new StringBuilder();
			if (groupSettings.Members == null)
			{
				validationMessage.Append("\ngroupSettings.Members is null");
			}
			else
			{
				if (groupSettings.Members.Count < 2)
				{
					validationMessage.Append("\ngroupSettings.Members.Count is less than 2");
				}
				else if (groupSettings.Members.Count > 6)
				{
					validationMessage.Append("\ngroupSettings.Members.Count is greater than 6");
				}

				var groupMemberIndex = 0;
				foreach (var groupMemberName in groupSettings.Members)
				{
					++groupMemberIndex;
					if (string.IsNullOrWhiteSpace(groupMemberName))
					{
						validationMessage.Append($"\ngroupSettings.Members[{groupMemberIndex}] is null/empty/whitespace");
					}
				}
			}

			if (string.IsNullOrWhiteSpace(groupSettings.Leader))
			{
				validationMessage.Append($"\ngroupSettings.Leader is null/empty/whitespace");
			}
			else if (!groupSettings.Members?.Any(groupMemberName => groupMemberName == groupSettings.Leader) == true)
			{
				validationMessage.Append($"\ngroupSettings.Leader does not match any of the member names");
			}

			if (!string.IsNullOrWhiteSpace(groupSettings.MainTank)
				&& !groupSettings.Members?.Any(groupMemberName => groupMemberName == groupSettings.MainTank) == true)
			{
				validationMessage.Append($"\ngroupSettings.MainTank does not match any of the member names");
			}

			LogDebug($"Validation message length: {validationMessage.Length}");

			if (validationMessage.Length == 0)
			{
				return true;
			}

			Bot.Mq2.WriteChatSafe($"GroupSettings ({groupSettings.Name}) are invalid:{validationMessage}");
			return false;
		}

		public void InviteToGroup(string firstGroupMemberName, string groupMemberNameToInvite)
			=> Bot.Mq2.DoCommand(Bot.ControlToonName == firstGroupMemberName
				? $"/invite {groupMemberNameToInvite}"
				: $"/bct {firstGroupMemberName} //invite {groupMemberNameToInvite}");

		public void SetGroupLeader(string currentGroupLeader, string newGroupLeader)
			=> Bot.Mq2.DoCommand(Bot.ControlToonName == currentGroupLeader
				? $"/makeleader {newGroupLeader}"
				: $"/bct {currentGroupLeader} //makeleader {newGroupLeader}");

		public async Task<bool> TryAcceptInviteAsync(string groupMemberNameToAccept, CancellationToken cancellationToken)
		{
			var acceptInviteCount = 0;

			// Multi-threading issue or bug with index access that causes this to return the wrong person
			GroupMemberType? nextGroupMember = Bot.Tlo.Group?.Member[groupMemberNameToAccept];

			var isCorrectGroupMember = nextGroupMember?.Name == groupMemberNameToAccept;
			LogDebug($"Inside {nameof(GroupCommands)}.{nameof(TryAcceptInviteAsync)}(..) for {groupMemberNameToAccept}, isCorrectGroupMember: {isCorrectGroupMember}");
			while (!isCorrectGroupMember && acceptInviteCount < 5)
			{
				// Accept the invites in order and wait until they're in the group before accepting the next invite
				Bot.Mq2.DoCommand($"/bct {groupMemberNameToAccept} //invite");
				++acceptInviteCount;

				await Task.Delay(1000, cancellationToken);

				nextGroupMember = Bot.Tlo.Group?.Member[groupMemberNameToAccept];
				isCorrectGroupMember = nextGroupMember?.Name == groupMemberNameToAccept;
				LogDebug($"Inside {nameof(GroupCommands)}.{nameof(TryAcceptInviteAsync)}(..) for {groupMemberNameToAccept}, isCorrectGroupMember: {isCorrectGroupMember}, acceptInviteCount: {acceptInviteCount}");
			}

			return isCorrectGroupMember;
		}
	}
}
