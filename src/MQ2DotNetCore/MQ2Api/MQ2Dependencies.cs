namespace MQ2DotNetCore.MQ2Api
{
	public class MQ2Dependencies
	{
		public MQ2Dependencies(
			MQ2SubmoduleCommandRegistry commandRegistry,
			MQ2ChatWindow mq2ChatWindow,
			string submoduleName
		)
		{
			CommandRegistry = commandRegistry;
			MQ2ChatWindow = mq2ChatWindow;
			SubmoduleName = submoduleName;
		}

		public MQ2SubmoduleCommandRegistry CommandRegistry { get; }
		public MQ2ChatWindow MQ2ChatWindow { get; }
		public string SubmoduleName { get; }
	}
}
