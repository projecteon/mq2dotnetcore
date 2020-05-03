#define TEST

#include "../../../MQ2Plugin.h"

#include "libs/nethost-win-x86/nethost.h"

#include "includes/coreclr_delegates.h"
#include "includes/hostfxr.h"

#include <strsafe.h>

//#import <mscorlib.tlb> raw_interfaces_only			\
//	auto_rename											\
//	high_property_prefixes("_get","_put","_putref")		\
//	rename("ReportEvent", "InteropServices_ReportEvent")


PLUGIN_VERSION(1.0);
PreSetup("MQ2DotNetCoreLoader");

bool g_bLoaded{ false };
wchar_t g_entryAssemblyLibraryPath[MAX_PATH] = { 0 };
wchar_t g_dotnetRuntimeConfigPath[MAX_PATH] = { 0 };

void* get_export(HMODULE hModule, const char* name);

bool loadDotNetClrAndExecuteEntryPoint();

// Functions in the managed dll. All standard plugin callbacks except initialize, since there's no point having that
extern "C" __declspec(dllexport) fMQShutdownPlugin g_pfShutdownPlugin{ nullptr };
extern "C" __declspec(dllexport) fMQCleanUI g_pfOnCleanUI{ nullptr };
extern "C" __declspec(dllexport) fMQReloadUI g_pfOnReloadUI{ nullptr };
extern "C" __declspec(dllexport) fMQDrawHUD g_pfOnDrawHUD{ nullptr };
extern "C" __declspec(dllexport) fMQSetGameState g_pfSetGameState{ nullptr };
extern "C" __declspec(dllexport) fMQPulse g_pfOnPulse{ nullptr };
extern "C" __declspec(dllexport) fMQIncomingChat g_pfOnIncomingChat{ nullptr };
extern "C" __declspec(dllexport) fMQWriteChatColor g_pfOnWriteChatColor{ nullptr };
extern "C" __declspec(dllexport) fMQSpawn g_pfOnAddSpawn{ nullptr };
extern "C" __declspec(dllexport) fMQSpawn g_pfOnRemoveSpawn{ nullptr };
extern "C" __declspec(dllexport) fMQGroundItem g_pfOnAddGroundItem{ nullptr };
extern "C" __declspec(dllexport) fMQGroundItem g_pfOnRemoveGroundItem{ nullptr };
extern "C" __declspec(dllexport) fMQBeginZone g_pfBeginZone{ nullptr };
extern "C" __declspec(dllexport) fMQEndZone g_pfEndZone{ nullptr };
extern "C" __declspec(dllexport) fMQZoned g_pfOnZoned{ nullptr };

// Exported helper functions to make things easier in the managed world
extern "C" __declspec(dllexport) PCHAR __stdcall GetIniPath() { return gszINIPath; }

// Exported MQ2Type functions
extern "C" __declspec(dllexport) bool MQ2Type__FromData(MQ2Type * pThis, MQ2VARPTR &VarPtr, MQ2TYPEVAR &Source) { return pThis->FromData(VarPtr, Source); }
extern "C" __declspec(dllexport) bool MQ2Type__FromString(MQ2Type * pThis, MQ2VARPTR &VarPtr, PCHAR Source) { return pThis->FromString(VarPtr, Source); }
extern "C" __declspec(dllexport) void MQ2Type__InitVariable(MQ2Type * pThis, MQ2VARPTR &VarPtr) { pThis->InitVariable(VarPtr); }
extern "C" __declspec(dllexport) void MQ2Type__FreeVariable(MQ2Type * pThis, MQ2VARPTR &VarPtr) { pThis->FreeVariable(VarPtr); }
extern "C" __declspec(dllexport) bool MQ2Type__GetMember(MQ2Type * pThis, MQ2VARPTR VarPtr, PCHAR Member, PCHAR Index, MQ2TYPEVAR &Dest) { return pThis->GetMember(VarPtr, Member, Index, Dest); }
extern "C" __declspec(dllexport) bool MQ2Type__ToString(MQ2Type * pThis, MQ2VARPTR VarPtr, PCHAR Destination) { return pThis->ToString(VarPtr, Destination); }

