using Newtonsoft.Json;
using RhinoBot.Base;
using RhinoBot.GroupHelpers;
using RhinoBot.LocationHelpers;
using RhinoBot.MissionHelpers;
using RhinoBot.ToonHelpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RhinoBot.Configs
{
	public class ConfigLoader : CommandBase
	{
		public ConfigLoader(RhinoBot bot, bool logDebugEnabled)
			: base(bot, logDebugEnabled)
		{
		}

		// TODO: Use file watch change tokens to detect if a config file is modified and 
		// to invalidate or reload the cached config object

		protected GroupsConfiguration? _groupsConfiguration;

		public GroupsConfiguration? GetGroupsConfiguration(bool forceReload = false)
		{
			try
			{
				if (_groupsConfiguration == null || forceReload)
				{
					var configFilePath = RhinoBot.GetConfigFilePath("groups.config.json");
					var groupsConfigurationContent = File.ReadAllText(configFilePath);
					_groupsConfiguration = JsonConvert.DeserializeObject<GroupsConfiguration>(groupsConfigurationContent);
				}

				return _groupsConfiguration;
			}
			catch (Exception exc)
			{
				Bot.Mq2.WriteChatSafe($"{nameof(ConfigLoader)}.{nameof(GetGroupsConfiguration)}(..) encountered an exception: {StringHelper.EscapeForMQ2Chat(exc.ToString())}");
				return null;
			}
		}

		public List<ToonAction?>? GetMissionActions(string missionName, bool forceReload = false)
		{
			var missionsConfiguration = GetMissionsConfiguration(forceReload);
			if (missionsConfiguration?.Missions == null)
			{
				return null;
			}

			if (missionsConfiguration.Missions.TryGetValue(missionName, out var missionActionNames))
			{
				return missionActionNames?
					.Select(missionActionName => GetNamedAction(missionActionName))
					.ToList();
			}

			return null;
		}

		protected MissionsConfiguration? _missionsConfiguration;

		public MissionsConfiguration? GetMissionsConfiguration(bool forceReload = false)
		{
			try
			{
				if (_missionsConfiguration == null || forceReload)
				{
					var configFilePath = RhinoBot.GetConfigFilePath("missions.config.json");
					var configFileContent = File.ReadAllText(configFilePath);
					_missionsConfiguration = JsonConvert.DeserializeObject<MissionsConfiguration>(configFileContent);
				}

				return _missionsConfiguration;
			}
			catch (Exception exc)
			{
				Bot.Mq2.WriteChatSafe($"{nameof(ConfigLoader)}.{nameof(GetNamedLocations)}(..) encountered an exception: {StringHelper.EscapeForMQ2Chat(exc.ToString())}");
				return null;
			}
		}


		protected ConcurrentDictionary<string, LocationSettings>? _namedLocations;

		public ConcurrentDictionary<string, LocationSettings>? GetNamedLocations(bool forceReload = false)
		{
			try
			{
				if (_namedLocations == null || forceReload)
				{
					var configFilePath = RhinoBot.GetConfigFilePath("named_locations.config.json");
					var configFileContent = File.ReadAllText(configFilePath);
					_namedLocations = JsonConvert.DeserializeObject<ConcurrentDictionary<string, LocationSettings>>(configFileContent);
				}

				return _namedLocations;
			}
			catch (Exception exc)
			{
				Bot.Mq2.WriteChatSafe($"{nameof(ConfigLoader)}.{nameof(GetNamedLocations)}(..) encountered an exception: {StringHelper.EscapeForMQ2Chat(exc.ToString())}");
				return null;
			}
		}

		public ToonAction? GetNamedAction(string actionName, bool forceReload = false)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				return null;
			}

			var actionsConfiguration = GetNamedActionsConfiguration(forceReload);
			if (actionsConfiguration == null || actionsConfiguration.Actions == null)
			{
				return null;
			}

			if (actionsConfiguration.Actions.TryGetValue(actionName, out var toonAction))
			{
				return toonAction;
			}

			return null;
		}

		private ToonActionsConfiguration? _toonActionsConfiguration;

		public ToonActionsConfiguration? GetNamedActionsConfiguration(bool forceReload = false)
		{
			try
			{
				if (_toonActionsConfiguration == null || forceReload)
				{
					var configFilePath = RhinoBot.GetConfigFilePath("named_toon_actions.config.json");
					var configFileContent = File.ReadAllText(configFilePath);
					_toonActionsConfiguration = JsonConvert.DeserializeObject<ToonActionsConfiguration>(configFileContent);
				}

				return _toonActionsConfiguration;
			}
			catch (Exception exc)
			{
				Bot.Mq2.WriteChatSafe($"{nameof(ConfigLoader)}.{nameof(GetNamedActionsConfiguration)}(..) encountered an exception: {StringHelper.EscapeForMQ2Chat(exc.ToString())}");
				return null;
			}
		}

		public void ReloadAll(string[] commandArguments)
		{
			// Just clearing them should be sufficient, they'll get reloaded on next access
			_groupsConfiguration = null;
			_missionsConfiguration = null;
			_namedLocations = null;
			_toonActionsConfiguration = null;
		}
	}
}
