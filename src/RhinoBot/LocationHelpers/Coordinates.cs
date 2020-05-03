using MQ2DotNet.MQ2API;
using System;
using System.Linq;

namespace RhinoBot.LocationHelpers
{
	public class Coordinates
	{
		public double X;
		public double Y;
		public double Z;

		public static Coordinates ParseYXZ(string yxzValue)
		{
			if (string.IsNullOrWhiteSpace(yxzValue))
			{
				throw new ArgumentNullException(nameof(yxzValue), "cannot be null/empty/whitespace");
			}

			var coordinateValues = yxzValue.Split(' ');
			if (coordinateValues.Length != 3)
			{
				throw new ArgumentException("was not a valid yxz location string", nameof(yxzValue));
			}

			if (!double.TryParse(coordinateValues[0], out var yValue))
			{
				throw new InvalidOperationException("Failed to parse yValue");
			}

			if (!double.TryParse(coordinateValues[1], out var xValue))
			{
				throw new InvalidOperationException("Failed to parse xValue");
			}

			if (!double.TryParse(coordinateValues[2], out var zValue))
			{
				throw new InvalidOperationException("Failed to parse zValue");
			}

			return new Coordinates
			{
				X = xValue,
				Y = yValue,
				Z = zValue
			};
		}

		public static bool TryParseYXZ(string yxzValue, out Coordinates? coordinates, MQ2? mq2 = null)
		{
			coordinates = null;
			if (string.IsNullOrWhiteSpace(yxzValue))
			{
				mq2?.WriteChatSafe("yxzValue was null/empty/whitespace");
				return false;
			}

			var coordinateValues = yxzValue
				.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
				.Select(nextValue => nextValue.Trim())
				.ToArray();

			if (coordinateValues.Length != 3)
			{
				mq2?.WriteChatSafe("yxzValue split length wasn't correct: " + coordinateValues.Length);
				return false;
			}

			if (!double.TryParse(coordinateValues[0], out var yValue))
			{
				mq2?.WriteChatSafe("yxzValue failed to parse yValue: " + coordinateValues[0]);
				return false;
			}

			if (!double.TryParse(coordinateValues[1], out var xValue))
			{
				mq2?.WriteChatSafe("yxzValue failed to parse xValue: " + coordinateValues[1]);
				return false;
			}

			if (!double.TryParse(coordinateValues[2], out var zValue))
			{
				mq2?.WriteChatSafe("yxzValue failed to parse zValue: " + coordinateValues[2]);
				return false;
			}

			coordinates = new Coordinates
			{
				X = xValue,
				Y = yValue,
				Z = zValue
			};

			return true;
		}
	}
}
