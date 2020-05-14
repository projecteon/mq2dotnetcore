using JetBrains.Annotations;
using MQ2DotNetCore.Interop;

namespace MQ2DotNetCore.MQ2Api.DataTypes
{
	/// <summary>
	/// MQ2 type for a benchmark. This does not seem to be implemented.
	/// </summary>
	// The definition exists in MQ2, but the implementation doesn't and it's never added as a type
	//[MQ2Type("benchmark")]
	[PublicAPI]
	public class BenchmarkType : MQ2DataType
	{
		internal BenchmarkType(MQ2TypeFactory mq2TypeFactory, MQ2TypeVar typeVar) : base(mq2TypeFactory, typeVar)
		{
		}
	}
}
