using System.Threading.Tasks;

namespace RhinoBot.MissionHelpers
{
	public class MissionCommands : CommandBase
	{
		public MissionCommands(RhinoBot bot, bool logDebugEnabled)
			: base(bot, logDebugEnabled)
		{
		}

		public async Task RunMissionAsync(string[] commandArguments)
		{
			var missionName = commandArguments?.Length > 0 ? commandArguments[0] : null;
			if (string.IsNullOrEmpty(missionName))
			{
				LogDebug($"{nameof(MissionCommands)}.{nameof(RunMissionAsync)}(..) received a null/empty mission name (1st parameter)!");
				return;
			}

			var missionActions = Bot.Configs.GetMissionActions(missionName);
			if (missionActions == null)
			{
				LogDebug($"No mission actions found for mission name: {missionName}");
				return;
			}

			foreach (var nextAction in missionActions)
			{
				await Bot.ToonCommands.ExecuteActionAsync(nextAction).ConfigureAwait(false);
			}
		}
	}
}
