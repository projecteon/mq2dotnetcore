using Microsoft.Win32.SafeHandles;

namespace MQ2DotNetCore.Interop
{
	internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		internal SafeLibraryHandle() : base(true) { }

		protected override bool ReleaseHandle()
		{
			return Kernel32.NativeMethods.FreeLibrary(handle);
		}
	}
}
