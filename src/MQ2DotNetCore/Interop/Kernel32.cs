using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MQ2DotNetCore.Interop
{
	internal static class Kernel32
	{
		public const string DLL = "kernel32.dll";

		internal static class NativeMethods
		{
			[DllImport(Kernel32.DLL, ExactSpelling = true, SetLastError = true)]
			internal static extern bool FreeLibrary(IntPtr moduleHandle);

			/// <summary>
			/// Native method for reading values from .INI files. Why not use a managed method or stop using INI files all together?
			/// </summary>
			[DllImport(Kernel32.DLL)]
			public static extern uint GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder returnValue, int size, string filePath);


			[DllImport(Kernel32.DLL)]
			public static extern IntPtr GetProcAddress(SafeLibraryHandle libraryHandle, string processName);


			[DllImport(Kernel32.DLL)]
			public static extern SafeLibraryHandle LoadLibrary(string libraryName);

			/// <summary>
			/// Native method for writing values to .INI files. Why not use a managed method or stop using INI files all together?
			/// </summary>
			[DllImport(Kernel32.DLL)]
			public static extern bool WritePrivateProfileString(string section, string key, string value, string filePath);
		}
	}
}
