using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace MQ2DotNetCore.Interop
{
	internal static class MQ2Main
	{
		internal const string DLL = "MQ2Main.dll";

		internal static class NativeMethods
		{
			// COMMANDS
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			[SuppressMessage("ReSharper", "InconsistentNaming")]
			internal delegate void fEQCommand(IntPtr characterSpawn, [MarshalAs(UnmanagedType.LPStr)] string buffer);

			[DllImport(MQ2Main.DLL, EntryPoint = "AddCommand", CallingConvention = CallingConvention.Cdecl)]
			[SuppressMessage("ReSharper", "InconsistentNaming")]
			internal static extern void MQ2AddCommand([MarshalAs(UnmanagedType.LPStr)] string command, fEQCommand function, bool eq = false, bool parse = true, bool inGame = false);

			[DllImport(MQ2Main.DLL, EntryPoint = "RemoveCommand", CallingConvention = CallingConvention.Cdecl)]
			[SuppressMessage("ReSharper", "InconsistentNaming")]
			internal static extern bool MQ2RemoveCommand([MarshalAs(UnmanagedType.LPStr)] string command);


			// CHAT + BASE
			[DllImport(MQ2Main.DLL, EntryPoint = "Calculate", CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool MQ2Calculate([MarshalAs(UnmanagedType.LPStr)] string formula, out double result);

			[DllImport(MQ2Main.DLL, EntryPoint = "HideDoCommand", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void MQ2HideDoCommand(IntPtr characterSpawnIntPointer, [MarshalAs(UnmanagedType.LPStr)] string command, bool delayed);

			[DllImport(MQ2Main.DLL, EntryPoint = "ParseMacroData", CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool MQ2ParseMacroData([MarshalAs(UnmanagedType.LPStr)] StringBuilder original, uint bufferSize);

			[DllImport(MQ2Main.DLL, EntryPoint = "WriteChatf", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void MQ2WriteChatf([MarshalAs(UnmanagedType.LPStr)] string buffer);

			[DllImport(MQ2Main.DLL, EntryPoint = "WriteChatfSafe", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void MQ2WriteChatfSafe([MarshalAs(UnmanagedType.LPStr)] string buffer);


			// DATA TYPES
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate bool fMQData([MarshalAs(UnmanagedType.LPStr)] string szIndex, out MQ2TypeVar ret);

			[DllImport(MQ2Main.DLL, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr FindMQ2DataType(string name);

			[DllImport(MQ2Main.DLL, EntryPoint = "FindMQ2Data", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr FindMQ2DataIntPtr([MarshalAs(UnmanagedType.LPStr)] string szName);

			[DllImport(MQ2Main.DLL, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr GetItemList();



			[StructLayout(LayoutKind.Explicit, Size = 68)]
			internal struct MQ2DataItem
			{
				[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
				[FieldOffset(0)]
				public byte[] Name;

				[FieldOffset(64)]
				public IntPtr pFunction;

				public fMQData Function => Marshal.GetDelegateForFunctionPointer<fMQData>(pFunction);
			}
		}
	}
}
