using System.Threading;
using System.Threading.Tasks;

namespace MQ2DotNetCore
{
	/// <summary>
	/// Contract interface for submodule program entry points executed via the /netrun &lt;programname&gth; command.
	/// </summary>
	public interface IMQ2Program
	{
		Task RunAsync(string[] commandArguments, CancellationToken token);
	}
}
