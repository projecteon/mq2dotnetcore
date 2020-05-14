using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MQ2DotNetCore.Interop
{
	/// <summary>
	/// Used by MQ2 to represent a variable. Consists of a type component, pType, that points to an instance of MQ2Type, and a data component, VarPtr, that stores data for this variable
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 16)]
	public struct MQ2TypeVar
	{
		// Since we don't care about members and will only be calling functions, marshalling as IntPtr seems the easiest/safest option
		// Only a 4 byte field but gets packed to 8 bytes. Many hours wasted before realizing this :(
		[FieldOffset(0)] internal IntPtr pType;
		[FieldOffset(8)] internal MQ2VarPtr VarPtr;

		internal bool TryGetMember(string memberName, string index, out MQ2TypeVar result)
		{
			if (pType == IntPtr.Zero)
			{
				throw new InvalidOperationException();
			}

			var wasGetMemberSuccessful = MQ2DotNetCoreLoader.NativeMethods.MQ2Type__GetMember(
				pType,
				VarPtr,
				memberName,
				index,
				out result
			);


			return wasGetMemberSuccessful && result.pType != IntPtr.Zero;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var resultStringBuilder = new StringBuilder(2048);
			if (!MQ2DotNetCoreLoader.NativeMethods.MQ2Type__ToString(pType, VarPtr, resultStringBuilder))
			{
				throw new ApplicationException("MQ2Type::ToString failed");
			}

			return resultStringBuilder.ToString();
		}
	}
}
