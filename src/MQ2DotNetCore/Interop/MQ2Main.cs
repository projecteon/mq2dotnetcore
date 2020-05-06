using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace MQ2DotNetCore.Interop
{
	internal static class MQ2Main
	{
		internal const string DLL = "MQ2Main.dll";

		internal static class NativeMethods
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			[SuppressMessage("ReSharper", "InconsistentNaming")]
			internal delegate void fEQCommand(IntPtr characterSpawn, [MarshalAs(UnmanagedType.LPStr)] string buffer);

			[DllImport(MQ2Main.DLL, EntryPoint = "AddCommand", CallingConvention = CallingConvention.Cdecl)]
			[SuppressMessage("ReSharper", "InconsistentNaming")]
			internal static extern void MQ2AddCommand([MarshalAs(UnmanagedType.LPStr)] string command, fEQCommand function, bool eq = false, bool parse = true, bool inGame = false);

			[DllImport(MQ2Main.DLL, EntryPoint = "RemoveCommand", CallingConvention = CallingConvention.Cdecl)]
			[SuppressMessage("ReSharper", "InconsistentNaming")]
			internal static extern bool MQ2RemoveCommand([MarshalAs(UnmanagedType.LPStr)] string command);
		}
	}
}
