using RhinoBot.Configs;
using System;
using System.Collections.Concurrent;

namespace RhinoBot.ToonHelpers
{
	public class ToonActionsConfiguration
	{
		public Version FileVersion { get; set; }
		public ConcurrentDictionary<string, ToonAction> Actions { get; set; }
	}
}
