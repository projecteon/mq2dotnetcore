using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore.MQ2Api
{
	internal sealed class MQ2AsyncCommandTaskWrapper
	{
		public MQ2AsyncCommandTaskWrapper(
			string commandName,
			Task task,
			CancellationToken taskCancellationToken
		)
		{
			CommandName = commandName;
			Task = task;
			TaskCancellationToken = taskCancellationToken;
		}

		public string CommandName { get; }
		public Task Task { get; }
		public CancellationToken TaskCancellationToken { get; }
	}
}
