#include <UnmanagedSymPrv.h>


using namespace System;
using namespace Dependencies::ClrPh;

#define DBGHELP_RELPATH 

VOID PvpGetDefaultPathForDbgHelp(
	_Inout_ PWSTR DefaultDbgHelpPath
)
{

#if _WIN64
	wsprintf((LPWSTR)DefaultDbgHelpPath, L"%s%s", _wgetenv(L"ProgramFiles"), L"\\Windows Kits\\10\\Debuggers\\x64\\dbghelp.dll");
#define DBGHELP_PATH _wgetenv(L"ProgramFiles") L"" DBGHELP_RELPATH 
#else
	if (PhIsExecutingInWow64())
	{
		wsprintf((LPWSTR)DefaultDbgHelpPath, L"%s%s", _wgetenv(L"ProgramFiles(x86)"), L"\\Windows Kits\\10\\Debuggers\\x86\\dbghelp.dll");
	}
	else
	{
		wsprintf((LPWSTR)DefaultDbgHelpPath, L"%s%s", _wgetenv(L"ProgramFiles"), L"\\Windows Kits\\10\\Debuggers\\x86\\dbghelp.dll");
	}
#endif // _WIN64

}

/*!
	@brief PhLoadLibrarySafe prevents the loader from searching in an unsafe
	 order by first requiring the loader try to load and resolve through
	 System32. Then upping the loading flags until the library is loaded.

	@param[in] LibFileName - The file name of the library to load.

	@return HMODULE to the library on success, null on failure.
*/
_Ret_maybenull_
PVOID
PhLoadLibrarySafe(
	_In_ PCWSTR LibFileName
)
{
	PVOID baseAddress;

	//
	// Force LOAD_LIBRARY_SEARCH_SYSTEM32. If the library file name is a fully
	// qualified path this will succeed.
	//
	baseAddress = LoadLibraryExW(LibFileName,
		NULL,
		LOAD_LIBRARY_SEARCH_SYSTEM32);
	if (baseAddress)
	{
		return baseAddress;
	}

	//
	// Include the application directory now.
	//
	return LoadLibraryExW(LibFileName,
		NULL,
		LOAD_LIBRARY_SEARCH_SYSTEM32 |
		LOAD_LIBRARY_SEARCH_APPLICATION_DIR);
}

VOID PvpLoadDbgHelpFromPath(
    _In_ PWSTR DbgHelpPath
)
{
    HMODULE dbghelpModule;
	
	// try to load from windows kits installed on the machine
	dbghelpModule = LoadLibrary(DbgHelpPath);

	// try system32, and then current dir
	if (!dbghelpModule)
	{
		dbghelpModule = (HMODULE) PhLoadLibrarySafe(L"dbghelp.dll");
	}

	// try to load symsrv.dll from the same folder as dbghelp.dll
    if (dbghelpModule)
    {
        PPH_STRING fullDbghelpPath;
        ULONG indexOfFileName;
        PH_STRINGREF dbghelpFolder;
        PPH_STRING symsrvPath;

        fullDbghelpPath = PhGetDllFileName(dbghelpModule, &indexOfFileName);

        if (fullDbghelpPath)
        {
            if (indexOfFileName != 0)
            {
                static PH_STRINGREF symsrvString = PH_STRINGREF_INIT(L"\\symsrv.dll");

                dbghelpFolder.Buffer = fullDbghelpPath->Buffer;
                dbghelpFolder.Length = indexOfFileName * sizeof(WCHAR);

                symsrvPath = PhConcatStringRef2(&dbghelpFolder, &symsrvString);

                LoadLibrary(symsrvPath->Buffer);

                PhDereferenceObject(symsrvPath);
            }

            PhDereferenceObject(fullDbghelpPath);
        }

		PhSymbolProviderCompleteInitialization(dbghelpModule);
    }
    //else
    //{
    //    dbghelpModule = LoadLibrary(L"dbghelp.dll");
    //}

    /*PhSymbolProviderCompleteInitialization(dbghelpModule);*/
}

BOOLEAN PvpLoadDbgHelp(
    _Inout_ PPH_SYMBOL_PROVIDER *SymbolProvider
)
{
    static UNICODE_STRING symbolPathVarName = RTL_CONSTANT_STRING(L"_NT_SYMBOL_PATH");
    PPH_STRING symbolSearchPath;
    PPH_SYMBOL_PROVIDER symbolProvider;
    WCHAR buffer[MAX_PATH] = L"";
    UNICODE_STRING symbolPathUs;
	const wchar_t DefaultDbgHelpPath[MAX_PATH] = { 0 };

    
    symbolPathUs.Buffer = buffer;
    symbolPathUs.Length = sizeof(buffer) - sizeof(UNICODE_NULL);
    symbolPathUs.MaximumLength = sizeof(buffer);
    

    if (!PhSymbolProviderInitialization())
        return FALSE;

	PvpGetDefaultPathForDbgHelp((PWSTR)DefaultDbgHelpPath);
    PvpLoadDbgHelpFromPath((PWSTR)DefaultDbgHelpPath);

    symbolProvider = PhCreateSymbolProvider(NULL);

    // Load symbol path from _NT_SYMBOL_PATH if configured by the user.    
    if (NT_SUCCESS(RtlQueryEnvironmentVariable_U(NULL, &symbolPathVarName, &symbolPathUs)))
    {
        symbolSearchPath = PhFormatString(L"SRV*%s*http://msdl.microsoft.com/download/symbols", symbolPathUs.Buffer);
    }
    else
    {
        symbolSearchPath = PhCreateString(L"SRV**http://msdl.microsoft.com/download/symbols");
    }

    PhSetSearchPathSymbolProvider(symbolProvider, symbolSearchPath->Buffer);
    PhDereferenceObject(symbolSearchPath);

    *SymbolProvider = symbolProvider;
    return TRUE;
}


UnmanagedSymPrv* UnmanagedSymPrv::Create()
{
    UnmanagedSymPrv *Instance = new UnmanagedSymPrv;
    
    if (!PvpLoadDbgHelp(&Instance->m_SymbolProvider)) {
        delete Instance;
        return NULL;
    }

    return Instance;
}