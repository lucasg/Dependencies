#include "UnmanagedSymPrv.h"

using namespace System;
using namespace Dependencies::ClrPh;

BOOLEAN PvpLoadDbgHelp(
    _Inout_ PPH_SYMBOL_PROVIDER *SymbolProvider
)
{
    UNICODE_STRING symbolPathVarName;
    PPH_STRING symbolSearchPath;
    PPH_SYMBOL_PROVIDER symbolProvider;
    WCHAR buffer[MAX_PATH] = L"";
    UNICODE_STRING symbolPathUs;

    RtlInitUnicodeString(&symbolPathVarName, L"_NT_SYMBOL_PATH");
    symbolPathUs.Buffer = buffer;
    symbolPathUs.Length = sizeof(buffer) - sizeof(UNICODE_NULL);
    symbolPathUs.MaximumLength = sizeof(buffer);

    // phlib initializes DbgHelp on first use, including loading the matching SDK DLLs.
    symbolProvider = PhCreateSymbolProvider(NULL);

    // Load symbol path from _NT_SYMBOL_PATH if configured by the user.
    if (NT_SUCCESS(RtlQueryEnvironmentVariable_U(NULL, &symbolPathVarName, &symbolPathUs)))
    {
        symbolSearchPath = PhFormatString(const_cast<PWSTR>(L"SRV*%s*http://msdl.microsoft.com/download/symbols"), symbolPathUs.Buffer);
    }
    else
    {
        symbolSearchPath = PhCreateString(const_cast<PWSTR>(L"SRV**http://msdl.microsoft.com/download/symbols"));
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
