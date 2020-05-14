using JetBrains.Annotations;
using MQ2DotNetCore.Interop;

namespace MQ2DotNetCore.MQ2Api.DataTypes
{
	/// <summary>
	/// MQ2 array type. Not well supported
	/// </summary>
	[PublicAPI]
	[MQ2Type("array")]
	public class ArrayType : MQ2DataType
	{
		internal ArrayType(MQ2TypeFactory mq2TypeFactory, MQ2TypeVar typeVar) : base(mq2TypeFactory, typeVar)
		{
		}
	}
}
