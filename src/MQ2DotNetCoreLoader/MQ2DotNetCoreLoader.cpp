#define TEST

#ifdef MQ2SourceRootFolder

#include "MQ2Plugin.h"

#else

#include "../../../MQ2Plugin.h"

#endif

#include "libs/nethost-win-x86/nethost.h"

#include "includes/coreclr_delegates.h"
#include "includes/hostfxr.h"

#include <ctime>
#include <fstream>
#include <memory>
#include <stdexcept>
#include <string>
#include <strsafe.h>


//#import <mscorlib.tlb> raw_interfaces_only			\
//	auto_rename											\
//	high_property_prefixes("_get","_put","_putref")		\
//	rename("ReportEvent", "InteropServices_ReportEvent")


PLUGIN_VERSION(0.1);
PreSetup("MQ2DotNetCoreLoader");

bool g_bLoaded{ false };
wchar_t g_entryAssemblyLibraryPath[MAX_PATH] = { 0 };
wchar_t g_dotnetRuntimeConfigPath[MAX_PATH] = { 0 };
wchar_t g_pluginLogFile[MAX_PATH] = { 0 };

void* get_export(HMODULE hModule, const char* name);

void logToFile(std::string message);

bool loadDotNetClrAndExecuteEntryPoint();

// Functions in the managed dll. All standard plugin callbacks except initialize, since there's no point having that
extern "C" __declspec(dllexport) fMQShutdownPlugin g_pfShutdownPlugin { nullptr };
extern "C" __declspec(dllexport) fMQCleanUI g_pfOnCleanUI { nullptr };
extern "C" __declspec(dllexport) fMQReloadUI g_pfOnReloadUI { nullptr };
extern "C" __declspec(dllexport) fMQDrawHUD g_pfOnDrawHUD { nullptr };
extern "C" __declspec(dllexport) fMQSetGameState g_pfSetGameState { nullptr };
extern "C" __declspec(dllexport) fMQPulse g_pfOnPulse { nullptr };
extern "C" __declspec(dllexport) fMQIncomingChat g_pfOnIncomingChat { nullptr };
extern "C" __declspec(dllexport) fMQWriteChatColor g_pfOnWriteChatColor { nullptr };
extern "C" __declspec(dllexport) fMQSpawn g_pfOnAddSpawn { nullptr };
extern "C" __declspec(dllexport) fMQSpawn g_pfOnRemoveSpawn { nullptr };
extern "C" __declspec(dllexport) fMQGroundItem g_pfOnAddGroundItem { nullptr };
extern "C" __declspec(dllexport) fMQGroundItem g_pfOnRemoveGroundItem { nullptr };
extern "C" __declspec(dllexport) fMQBeginZone g_pfBeginZone { nullptr };
extern "C" __declspec(dllexport) fMQEndZone g_pfEndZone { nullptr };
extern "C" __declspec(dllexport) fMQZoned g_pfOnZoned { nullptr };

// Exported helper functions to make things easier in the managed world
extern "C" __declspec(dllexport) PCHAR __stdcall GetIniPath() { return gszINIPath; }

// Exported MQ2Type functions
extern "C" __declspec(dllexport) bool MQ2Type__FromData(MQ2Type * pThis, MQ2VARPTR & VarPtr, MQ2TYPEVAR & Source) { return pThis->FromData(VarPtr, Source); }
extern "C" __declspec(dllexport) bool MQ2Type__FromString(MQ2Type * pThis, MQ2VARPTR & VarPtr, PCHAR Source) { return pThis->FromString(VarPtr, Source); }
extern "C" __declspec(dllexport) void MQ2Type__InitVariable(MQ2Type * pThis, MQ2VARPTR & VarPtr) { pThis->InitVariable(VarPtr); }
extern "C" __declspec(dllexport) void MQ2Type__FreeVariable(MQ2Type * pThis, MQ2VARPTR & VarPtr) { pThis->FreeVariable(VarPtr); }
extern "C" __declspec(dllexport) bool MQ2Type__GetMember(MQ2Type * pThis, MQ2VARPTR VarPtr, PCHAR Member, PCHAR Index, MQ2TYPEVAR & Dest) { return pThis->GetMember(VarPtr, Member, Index, Dest); }
extern "C" __declspec(dllexport) bool MQ2Type__ToString(MQ2Type * pThis, MQ2VARPTR VarPtr, PCHAR Destination) { return pThis->ToString(VarPtr, Destination); }

