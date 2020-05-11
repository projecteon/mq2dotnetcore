using MQ2DotNetCore.MQ2Api;
using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore
{
	/// <summary>
	/// Contract interface for submodule program entry points executed via the /netrun &lt;programname&gth; command.
	/// </summary>
	public interface IMQ2Program
	{
		/// <summary>
		/// Entry point for an IMQ2Program to be executed using the /netcorerun &lt;programname&gt; [...arguments] comamnd.
		/// </summary>
		/// <param name="commandArguments">The command arguments including the program name.</param>
		/// <param name="mq2Dependencies">The <see cref="MQ2Dependencies"/> instance.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the task.</param>
		Task RunAsync(string[] commandArguments, MQ2Dependencies mq2Dependencies, CancellationToken cancellationToken);
	}
}
