namespace RhinoBot.Base
{
	public static class StringHelper
	{
		public static string EscapeForMQ2Chat(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				return message;
			}

			return message.Replace("\\", "\\\\");
		}

		public static bool ParseBoolean(string value, bool defaultValue = false)
		{
			var convertedValue = TryConvertToBoolean(value);
			return convertedValue ?? defaultValue;
		}

		public static bool? TryConvertToBoolean(string? value)
		{
			if (value?.Equals("true", System.StringComparison.OrdinalIgnoreCase) == true)
			{
				return true;
			}

			if (value?.Equals("false", System.StringComparison.OrdinalIgnoreCase) == true)
			{
				return false;
			}

			return null;
		}
	}
}
