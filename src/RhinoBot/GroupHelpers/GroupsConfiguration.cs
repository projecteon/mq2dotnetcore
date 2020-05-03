using RhinoBot.Configs;
using System;
using System.Collections.Generic;

namespace RhinoBot.GroupHelpers
{
	public class GroupsConfiguration
	{
		public Version? FileVersion { get; set; }

		public ICollection<GroupSettings>? Groups { get; set; }
	}
}
