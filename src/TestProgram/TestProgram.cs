using MQ2DotNetCore;
using System.Threading;
using System.Threading.Tasks;

namespace TestProgram
{
	public class TestProgram : IMQ2Program
	{
		public async Task RunAsync(string[] commandArguments, CancellationToken token)
		{
			MQ2DotNetCore.Logging.FileLoggingHelper
				.LogInformation($"{nameof(TestProgram)}.{nameof(RunAsync)}(..) is executing. [CommandArguments: {string.Join(", ", commandArguments)}]");

			await Task.Delay(500);
		}
	}
}
