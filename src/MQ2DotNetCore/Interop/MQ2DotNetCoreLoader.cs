using MQ2DotNetCore.Base;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MQ2DotNetCore.Interop
{
	internal static class MQ2DotNetCoreLoader
	{
		public static readonly string AbsoluteDllPath = Path.GetFullPath(Path.Combine(MQ2DotNetCoreAssemblyInformation.AssemblyDirectory, @"..\MQ2DotNetLoader.dll"));
		public const string DllName = "MQ2DotNetCoreLoader.dll";
		public const string RelativeDllpath = @"..\MQ2DotNetCoreLoader.dll";

		internal static class NativeMethods
		{
			// These are all class methods and I don't want to deal with PInvoking that, so the loader dll has some helper methods
			[DllImport(MQ2DotNetCoreLoader.DllName, CallingConvention = CallingConvention.Cdecl)]
			public static extern bool MQ2Type__FromData(IntPtr pThis, out MQ2VarPtr varPtr, ref MQ2TypeVar source);

			[DllImport(MQ2DotNetCoreLoader.DllName, CallingConvention = CallingConvention.Cdecl)]
			public static extern bool MQ2Type__FromString(IntPtr pThis, out MQ2VarPtr varPtr, string source);

			[DllImport(MQ2DotNetCoreLoader.DllName, CallingConvention = CallingConvention.Cdecl)]
			public static extern void MQ2Type__InitVariable(IntPtr pThis, out MQ2VarPtr varPtr);

			[DllImport(MQ2DotNetCoreLoader.DllName, CallingConvention = CallingConvention.Cdecl)]
			public static extern void MQ2Type__FreeVariable(IntPtr pThis, ref MQ2VarPtr varPtr);

			[DllImport(MQ2DotNetCoreLoader.DllName, CallingConvention = CallingConvention.Cdecl)]
			public static extern bool MQ2Type__GetMember(IntPtr pThis, MQ2VarPtr varPtr, string member, string index, out MQ2TypeVar dest);

			[DllImport(MQ2DotNetCoreLoader.DllName, CallingConvention = CallingConvention.Cdecl)]
			public static extern bool MQ2Type__ToString(IntPtr pThis, MQ2VarPtr varPtr, [MarshalAs(UnmanagedType.LPStr)] StringBuilder destination);
		}
	}
}
