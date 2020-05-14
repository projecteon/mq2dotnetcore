using JetBrains.Annotations;
using MQ2DotNetCore.Interop;
using MQ2DotNetCore.MQ2Api.DataTypes;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MQ2DotNetCore.MQ2Api
{
	/// <summary>
	/// Contains utility methods and properties relating to spawns
	/// </summary>
	[PublicAPI]
	public class MQ2Spawns
	{
		private readonly MQ2TypeFactory _mq2TypeFactory;

		internal MQ2Spawns(MQ2TypeFactory mq2TypeFactory)
		{
			_mq2TypeFactory = mq2TypeFactory;
		}

		/// <summary>
		/// All spawns in the current zone
		/// </summary>
		public IEnumerable<SpawnType> GetAll()
		{
			var spawnManagerPointer = MQ2NativeHelper.GetSpawnManagerIntPointer();
			if (spawnManagerPointer == IntPtr.Zero)
			{
				yield break;
			}

			var nextSpawnPointer = Marshal.ReadIntPtr(spawnManagerPointer + 8);
			while (nextSpawnPointer != IntPtr.Zero)
			{
				yield return new SpawnType(_mq2TypeFactory, nextSpawnPointer);
				nextSpawnPointer = Marshal.ReadIntPtr(nextSpawnPointer + 8);
			}
		}

		/// <summary>
		/// All ground spawns in the current zone
		/// </summary>
		public IEnumerable<GroundType> GetAllGround()
		{
			var groundItemListManagerPointer = MQ2Main.NativeMethods.GetItemList();
			var nextGroundItemPointer = Marshal.ReadIntPtr(groundItemListManagerPointer);

			while (nextGroundItemPointer != IntPtr.Zero)
			{
				yield return new GroundType(_mq2TypeFactory, nextGroundItemPointer);
				nextGroundItemPointer = Marshal.ReadIntPtr(nextGroundItemPointer + 4);
			}
		}
	}
}
