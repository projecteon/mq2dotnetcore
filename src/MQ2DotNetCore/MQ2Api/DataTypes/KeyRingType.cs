using JetBrains.Annotations;
using MQ2DotNetCore.Interop;

namespace MQ2DotNetCore.MQ2Api.DataTypes
{
	/// <summary>
	/// MQ2 type for an item on the keyring
	/// </summary>
	[PublicAPI]
	[MQ2Type("keyring")]
	public class KeyRingType : MQ2DataType
	{
		internal KeyRingType(MQ2TypeFactory mq2TypeFactory, MQ2TypeVar typeVar) : base(mq2TypeFactory, typeVar)
		{
		}

		/// <summary>
		/// Index of the item in the list (1 based)
		/// </summary>
		public int? Index => GetMember<IntType>("Index");

		/// <summary>
		/// Name of the item
		/// </summary>
		public string? Name => GetMember<StringType>("Name");
	}
}
