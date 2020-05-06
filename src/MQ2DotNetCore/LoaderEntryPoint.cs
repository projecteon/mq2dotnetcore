using MQ2DotNetCore.Base;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace MQ2DotNetCore
{
	public static class LoaderEntryPoint
	{
		//private static readonly string _logFilePath = Path.Combine(AssemblyInformation.AssemblyLocation, "debug_entry_point.log");
		private static readonly string _logFilePath = @"C:\Downloads\EQ\VeryVanilla\Live\Release\MQ2DotNetCore\debug_entry_point.log";
		private static void LogToFile(string message)
		{
			try
			{
				File.AppendAllText(_logFilePath, $"[{DateTime.Now}  {nameof(LoaderEntryPoint)}]  {message}\n");
			}
			catch (Exception exc)
			{
				try
				{
					File.WriteAllText($@"C:\Downloads\debug_loader_entry_point.{DateTime.Now}.log", message);
				}
				catch (Exception exc2)
				{
					Console.Write(exc2);
				}
			}
		}

		public static string GetCallerMemberName([CallerMemberName] string? callerMemberName = null)
			=> callerMemberName;

		public static int InitializePlugin(IntPtr arg, int argLength)
		{
			try
			{
				LogToFile("The InitializePlugin(..) method is executing...");


				string? callerMemberName;
				try
				{
					callerMemberName = GetCallerMemberName();
				}
				catch (Exception exc)
				{
					callerMemberName = exc.ToString();
				}
				LogToFile($"Caller Member Name: {callerMemberName ?? "null"}");

				string? assemblyLocation;
				try
				{
					assemblyLocation = AssemblyInformation.AssemblyLocation;
				}
				catch (Exception exc)
				{
					assemblyLocation = exc.ToString();
				}
				LogToFile($"Assembly Location: {assemblyLocation ?? "null"}");

				//LogToFile($"Attempting to load the MQ2DotNetCoreLoader.dll from: {loaderLibraryPath}");

				return 0;
			}
			catch (Exception exc)
			{
				try
				{
					LogToFile($"The InitializePlugin(..) encountered an exception:\n\n{exc.ToString()}");
				}
				catch (Exception)
				{

				}
			}

			return 1;
		}
	}
}
