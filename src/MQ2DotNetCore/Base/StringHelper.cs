﻿using System.Collections.Generic;
using System.Text;

namespace MQ2DotNetCore.Base
{
	public static class StringHelper
	{
		/// <summary>
		/// Split a string into an array of arguments separated by spaces, accounting for arguments
		/// that are wrapped in double quotes.
		///
		/// Note: I'm not sure if EQ / MQ2 supports single quotes or escaping characters using
		/// \ or ` so I'll leave this logic to just double quotes for now....
		/// </summary>
		/// <param name="input">The input string to split into arguments.</param>
		/// <returns>The list of arguments from the input string</returns>
		public static List<string> SplitArguments(string input)
		{
			var argumentList = new List<string>();
			if (string.IsNullOrWhiteSpace(input))
			{
				return argumentList;
			}

			var argumentStringBuilder = new StringBuilder();
			var isInsideDoubleQuote = false;

			foreach (char nextCharacter in input)
			{
				if (nextCharacter == '"')
				{
					isInsideDoubleQuote = !isInsideDoubleQuote;
					continue;
				}

				if (nextCharacter == ' ' && !isInsideDoubleQuote)
				{
					argumentList.Add(argumentStringBuilder.ToString());
					argumentStringBuilder.Clear();
					continue;
				}

				argumentStringBuilder.Append(nextCharacter);
			}

			if (argumentStringBuilder.Length > 0)
			{
				argumentList.Add(argumentStringBuilder.ToString());
			}

			return argumentList;
		}
	}
}