PLUGIN_API VOID InitializePlugin(VOID)
{
	if (gszINIPath[0])
	{
		StringCbPrintfW(g_entryAssemblyLibraryPath, MAX_PATH, L"%hs\\%ws", gszINIPath, L"MQ2DotNetCore\\MQ2DotNetCore.dll");
		StringCbPrintfW(g_dotnetRuntimeConfigPath, MAX_PATH, L"%hs\\%ws", gszINIPath, L"MQ2DotNetCore\\MQ2DotNetCore.runtimeconfig.json");
		StringCbPrintfW(g_pluginLogFile, MAX_PATH, L"%hs\\%ws", gszINIPath, L"MQ2DotNetCore\\debug_plugin.log");
	}
	else
	{
		// If loaded by the test program, INIPath won't be set
		StringCbPrintfW(g_entryAssemblyLibraryPath, MAX_PATH, L"%ws", L"MQ2DotNetCore.dll");
		StringCbPrintfW(g_dotnetRuntimeConfigPath, MAX_PATH, L"%ws", L"MQ2DotNetCore.runtimeconfig.json");
		StringCbPrintfW(g_pluginLogFile, MAX_PATH, L"%ws", gszINIPath, L"debug_plugin.log");
	}

	g_bLoaded = loadDotNetClrAndExecuteEntryPoint();
	if (g_bLoaded)
	{
		WriteChatf("[MQ2DotNetCoreLoader] Successfully loaded the .net core CLR.");
	}
	else
	{
		WriteChatf("[MQ2DotNetCoreLoader] Failed to load .net CLR and/or execute the entry point method!");
	}
}

// The rest of the PLUGIN_API functions just call the callback (which will be set to the managed version of the function) if they're set and the CLR is loaded

PLUGIN_API VOID ShutdownPlugin(VOID)
{
	// Unfortunately, no way to fully unload. The default appdomain stays, as does any assembly loaded in it, e.g. MQ2DotNet.
	// The only side effect of this is the inability to patch this dll while a game instance is open.
	// Other assemblies are loaded in their own AppDomain, and can be unloaded any time
	if (g_bLoaded && g_pfShutdownPlugin)
	{
		g_pfShutdownPlugin();
	}

	// TODO: Determine if there is a way to unload the loaded libraries (hostfxr ?) without crashing the process?

	// TODO: Determine if the new hostfxr way of hosting the .net core runtime has a way it can be fully
	// unloaded at runtime?
}

PLUGIN_API VOID OnCleanUI(VOID)
{
	if (g_bLoaded && g_pfOnCleanUI)
		g_pfOnCleanUI();
}

PLUGIN_API VOID OnReloadUI(VOID)
{
	if (g_bLoaded && g_pfOnReloadUI)
		g_pfOnReloadUI();
}

PLUGIN_API VOID OnDrawHUD(VOID)
{
	if (g_bLoaded && g_pfOnDrawHUD)
		g_pfOnDrawHUD();
}

PLUGIN_API VOID SetGameState(DWORD GameState)
{
	if (g_bLoaded && g_pfSetGameState)
		g_pfSetGameState(GameState);
}

PLUGIN_API VOID OnPulse(VOID)
{
	if (g_bLoaded && g_pfOnPulse)
		g_pfOnPulse();
}

PLUGIN_API DWORD OnWriteChatColor(PCHAR Line, DWORD Color, DWORD Filter)
{
	if (g_bLoaded && g_pfOnWriteChatColor)
		return g_pfOnWriteChatColor(Line, Color, Filter);
	return 0;
}

