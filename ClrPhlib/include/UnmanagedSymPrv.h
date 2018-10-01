#pragma once

#include <stdbool.h>

#include <ph.h>
#include <symprv.h>




class UnmanagedSymPrv {
public :

    static UnmanagedSymPrv* Create();

	bool DemangleName(
		wchar_t* DecoratedName,
		size_t DecoratedNameLen,
		wchar_t** UndecoratedName,
		size_t* UndecoratedNameLen
	);

private:
    PPH_SYMBOL_PROVIDER m_SymbolProvider;
};