namespace MQ2DotNetCore.MQ2Api
{
	public class MQ2Dependencies
	{
		public MQ2Dependencies(
			MQ2SubmoduleCommandRegistry commandRegistry,
			MQ2ChatWindow mq2ChatWindow,
			MQ2SynchronizationContext mq2SynchronizationContext,
			string submoduleName
		)
		{
			CommandRegistry = commandRegistry;
			MQ2ChatWindow = mq2ChatWindow;
			MQ2SynchronizationContext = mq2SynchronizationContext;
			SubmoduleName = submoduleName;
		}

		public MQ2SubmoduleCommandRegistry CommandRegistry { get; }
		public MQ2ChatWindow MQ2ChatWindow { get; }
		public MQ2SynchronizationContext MQ2SynchronizationContext { get; }

		public string SubmoduleName { get; }
	}
}
