using MQ2DotNetCore.Interop;
using System;
using System.Text;

namespace MQ2DotNetCore.MQ2Api
{
	/// <summary>
	/// Contains methods and properties relating to MQ2 functionality
	/// </summary>
	public sealed class MQ2
	{
		private readonly MQ2NativeHelper _mq2NativeHelper;

		internal MQ2(MQ2NativeHelper mq2NativeHelper)
		{
			_mq2NativeHelper = mq2NativeHelper ?? throw new ArgumentNullException(nameof(mq2NativeHelper));
		}

		/// <summary>
		/// Uses MQ2's parser to evaluate a formula
		/// </summary>
		/// <param name="formula">Formula to calculate</param>
		/// <param name="parse">If <c>true</c>, will first parse any MQ2 variables in <paramref name="formula"/> before calculating</param>
		/// <returns>Result of the calculation</returns>
		public double Calculate(string formula, bool parse = true)
		{
			if (parse)
			{
				formula = Parse(formula);
			}

			if (!MQ2Main.NativeMethods.MQ2Calculate(formula, out var result))
			{
				throw new FormatException("Could not parse if condition: " + formula);
			}

			return result;
		}

		/// <summary>
		/// Execute a command, exactly as if you typed it in the chat window
		/// Note: whether this will parse MQ2 variables or not depends only on the command entered. Use /noparse to force no parsing
		/// </summary>
		/// <param name="command">Command to execute</param>
		public void DoCommand(string command)
		{
			var characterSpawnIntPointer = _mq2NativeHelper.GetCharacterSpawnIntPointer();
			if (characterSpawnIntPointer == IntPtr.Zero)
			{
				return;
			}

			MQ2Main.NativeMethods.MQ2HideDoCommand(characterSpawnIntPointer, command, false);
		}

		/// <summary>
		/// Use MQ2's parser to evaluate a formula and return true if it is non-zero
		/// </summary>
		/// <param name="formula">Formula to calculate</param>
		/// <param name="parse">If <c>true</c>, will first parse any MQ2 variables in <paramref name="formula"/> before calculating</param>
		/// <returns>True if the result is non-zero, otherwise false</returns>
		public bool If(string formula, bool parse = true)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			return Calculate(formula, parse) != 0.0;
		}

		/// <summary>
		/// Parse any MQ2 variables in <paramref name="expression"/> and replace them with the resulting text
		/// </summary>
		/// <param name="expression">Expression to parse</param>
		/// <returns>Parsed expression</returns>
		public string Parse(string expression)
		{
			var stringBuilder = new StringBuilder(expression, 2047);
			if (!MQ2Main.NativeMethods.MQ2ParseMacroData(stringBuilder, (uint)stringBuilder.Capacity + 1))
			{
				throw new FormatException("Could not parse expression: " + expression);
			}

			return stringBuilder.ToString();
		}

		/// <summary>
		/// Write a line of chat to the MQ2 chat window
		/// </summary>
		/// <param name="text">Text to write</param>
		public void WriteChat(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			// TODO: The Sanitize(..) logic trims and cuts off anything after a line break to prevent
			// MQ2 from crashing. We should modify this to split the text, if necessary and print
			// it in segments so that it successfully prints all of the requested text.

			MQ2Main.NativeMethods.MQ2WriteChatf(Sanitize(text));
		}

		/// <summary>
		/// Threadsafe version of <see cref="WriteChat"/>
		/// </summary>
		/// <param name="text">Text to write</param>
		public void WriteChatSafe(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			// TODO: The Sanitize(..) logic trims and cuts off anything after a line break to prevent
			// MQ2 from crashing. We should modify this to split the text, if necessary and print
			// it in segments so that it successfully prints all of the requested text. 
			MQ2Main.NativeMethods.MQ2WriteChatfSafe(Sanitize(text));
		}

		private static string Sanitize(string text)
		{
			// Trim so as to not crash MQ2/EQ
			var sanitized = text.Substring(0, Math.Min(text.Length, 2047));
			var index = text.IndexOfAny(new[] { '\r', '\n' });
			if (index > 0)
			{
				sanitized = sanitized.Substring(0, index);
			}

			return sanitized;
		}

		// TODO: Add an MQ2ColorHelper with methods to wrap text strings up with the control characters
		// for colors...
		internal void WriteChatGeneralError(string text)
		{
			WriteChat($"\ag[.NET] \arError: \aw{text}");
		}

		internal void WriteChatGeneralWarning(string text)
		{
			WriteChat($"\ag[.NET] \ayWarning: \aw{text}");
		}

		internal void WriteChatGeneral(string text)
		{
			WriteChat($"\ag[.NET] \aw{text}");
		}

		internal void WriteChatPluginError(string text)
		{
			WriteChat($"\ag[.NET Plugin] \arError: \aw{text}");
		}

		internal void WriteChatPluginWarning(string text)
		{
			WriteChat($"\ag[.NET Plugin] \ayWarning: \aw{text}");
		}

		internal void WriteChatPlugin(string text)
		{
			WriteChat($"\ag[.NET Plugin] \aw{text}");
		}

		internal void WriteChatProgramError(string text)
		{
			WriteChat($"\ag[.NET Program] \arError: \aw{text}");
		}

		internal void WriteChatProgramWarning(string text)
		{
			WriteChatSafe($"\ag[.NET Program] \ayWarning: \aw{text}");
		}

		internal void WriteChatProgram(string text)
		{
			WriteChatSafe($"\ag[.NET Program] \aw{text}");
		}

		internal void WriteChatScriptError(string text)
		{
			WriteChat($"\ag[C# Script] \arError: \aw{text}");
		}

		internal void WriteChatScriptWarning(string text)
		{
			WriteChat($"\ag[C# Script] \ayWarning: \aw{text}");
		}

		internal void WriteChatScript(string text)
		{
			WriteChat($"\ag[C# Script] \aw{text}");
		}
	}
}
