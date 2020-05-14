using MQ2DotNetCore.Logging;

namespace RhinoBot
{
	public abstract class CommandBase
	{
		protected readonly bool _logDebugEnabled = true;

		protected readonly RhinoBot Bot;

		protected CommandBase(RhinoBot bot, bool logDebugEnabled)
		{
			Bot = bot;
			_logDebugEnabled = logDebugEnabled;
		}

		protected void LogDebug(string message)
		{
			if (_logDebugEnabled)
			{
				FileLoggingHelper.LogDebug(message);
				Bot.Mq2.WriteChatSafe(message);
			}
		}
	}
}
