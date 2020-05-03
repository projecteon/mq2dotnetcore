using System.Collections.Generic;

namespace RhinoBot.GroupHelpers
{
	/// <summary>
	/// Represents the settings for a specific group definition.
	/// </summary>
	public class GroupSettings
	{
		// Default constructor for de-serialization support
		public GroupSettings() { }

		public string Leader { get; set; }
		public string MainTank { get; set; }
		public ICollection<string> Members { get; set; }
		public string Name { get; set; }
	}
}
