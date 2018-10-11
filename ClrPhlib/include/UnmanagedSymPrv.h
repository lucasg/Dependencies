#pragma once

#include <stdbool.h>

#include <ph.h>
#include <symprv.h>
#include <ClrPhlib.h>


// Native Symbol Provider class.
// Allow the application to unmangle C and C++ mangled names.
class UnmanagedSymPrv {

public:

	// Initialize a new provider.
	static UnmanagedSymPrv* Create();

	// Attempt to demangle a C/C++ name. Return false if the name is not mangled.
	bool DemangleName(
		_In_ wchar_t* DecoratedName,
		_In_ size_t DecoratedNameLen,
		_Out_ wchar_t** UndecoratedName,
		_Out_ size_t* UndecoratedNameLen,
		_Out_ Dependencies::ClrPh::CLRPH_DEMANGLER *Demangler
	);

public:
	PPH_SYMBOL_PROVIDER m_SymbolProvider;
};




typedef bool(__cdecl *DemangleNameFn)	(UnmanagedSymPrv*, wchar_t*, size_t, wchar_t**, size_t*);

bool DemumbleDemangleName(
	_In_ UnmanagedSymPrv* obj,
	_In_ wchar_t* DecoratedName,
	_In_ size_t DecoratedNameLen,
	_Out_ wchar_t** UndecoratedName,
	_Out_ size_t* UndecoratedNameLen
);

bool LLVMItaniumDemangleName(
	_In_ UnmanagedSymPrv* obj,
	_In_ wchar_t* DecoratedName,
	_In_ size_t DecoratedNameLen,
	_Out_ wchar_t** UndecoratedName,
	_Out_ size_t* UndecoratedNameLen
);

bool LLVMMicrosoftDemangleName(
	_In_ UnmanagedSymPrv* obj,
	_In_ wchar_t* DecoratedName,
	_In_ size_t DecoratedNameLen,
	_Out_ wchar_t** UndecoratedName,
	_Out_ size_t* UndecoratedNameLen
);

bool UndecorateSymbolDemangleName(
	_In_ UnmanagedSymPrv* obj,
	_In_ wchar_t* DecoratedName,
	_In_ size_t DecoratedNameLen,
	_Out_ wchar_t** UndecoratedName,
	_Out_ size_t* UndecoratedNameLen
);

const DemangleNameFn Demanglers[] = {
	DemumbleDemangleName,				// Undecorate name using demumble library
	LLVMItaniumDemangleName,			// Undecorate name using llvm::itaniumDemangle library
	LLVMMicrosoftDemangleName,			// Undecorate name using llvm::microsoftDemangle library
	UndecorateSymbolDemangleName		// Undecorate name using UnDecorateSymbolNameW library
};