PLUGIN_API DWORD OnIncomingChat(PCHAR Line, DWORD Color)
{
	if (g_bLoaded && g_pfOnIncomingChat)
		return g_pfOnIncomingChat(Line, Color);
	return 0;
}

PLUGIN_API VOID OnAddSpawn(PSPAWNINFO pNewSpawn)
{
	if (g_bLoaded && g_pfOnAddSpawn)
		g_pfOnAddSpawn(pNewSpawn);
}

PLUGIN_API VOID OnRemoveSpawn(PSPAWNINFO pSpawn)
{
	if (g_bLoaded && g_pfOnRemoveSpawn)
		g_pfOnRemoveSpawn(pSpawn);
}

PLUGIN_API VOID OnAddGroundItem(PGROUNDITEM pNewGroundItem)
{
	if (g_bLoaded && g_pfOnAddGroundItem)
		g_pfOnAddGroundItem(pNewGroundItem);
}

PLUGIN_API VOID OnRemoveGroundItem(PGROUNDITEM pGroundItem)
{
	if (g_bLoaded && g_pfOnRemoveGroundItem)
		g_pfOnRemoveGroundItem(pGroundItem);
}

PLUGIN_API VOID BeginZone(VOID)
{
	if (g_bLoaded && g_pfBeginZone)
		g_pfBeginZone();
}

PLUGIN_API VOID EndZone(VOID)
{
	if (g_bLoaded && g_pfEndZone)
		g_pfEndZone();
}

PLUGIN_API VOID OnZoned(VOID)
{
	if (g_bLoaded && g_pfOnZoned)
		g_pfOnZoned();
}

/********************************************************************************************
 * Function used to load and activate .NET Core
 * See: https://github.com/dotnet/samples/blob/master/core/hosting/HostWithHostFxr/src/NativeHost/nativehost.cpp
 ********************************************************************************************/

void* get_export(HMODULE hModule, const char* name)
{
	void* f = ::GetProcAddress(hModule, name);
	return f;
}

// DEBUGGING this plugin is a bitch since it loads libraries + the .net core runtime on initialization
// and then y they can't be unloaded, and the basic logging in MQ2 isn't as nice as I'd like so we'll
// log output to our own file when the loader is initializing

template<typename ... Args>
std::string formatString(const std::string& format, Args ... args)
{
	size_t size = snprintf(nullptr, 0, format.c_str(), args ...) + 1; // Extra space for '\0'
	if (size <= 0) { throw std::runtime_error("Error during formatting."); }
	std::unique_ptr<char[]> buf(new char[size]);
	snprintf(buf.get(), size, format.c_str(), args ...);
	return std::string(buf.get(), buf.get() + size - 1); // We don't want the '\0' inside
}

void logToFile(std::string message)
{
	std::fstream logFilestream;
	logFilestream.open(g_pluginLogFile, std::fstream::in | std::fstream::out | std::fstream::app);
	if (!logFilestream)
	{
		char* messageCString = new char[message.length() + 1];
		std::strcpy(messageCString, message.c_str());
		WriteChatf(messageCString);
		return;
	}

	time_t rawtime;
	struct tm* timeinfo;
	char buffer[80];

	time(&rawtime);
	timeinfo = localtime(&rawtime);

	strftime(buffer, sizeof(buffer), "%Y-%m-%d %H:%M:%S", timeinfo);
	std::string timestampString(buffer);

	logFilestream << "[ " << timestampString << " MQ2DotNetCoreLoader ]  " << message << "\n\n";

	logFilestream.close();
}

