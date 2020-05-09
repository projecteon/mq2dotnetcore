using MQ2DotNetCore.Interop;
using MQ2DotNetCore.Logging;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MQ2DotNet.MQ2API
{
	/// <summary>
	/// Contains methods and properties relating to MQ2 functionality
	/// </summary>
	public sealed class MQ2
	{
		private static SafeLibraryHandle? _mq2MainLibraryHandle;
		static MQ2()
		{
			try
			{
				_mq2MainLibraryHandle = Kernel32.NativeMethods.LoadLibrary(MQ2Main.DLL);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogCritical($"Failed to load library {MQ2Main.DLL}!");
			}
		}

		/// <summary>
		/// Write a line of chat to the MQ2 chat window
		/// </summary>
		/// <param name="text">Text to write</param>
		public void WriteChat(string text)
		{
			MQ2Main.NativeMethods.MQ2WriteChatf(Sanitize(text));
		}

		/// <summary>
		/// Threadsafe version of <see cref="WriteChat"/>
		/// </summary>
		/// <param name="text">Text to write</param>
		public void WriteChatSafe(string text)
		{
			// Trim so as to not crash MQ2/EQ
			MQ2Main.NativeMethods.MQ2WriteChatfSafe(Sanitize(text));
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
				formula = Parse(formula);

			if (!MQ2Main.NativeMethods.MQ2Calculate(formula, out var result))
			{
				throw new FormatException("Could not parse if condition: " + formula);
			}

			return result;
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
			var sb = new StringBuilder(expression, 2047);
			if (!MQ2Main.NativeMethods.MQ2ParseMacroData(sb, (uint)sb.Capacity + 1))
			{
				throw new FormatException("Could not parse expression: " + expression);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Execute a command, exactly as if you typed it in the chat window
		/// Note: whether this will parse MQ2 variables or not depends only on the command entered. Use /noparse to force no parsing
		/// </summary>
		/// <param name="command">Command to execute</param>
		public void DoCommand(string command)
		{
			var characterSpawnIntPointer = GetCharacterSpawnIntPointer();
			if (characterSpawnIntPointer == IntPtr.Zero)
			{
				return;
			}

			MQ2Main.NativeMethods.MQ2HideDoCommand(characterSpawnIntPointer, command, false);
		}

		private static string? _mq2IniPath = null;
		public static string? GetMQ2IniPath()
		{
			if (_mq2IniPath != null)
			{
				return _mq2IniPath;
			}

			if (_mq2MainLibraryHandle == null)
			{
				return null;
			}

			var mq2InitPath = Marshal.PtrToStringAnsi(Kernel32.NativeMethods.GetProcAddress(_mq2MainLibraryHandle, "gszINIPath"));
			_mq2IniPath = mq2InitPath;
			return _mq2IniPath;
		}

		public static IntPtr GetCharacterSpawnIntPointer()
		{
			if (_mq2MainLibraryHandle == null)
			{
				return IntPtr.Zero;
			}

			try
			{
				var ppLocalPlayer = Kernel32.NativeMethods.GetProcAddress(_mq2MainLibraryHandle, "ppLocalPlayer");
				var ppPlayer = Marshal.ReadIntPtr(ppLocalPlayer);
				return Marshal.ReadIntPtr(ppPlayer);
			}
			catch (Exception exc)
			{
				FileLoggingHelper.LogError($"{nameof(GetCharacterSpawnIntPointer)} failed locating / marshalling the integer pointer!\n\n{exc.ToString()}");
				return IntPtr.Zero;
			}
		}

		private static string Sanitize(string text)
		{
			// Trim so as to not crash MQ2/EQ
			var sanitized = text.Substring(0, Math.Min(text.Length, 2047));
			var index = text.IndexOfAny(new[] { '\r', '\n' });
			if (index > 0)
				sanitized = sanitized.Substring(0, index);
			return sanitized;
		}

		internal static void WriteChatGeneralError(string text)
		{
			new MQ2().WriteChat($"\ag[.NET] \arError: \aw{text}");
		}

		internal static void WriteChatGeneralWarning(string text)
		{
			new MQ2().WriteChat($"\ag[.NET] \ayWarning: \aw{text}");
		}

		internal static void WriteChatGeneral(string text)
		{
			new MQ2().WriteChat($"\ag[.NET] \aw{text}");
		}

		internal static void WriteChatPluginError(string text)
		{
			new MQ2().WriteChat($"\ag[.NET Plugin] \arError: \aw{text}");
		}

		internal static void WriteChatPluginWarning(string text)
		{
			new MQ2().WriteChat($"\ag[.NET Plugin] \ayWarning: \aw{text}");
		}

		internal static void WriteChatPlugin(string text)
		{
			new MQ2().WriteChat($"\ag[.NET Plugin] \aw{text}");
		}

		internal static void WriteChatProgramError(string text)
		{
			new MQ2().WriteChat($"\ag[.NET Program] \arError: \aw{text}");
		}

		internal static void WriteChatProgramWarning(string text)
		{
			new MQ2().WriteChat($"\ag[.NET Program] \ayWarning: \aw{text}");
		}

		internal static void WriteChatProgram(string text)
		{
			new MQ2().WriteChat($"\ag[.NET Program] \aw{text}");
		}

		internal static void WriteChatScriptError(string text)
		{
			new MQ2().WriteChat($"\ag[C# Script] \arError: \aw{text}");
		}

		internal static void WriteChatScriptWarning(string text)
		{
			new MQ2().WriteChat($"\ag[C# Script] \ayWarning: \aw{text}");
		}

		internal static void WriteChatScript(string text)
		{
			new MQ2().WriteChat($"\ag[C# Script] \aw{text}");
		}
	}
}
