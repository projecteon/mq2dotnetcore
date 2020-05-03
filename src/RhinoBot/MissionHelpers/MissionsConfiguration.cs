using RhinoBot.ToonHelpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RhinoBot.MissionHelpers
{
	public class MissionsConfiguration
	{
		public Version FileVersion { get; set; }
		public ConcurrentDictionary<string, List<string>> Missions { get; set; }
	}
}
