#pragma once

#include <ClrPhlib.h>
#include <UnmanagedSymPrv.h>

using namespace Dependencies::ClrPh;

// Symbol resolution and undecoration utility class
public ref class PhSymbolProvider
{
public:
    PhSymbolProvider();
    ~PhSymbolProvider();
    !PhSymbolProvider();

    virtual Tuple<CLRPH_DEMANGLER, String^>^ UndecorateName(_In_ String ^DecoratedName);

protected:

	String^ UndecorateNameDemumble(_In_ String ^DecoratedName);
	String^ UndecorateNameLLVMItanium(_In_ String ^DecoratedName);
	String^ UndecorateNameLLVMMicrosoft(_In_ String ^DecoratedName);
	String^ UndecorateNamePh(_In_ String ^DecoratedName);


private:
	String ^ UndecorateNamePrv(_In_ String ^DecoratedName, _In_ DemangleNameFn Demangler);

    UnmanagedSymPrv *m_Impl;

};