bool loadDotNetClrAndExecuteEntryPoint()
{
	WriteChatf("Loading .net core runtime and entry point...");

	const char_t* entryPointDotNetType = L"MQ2DotNetCore.LoaderEntryPoint, MQ2DotNetCore";
	const char_t* entryPointMethodName = L"InitializePlugin";

	//logToFile(std::string ("MQ2DotNetCore.dll"));
	logToFile(formatString("Entry Assembly Path: %ws", g_entryAssemblyLibraryPath));
	logToFile(formatString("Entry Point .Net Type: %ws", entryPointDotNetType));
	logToFile(formatString("Entry Point Method Name: %ws", entryPointMethodName));

	logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  Using nethost library to locate the hostfxr path...");


	hostfxr_initialize_for_runtime_config_fn hostfxrInitializeFunctionPointer = nullptr;
	hostfxr_get_runtime_delegate_fn hostfxrGetRuntimeDelegateFunctionPointer = nullptr;
	hostfxr_close_fn hostfxrCloseFunctionPointer = nullptr;

	// Pre-allocate a large buffer for the path to hostfxr
	char_t hostfxrPathBuffer[MAX_PATH];
	size_t hostfxrPathBufferSize = sizeof(hostfxrPathBuffer) / sizeof(char_t);
	int getHostfxrPathReturnCode = get_hostfxr_path(hostfxrPathBuffer, &hostfxrPathBufferSize, nullptr);
	if (getHostfxrPathReturnCode != 0)
	{
		logToFile(formatString("[ loadDotNetClrAndExecuteEntryPoint() ]  get_hostfxr_path(..) failed w/ return code: %d", getHostfxrPathReturnCode));
		return false;
	}

	logToFile(formatString("[ loadDotNetClrAndExecuteEntryPoint() ]  Loading the hostfxr library from:  %w", hostfxrPathBuffer));
	//WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] Loading the hostfxr library...");

	// Load hostfxr and get desired exports
	HMODULE hostfxrLibraryHandle = ::LoadLibraryW(hostfxrPathBuffer);
	hostfxrInitializeFunctionPointer = (hostfxr_initialize_for_runtime_config_fn)get_export(hostfxrLibraryHandle, "hostfxr_initialize_for_runtime_config");
	hostfxrGetRuntimeDelegateFunctionPointer = (hostfxr_get_runtime_delegate_fn)get_export(hostfxrLibraryHandle, "hostfxr_get_runtime_delegate");
	hostfxrCloseFunctionPointer = (hostfxr_close_fn)get_export(hostfxrLibraryHandle, "hostfxr_close");


	bool areAllFunctionPointersSet = true;
	if (!hostfxrInitializeFunctionPointer || hostfxrInitializeFunctionPointer == nullptr)
	{
		areAllFunctionPointersSet = false;
		logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  get_export(..) failed to locate/set the hostfxrInitializeFunctionPointer");
	}

	if (!hostfxrGetRuntimeDelegateFunctionPointer || hostfxrGetRuntimeDelegateFunctionPointer == nullptr)
	{
		areAllFunctionPointersSet = false;
		logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  get_export(..) failed to locate/set the hostfxrGetRuntimeDelegateFunctionPointer");
	}

	if (!hostfxrCloseFunctionPointer || hostfxrCloseFunctionPointer == nullptr)
	{
		areAllFunctionPointersSet = false;
		logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  get_export(..) failed to locate/set the hostfxrCloseFunctionPointer");
	}

	if (!areAllFunctionPointersSet) {
		return false;
	}

	logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  hostfxr library loaded, initializing .net core runtime...");

	// Load .NET Core
	void* hostfxrLoadAssemblyAndGetFunctionPointer = nullptr;
	hostfxr_handle hostfxr_context = nullptr;
	int hostfxrInitializeReturnCode = hostfxrInitializeFunctionPointer(g_dotnetRuntimeConfigPath, nullptr, &hostfxr_context);
	if (hostfxrInitializeReturnCode != 0)
	{
		logToFile(formatString("[ loadDotNetClrAndExecuteEntryPoint() ]  The hostfxr inititialize(..) (function pointer) call returned a non zero exit code: %d!", hostfxrInitializeReturnCode));
		hostfxrCloseFunctionPointer(hostfxr_context);
		return nullptr;
	}

	if (hostfxr_context == nullptr)
	{
		logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  The hostfxr inititialize(..) (function pointer) did not succeed, the hostfxr_context is equal to the nullptr value!");
		hostfxrCloseFunctionPointer(hostfxr_context);
		return nullptr;
	}

	logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  Getting the hdt_load_assembly_and_get_function_pointer from the .net runtime...");

	// Get the load assembly function pointer
	int hostfxrGetDelegateFunctionPointerReturnCode = hostfxrGetRuntimeDelegateFunctionPointer(
		hostfxr_context,
		hdt_load_assembly_and_get_function_pointer,
		&hostfxrLoadAssemblyAndGetFunctionPointer
	);

	if (hostfxrGetDelegateFunctionPointerReturnCode != 0)
	{
		logToFile(formatString("[ loadDotNetClrAndExecuteEntryPoint() ]  The hostfxr getRuntimeDelegate(..) (function pointer) call returned a non zero exit code: %d!", hostfxrGetDelegateFunctionPointerReturnCode));
		hostfxrCloseFunctionPointer(hostfxr_context);
		return false;
	}

	if (hostfxrLoadAssemblyAndGetFunctionPointer == nullptr)
	{
		logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  The hostfxr getRuntimeDelegate(..) (function pointer) call returned a did not succeed, the hostfxrLoadAssemblyAndGetFunctionPointer is set to a nullptr value!");
		hostfxrCloseFunctionPointer(hostfxr_context);
		return false;
	}


	load_assembly_and_get_function_pointer_fn dotnetLoadAssemblyAndGetFunctionPointer = nullptr;
	dotnetLoadAssemblyAndGetFunctionPointer = (load_assembly_and_get_function_pointer_fn)hostfxrLoadAssemblyAndGetFunctionPointer;

	// We don't need to customize the entry point delegate signature at this point we can just
	// use the default that takes an IntPtr args and args size... not something we're currently using
	//typedef void (CORECLR_DELEGATE_CALLTYPE* custom_entry_point_fn)(lib_args args);
	//custom_entry_point_fn custom = nullptr;

	logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  Loading the entry point assembly and method...");

	// Function pointer to managed delegate
	component_entry_point_fn initializePluginFunctionPointer = nullptr;
	int loadEntryPointReturnCode = dotnetLoadAssemblyAndGetFunctionPointer(
		//L"MQ2DotNetCore.dll",
		g_entryAssemblyLibraryPath,
		entryPointDotNetType,
		entryPointMethodName,
		nullptr, // Pass nullptr for default delegate type
		nullptr,
		(void**)&initializePluginFunctionPointer
	);

	logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  Closing the hostfxr context...");
	hostfxrCloseFunctionPointer(hostfxr_context);

	if (loadEntryPointReturnCode != 0)
	{
		logToFile(formatString("[ loadDotNetClrAndExecuteEntryPoint() ]  The loadEntryPointReturnCode is non zero: %d", loadEntryPointReturnCode));
		return false;
	}

	if (initializePluginFunctionPointer == nullptr)
	{
		logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  The initializePluginFunctionPointer is set to the nullptr value!");
		return false;
	}

	struct entryPointArguments
	{
		const char_t* message;
	};


	wchar_t loaderAssemblyPath[MAX_PATH] = { 0 };
	StringCbPrintfW(loaderAssemblyPath, MAX_PATH, L"%hs\\%ws", gszINIPath, L"MQ2DotNetCoreLoader.dll");
	entryPointArguments args
	{
		loaderAssemblyPath
	};

	logToFile("[ loadDotNetClrAndExecuteEntryPoint() ]  Calling the InitializePlugin(..) entry point method...");

	int initializePluginEntryPointMethodReturnCode = initializePluginFunctionPointer(&args, sizeof(args));
	if (initializePluginEntryPointMethodReturnCode != 0)
	{
		logToFile(formatString("[ loadDotNetClrAndExecuteEntryPoint() ]  The InitializePlugin(..) entry point method returned a non zero return code: %d", initializePluginEntryPointMethodReturnCode));
		return false;
	}

	return true;
}