PLUGIN_API VOID InitializePlugin(VOID)
{
	if (gszINIPath[0])
	{
		StringCbPrintfW(g_entryAssemblyLibraryPath, MAX_PATH, L"%hs\\%ws", gszINIPath, L"MQ2DotNetCore\\MQ2DotNetCore.dll");
		StringCbPrintfW(g_dotnetRuntimeConfigPath, MAX_PATH, L"%hs\\%ws", gszINIPath, L"MQ2DotNetCore\\MQ2DotNetCore.runtimeconfig.json");
	}
	else
	{
		// If loaded by the test program, INIPath won't be set
		StringCbPrintfW(g_entryAssemblyLibraryPath, MAX_PATH, L"%ws", L"MQ2DotNetCore.dll");
		StringCbPrintfW(g_dotnetRuntimeConfigPath, MAX_PATH, L"%ws",L"MQ2DotNetCore.runtimeconfig.json");
	}

	g_bLoaded = loadDotNetClrAndExecuteEntryPoint();
	if (!g_bLoaded)
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
		g_pfShutdownPlugin();
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

bool loadDotNetClrAndExecuteEntryPoint()
{
	// Globals to hold hostfxr exports
	hostfxr_initialize_for_runtime_config_fn hostfxrInitializeFunctionPointer;
	hostfxr_get_runtime_delegate_fn hostfxrGetRuntimeDelegateFunctionPointer;
	hostfxr_close_fn hostfxrCloseFunctionPointer;

	WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] Using nethost library to locate the hostfxr path...");

	// Pre-allocate a large buffer for the path to hostfxr
	char_t hostfxrPathBuffer[MAX_PATH];
	size_t hostfxrPathBufferSize = sizeof(hostfxrPathBuffer) / sizeof(char_t);
	int getHostfxrPathReturnCode = get_hostfxr_path(hostfxrPathBuffer, &hostfxrPathBufferSize, nullptr);
	if (getHostfxrPathReturnCode != 0)
	{
		WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] get_hostfxr_path(..) failed w/ return code: %d", getHostfxrPathReturnCode);
		return false;
	}

	WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] Loading the hostfxr library...");

	// Load hostfxr and get desired exports
	HMODULE hostfxrLibraryHandle = ::LoadLibraryW(hostfxrPathBuffer);
	hostfxrInitializeFunctionPointer = (hostfxr_initialize_for_runtime_config_fn)get_export(hostfxrLibraryHandle, "hostfxr_initialize_for_runtime_config");
	hostfxrGetRuntimeDelegateFunctionPointer = (hostfxr_get_runtime_delegate_fn)get_export(hostfxrLibraryHandle, "hostfxr_get_runtime_delegate");
	hostfxrCloseFunctionPointer = (hostfxr_close_fn)get_export(hostfxrLibraryHandle, "hostfxr_close");


	bool areAllFunctionPointersSet = true;
	if (!hostfxrInitializeFunctionPointer || hostfxrInitializeFunctionPointer == nullptr)
	{
		areAllFunctionPointersSet = false;
		WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] get_export(..) failed to locate/set the hostfxrInitializeFunctionPointer");
	}

	if (!hostfxrGetRuntimeDelegateFunctionPointer || hostfxrGetRuntimeDelegateFunctionPointer == nullptr)
	{
		areAllFunctionPointersSet = false;
		WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] get_export(..) failed to locate/set the hostfxrGetRuntimeDelegateFunctionPointer");
	}

	if (!hostfxrCloseFunctionPointer || hostfxrCloseFunctionPointer == nullptr)
	{
		areAllFunctionPointersSet = false;
		WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] get_export(..) failed to locate/set the hostfxrCloseFunctionPointer");
	}

	if (!areAllFunctionPointersSet) {
		return false;
	}

	WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] hostfxr library loaded, initializing .net core runtime...");

	// Load .NET Core
	void* hostfxrLoadAssemblyAndGetFunctionPointer = nullptr;
	hostfxr_handle hostfxr_context = nullptr;
	int hostfxrInitializeReturnCode = hostfxrInitializeFunctionPointer(g_dotnetRuntimeConfigPath, nullptr, &hostfxr_context);
	if (hostfxrInitializeReturnCode != 0 || hostfxr_context == nullptr)
	{
		WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] The hostfxr inititialize(..) (function pointer) call returned a non zero exit code or a nullptr for the context handle!");
		hostfxrCloseFunctionPointer(hostfxr_context);
		return nullptr;
	}

	WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] Getting the hdt_load_assembly_and_get_function_pointer from the .net runtime...");

	// Get the load assembly function pointer
	int hostfxrGetDelegateFunctionPointerReturnCode = hostfxrGetRuntimeDelegateFunctionPointer(
		hostfxr_context,
		hdt_load_assembly_and_get_function_pointer,
		&hostfxrLoadAssemblyAndGetFunctionPointer
	);

	if (hostfxrGetDelegateFunctionPointerReturnCode != 0 || hostfxrLoadAssemblyAndGetFunctionPointer == nullptr)
	{
		WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] The hostfxr getRuntimeDelegate(..) (function pointer) call returned a non zero exit code or the hostfxrLoadAssemblyAndGetFunctionPointer is set to a nullptr value!");
		hostfxrCloseFunctionPointer(hostfxr_context);
		return false;
	}

	hostfxrCloseFunctionPointer(hostfxr_context);

	load_assembly_and_get_function_pointer_fn dotnetLoadAssemblyAndGetFunctionPointer = (load_assembly_and_get_function_pointer_fn)hostfxrLoadAssemblyAndGetFunctionPointer;

	WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] Loading the entry point assembly and method...");

	const char_t *entryPointDotNetType = L"MQ2DotNetCore.LoaderEntryPoint, MQ2DotNetCore";
	const char_t *entryPointMethodName = L"InitializePlugin";

	// We don't need to customize the entry point delegate signature at this point we can just
	// use the default that takes an IntPtr args and args size... not something we're currently using
	//typedef void (CORECLR_DELEGATE_CALLTYPE* custom_entry_point_fn)(lib_args args);
	//custom_entry_point_fn custom = nullptr;

	// Function pointer to managed delegate
	component_entry_point_fn initializePluginFunctionPointer = nullptr;
	int loadEntryPointReturnCode = dotnetLoadAssemblyAndGetFunctionPointer(
		g_entryAssemblyLibraryPath,
		entryPointDotNetType,
		entryPointMethodName,
		nullptr, // 
		nullptr,
		(void**)&initializePluginFunctionPointer
	);

	if (loadEntryPointReturnCode != 0 || initializePluginFunctionPointer == nullptr)
	{
		WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] The loadEntryPointReturnCode is non zero or the initializePluginFunctionPointer is set to a nullptr value!");
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

	WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] Calling the InitializePlugin(..) entry point method...");

	int initializePluginEntryPointMethodReturnCode = initializePluginFunctionPointer(&args, sizeof(args));
	if (initializePluginEntryPointMethodReturnCode != 0)
	{
		WriteChatf("[MQ2DotNetCoreLoader - loadDotNetClrAndExecuteEntryPoint()] The InitializePlugin(..) entry point method returned a non zero return code!");
		return false;
	}

	return true;
}