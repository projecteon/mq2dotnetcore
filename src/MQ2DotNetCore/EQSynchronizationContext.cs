using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore
{
	/// <summary>
	/// Per Alynel, the vast majority of EQ/MQ2 calls are not thread safe so we'll start most tasks using the task factory combined with this
	/// synchronization context to ensure the task continuations run on the 'EQ' thread by default.
	/// </summary>
	internal class EQSynchronizationContext : SynchronizationContext
	{
		internal static EQSynchronizationContext Instance = new EQSynchronizationContext();

		private EQSynchronizationContext(): base()
		{

		}

		internal Task RunDelegateAsync(Func<Task> taskFunction)
		{
			// TODO: Re-implement sync context
			return Task.Run(taskFunction);
		}
	}
}
