namespace RhinoBot.LocationHelpers
{
	public class LocationSettings
	{// Default constructor for de-serialization support
		public LocationSettings() { }

		public int? DistanceThreshold { get; set; }
		public string Name { get; set; }
		public NavigationType NavigationType { get; set; }
		public string Value { get; set; }
		public string ZoneName { get; set; }
	}
}
