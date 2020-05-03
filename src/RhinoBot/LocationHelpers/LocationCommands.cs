using MQ2DotNet.MQ2API.DataTypes;
using RhinoBot.Base;
using System;
using System.Threading.Tasks;

namespace RhinoBot.LocationHelpers
{
	public class LocationCommands : CommandBase
	{
		public LocationCommands(RhinoBot bot, bool logDebugEnabled)
			: base(bot, logDebugEnabled)
		{
		}

		public void DoNavigation(string navigationCommand, string toonName)
			=> Bot.Mq2.DoCommand(toonName == Bot.ControlToonName
				? navigationCommand
				: $"/bct {toonName} /{navigationCommand}");

		public async Task<Coordinates> GetLocationCoordinatesAsync(LocationSettings location, string toonName)
		{
			if (location == null)
			{
				LogDebug($"{nameof(LocationCommands)}.{nameof(GetLocationCoordinatesAsync)}(..) was called with a null {nameof(location)} parameter!");
				return null;
			}

			var isValueRequired = location.NavigationType != NavigationType.CurrentTarget;
			if (isValueRequired && string.IsNullOrWhiteSpace(location.Value))
			{
				LogDebug($"{nameof(LocationCommands)}.{nameof(GetLocationCoordinatesAsync)}(..) was called with a null/empty/whitespace {nameof(location)}.Value!");
				return null;
			}

			switch (location.NavigationType)
			{
				case NavigationType.None:
					Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(GetLocationCoordinatesAsync)}(..) was called with a location.NavigationType of None!");
					return null;

				case NavigationType.Coordinates:
				{
					var wasParseSuccessful = Coordinates.TryParseYXZ(location.Value, out var parsedCoordinates);
					if (!wasParseSuccessful || parsedCoordinates == null)
					{
						LogDebug($"Failed to parse coordinates from value: {location.Value}");
					}

					return parsedCoordinates;
				}

				case NavigationType.CurrentTarget:
				{
					string? targetLocation;
					if (toonName == Bot.ControlToonName)
					{
						targetLocation = Bot.Tlo.Target.MQLoc;
					}
					else
					{
						var remoteToonTargetId = await Bot.ToonCommands
							.GetRemoteToonTargetIdAsync(toonName)
							.ConfigureAwait(false);

						var targetSpawn = Bot.Tlo.Spawn[remoteToonTargetId];
						targetLocation = targetSpawn?.MQLoc;
					}

					var wasParseSuccessful = Coordinates.TryParseYXZ(targetLocation, out var parsedCoordinates);
					if (!wasParseSuccessful || parsedCoordinates == null)
					{
						LogDebug($"Failed to parse coordinates from target location: {targetLocation}");
					}

					return parsedCoordinates;
				}

				case NavigationType.SpawnSearch:
				{
					var spawn = Bot.Tlo.Spawn[location.Value];
					if (spawn == null)
					{
						var spawnId = Bot.Mq2.Parse($"${{Spawn[{location.Value}].ID}}");
						spawn = Bot.Tlo.Spawn[spawnId];

						if (spawn == null)
						{
							LogDebug($"Failed to get spawn instance ({location.NavigationType}) from location value: {location.Value} (parsed spawn id: {spawnId})");
							return null;
						}
					}

					if (location.NavigationType == NavigationType.SpawnId)
					{
						var spawnIdMatches = int.TryParse(location.Value, out int parsedSpawnId)
							&& spawn.ID == parsedSpawnId;

						if (spawnIdMatches)
						{
							LogDebug($"Spawn instance ID ({spawn.ID}) does not match location value: {location.Value}");
							return null;
						}
					}

					var spawnLocation = spawn.MQLoc;
					var wasParseSuccessful = Coordinates.TryParseYXZ(spawnLocation, out var parsedCoordinates, Bot.Mq2);
					if (!wasParseSuccessful || parsedCoordinates == null)
					{
						LogDebug($"Failed to parse coordinates from spawn location: {spawnLocation}");
					}

					return parsedCoordinates;
				}

				case NavigationType.TargetOfGroupMemberName:
				{
					var groupMember = Bot.Tlo.Group.Member[location.Value];
					if (groupMember?.Name != location.Value)
					{
						Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(GetLocationCoordinatesAsync)}(..) was unable find the target group member with name: {location.Value}!");
						return null;
					}

					var groupMemberTarget = groupMember.Spawn.TargetOfTarget;
					if (groupMemberTarget == null)
					{
						Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(GetLocationCoordinatesAsync)}(..) group member with name: {location.Value} does not have a target to navigate to!");
						return null;
					}

					var targetLocation = groupMemberTarget.MQLoc;
					var wasParseSuccessful = Coordinates.TryParseYXZ(targetLocation, out var parsedCoordinates);
					if (!wasParseSuccessful || parsedCoordinates == null)
					{
						LogDebug($"Failed to parse coordinates from group member ({location.Value})'s target ({groupMemberTarget.Name}) location: {targetLocation}");
					}

					return parsedCoordinates;
				}

				case NavigationType.WaypointName:
				{
					LogDebug($"{nameof(GetLocationCoordinatesAsync)}(..) is not currently supported for type {nameof(NavigationType.WaypointName)}");
					return null;
				}

				default:
					Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(GetLocationCoordinatesAsync)}(..) was called with an unrecognized/unsupported navigation type: {location.NavigationType}!");
					return null;
			}
		}

		public async Task<string> GetNavigationCommandAsync(LocationSettings location)
		{
			if (location == null)
			{
				Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(GetNavigationCommandAsync)}(..) was called with a null location!");
				return null;
			}

			if (string.IsNullOrWhiteSpace(location.Value))
			{
				Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(GetNavigationCommandAsync)}(..) was called with a null/empty/whitespace location.Value!");
				return null;
			}

			string navigationCommand;
			switch (location.NavigationType)
			{
				case NavigationType.None:
					Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(GetNavigationCommandAsync)}(..) was called with a location.NavigationType of None!");
					return null;

				case NavigationType.Coordinates:
					return $"/nav loc {location.Value}";

				case NavigationType.CurrentTarget:
					return "/nav target";

				case NavigationType.SpawnId:
					return $"/nav id {location.Value}";

				case NavigationType.SpawnSearch:
					return $"/nav spawn {location.Value}";

				case NavigationType.TargetOfGroupMemberName:
					int? targetId = null;

					if (Bot.ControlToonName == location.Value)
					{
						targetId = Bot.Tlo.Target.ID;
					}
					else
					{
						var remoteToonTargetId = await Bot.ToonCommands
							.GetRemoteToonTargetIdAsync(location.Value)
							.ConfigureAwait(false);

						if (int.TryParse(remoteToonTargetId, out var remoteToonTargetIdInteger))
						{
							targetId = remoteToonTargetIdInteger;
						}
					}

					return targetId != null
						? $"/nav id {targetId.Value}"
						: $"/nav name {location.Value}";

				case NavigationType.WaypointName:
					return $"/nav waypoint {location.Value}";

				default:
					Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(GetNavigationCommandAsync)}(..) was called with an unrecognized/unsupported navigation type: {location.NavigationType}!");
					return null;
			}
		}

		public async Task<bool?> IsNavigatingAsync(string toonName)
		{
			var isNavigatingValue = await Bot
				.ParseVariablesOnRemoteToonAsync(toonName, "IsNavigating", "${Navigation.Active}")
				.ConfigureAwait(false);

			var isNavigating = StringHelper.TryConvertToBoolean(isNavigatingValue);
			if (isNavigating == null)
			{
				Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(IsNavigatingAsync)}(..) failed to parse the isNavigationValue of: {isNavigatingValue}");
			}

			return isNavigating;
		}

		public bool? IsNearCoordinates(Coordinates coordinates, int distanceThreshold, GroupMemberType toon)
		{
			if (coordinates == null || toon == null)
			{
				LogDebug($"{nameof(LocationCommands)}.{nameof(IsNearCoordinates)} was called with null coordinates/and or toon parameters");
				return null;
			}

			if (!Coordinates.TryParseYXZ(toon.LocYXZ, out var currentLocation))
			{
				LogDebug($"{nameof(LocationCommands)}.{nameof(IsNearCoordinates)} was unable to parse the toon's current location: {toon.LocYXZ}");
				return null;
			}

			// Too lazy to calcualte the straight line distance so i'll just treat the threshold as a value to check each
			// dimension against...

			var deltaX = Math.Abs(currentLocation.X - coordinates.X);
			var deltaY = Math.Abs(currentLocation.Y - coordinates.Y);
			var deltaZ = Math.Abs(currentLocation.Z - coordinates.Z);
			var isNearXYCoordinates = deltaX <= distanceThreshold && deltaY <= distanceThreshold;
			var isNearZCoordinate = deltaZ <= distanceThreshold;

			if (!isNearZCoordinate)
			{
				LogDebug($"{nameof(LocationCommands)}.{nameof(IsNearCoordinates)} deltaZ is not within the threshold...");
				// don't care about Z for now
				// return false; 
			}

			return isNearXYCoordinates;
		}

		public async Task NavigateToLocationAsync(bool cancelCurrentNavigation, LocationSettings location, string toonName)
		{
			if (location == null || string.IsNullOrWhiteSpace(toonName))
			{
				LogDebug($"{nameof(LocationCommands)}.{nameof(NavigateToLocationAsync)}(..) received an invalid location object and/or toon name, cancelling!");
				return;
			}

			var locationName = location.Name 
				?? $"Missing Location Name (Zone: {location.ZoneName}) (Type: {location.NavigationType}) (Value: {location.Value})";

			Bot.Mq2.WriteChatSafe($"Navigating {toonName} to {locationName}");

			var targetToon = Bot.Tlo.Group.Member[toonName];
			if (targetToon?.Name != toonName)
			{
				Bot.Mq2.WriteChatSafe($"Unable to locate toon to navigate with name: {toonName}");
				return;
			}

			if (cancelCurrentNavigation)
			{
				DoNavigation("/nav stop", toonName);
				await Task.Delay(200).ConfigureAwait(false);
			}

			var navigationCommand = await GetNavigationCommandAsync(location).ConfigureAwait(false);
			if (navigationCommand == null)
			{
				LogDebug($"{nameof(LocationCommands)}.{nameof(NavigateToLocationAsync)}(..) received a null navigation command result!");
				return;
			}

			LogDebug($"Running navigation command: {navigationCommand}");
			DoNavigation(navigationCommand, toonName);
			await Task.Delay(200).ConfigureAwait(false);

			var targetCoordinates = await GetLocationCoordinatesAsync(location, toonName).ConfigureAwait(false);
			if (targetCoordinates == null)
			{
				LogDebug($"Couldn't determine the target coordinates so we'll just assume the navigation command is going to work...");
				await Task.Delay(500).ConfigureAwait(false);
				return;
			}

			// TODO: Support specifying this somehow...
			var updateTargetCoordinatesForMovingTarget = false;

			//Coordinates? previousCoordinates = null;
			while (IsNearCoordinates(targetCoordinates, location.DistanceThreshold ?? 10, targetToon) == false)
			{
				var isNavigating = await IsNavigatingAsync(toonName).ConfigureAwait(false);
				if (isNavigating == null)
				{
					LogDebug("Unable to determine if the toon is currently navigating, cancelling!");
					return;
				}

				if (!isNavigating.Value)
				{
					LogDebug($"IsNavigating is false, executing the navigation command ({navigationCommand})...");
					DoNavigation(navigationCommand, toonName);
					await Task.Delay(200).ConfigureAwait(false);
				}
				else
				{
					LogDebug($"IsNavigating is true...");
				}

				await Task.Delay(500).ConfigureAwait(false);

				if (updateTargetCoordinatesForMovingTarget)
				{
					targetCoordinates = await GetLocationCoordinatesAsync(location, toonName).ConfigureAwait(false);
				}
			}

			LogDebug($"Toon is near location, returning from the {nameof(NavigateToLocationAsync)} method...");
		}

		public async Task NavigateToLocationAsync(string[] commandArguments)
		{
			if (commandArguments.Length < 1)
			{
				Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(NavigateToLocationAsync)}(..) was called with zero command arguments. The location name is required at a minimum!");
			}

			var locationName = commandArguments[0];
			if (string.IsNullOrWhiteSpace(locationName))
			{
				Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(NavigateToLocationAsync)}(..) was called with a null/empty/whitespace location name argument!");
				return;
			}

			var namedLocations = Bot.Configs.GetNamedLocations();
			if (namedLocations == null)
			{
				LogDebug($"{nameof(LocationCommands)}.{nameof(NavigateToLocationAsync)}(..) received null for the named locations config!");
				return;
			}

			if (!namedLocations.TryGetValue(locationName, out var location)
				|| location == null)
			{
				Bot.Mq2.WriteChatSafe($"{nameof(LocationCommands)}.{nameof(NavigateToLocationAsync)}(..) did not find a matching named location for: {locationName}!");
				return;
			}

			var remoteToonName = commandArguments.Length > 1 ? commandArguments[1] : null;
			if (string.IsNullOrEmpty(remoteToonName))
			{
				remoteToonName = Bot.ControlToonName;
			}

			// TODO: Check current zone, if not in location zone navigate to it and/or cancel

			var cancelCurrentNavigation = true;
			if (commandArguments.Length > 2)
			{
				cancelCurrentNavigation = StringHelper.ParseBoolean(commandArguments[2], true);
			}

			await NavigateToLocationAsync(cancelCurrentNavigation, location, remoteToonName);
		}

	}
}
