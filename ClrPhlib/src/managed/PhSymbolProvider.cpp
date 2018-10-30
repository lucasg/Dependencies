#include <ClrPhlib.h>
#include <UnmanagedPh.h>
#include <ClrPhSymbolProvider.h>

using namespace Dependencies;
using namespace ClrPh;
using namespace Runtime::InteropServices;




PhSymbolProvider::PhSymbolProvider()
:m_Impl(UnmanagedSymPrv::Create())
{
}

PhSymbolProvider::~PhSymbolProvider()
{
	if (m_Impl) {
		delete m_Impl;
	}
		
}

PhSymbolProvider::!PhSymbolProvider()
{
	if (m_Impl) {
		delete m_Impl;
	}

}

String^ PhSymbolProvider::UndecorateNameDemumble(
	_In_ String ^DecoratedName
)
{
	return UndecorateNamePrv(DecoratedName, DemumbleDemangleName);
}

String^ PhSymbolProvider::UndecorateNameLLVMItanium(_In_ String ^DecoratedName)
{
	return UndecorateNamePrv(DecoratedName, LLVMItaniumDemangleName);
}

String^ PhSymbolProvider::UndecorateNameLLVMMicrosoft(_In_ String ^DecoratedName)
{
	return UndecorateNamePrv(DecoratedName, LLVMMicrosoftDemangleName);
}

String^ PhSymbolProvider::UndecorateNamePh(_In_ String ^DecoratedName)
{
	return UndecorateNamePrv(DecoratedName, UndecorateSymbolDemangleName);
}

Tuple<CLRPH_DEMANGLER, String^>^  PhSymbolProvider::UndecorateName(_In_ String ^DecoratedName)
{
	String ^ManagedUndName;
	wchar_t* UndecoratedName = NULL;
	size_t UndecoratedNameLen = 0;
	CLRPH_DEMANGLER Demangler = CLRPH_DEMANGLER::None;

	if (!m_Impl || !DecoratedName || DecoratedName->Length == 0) {
		return gcnew Tuple<CLRPH_DEMANGLER, String^> (Demangler, gcnew String(""));
	}

	wchar_t* PvDecoratedName = (wchar_t*)(Marshal::StringToHGlobalUni(DecoratedName)).ToPointer();
	size_t PvDecoratedNameLen = wcslen(PvDecoratedName);


	if (m_Impl->DemangleName(
		PvDecoratedName,
		PvDecoratedNameLen,
		&UndecoratedName,
		&UndecoratedNameLen,
		&Demangler
	))
	{
		ManagedUndName = gcnew String(UndecoratedName);
	}
	else
	{
		ManagedUndName = gcnew String("");
	}

	if (UndecoratedName)
	{
		free(UndecoratedName);
	}

	if (PvDecoratedName)
	{
		Marshal::FreeHGlobal(IntPtr((void*)PvDecoratedName));
	}


	return gcnew Tuple<CLRPH_DEMANGLER, String^>(Demangler, ManagedUndName);
}

String^ PhSymbolProvider::UndecorateNamePrv(
	_In_ String ^DecoratedName,
	_In_ DemangleNameFn Demangler

)
{
	String ^ManagedUndName;
	wchar_t* UndecoratedName = NULL;
	size_t UndecoratedNameLen = 0;
	
	if (!m_Impl || DecoratedName->Length == 0) {
		return gcnew String("");
	}
	
	wchar_t* PvDecoratedName = (wchar_t*)(Marshal::StringToHGlobalUni(DecoratedName)).ToPointer();
	size_t PvDecoratedNameLen = wcslen(PvDecoratedName);
	

	if (Demangler(
		this->m_Impl,
		PvDecoratedName,
		PvDecoratedNameLen,
		&UndecoratedName,
		&UndecoratedNameLen
	))
	{
		ManagedUndName = gcnew String(UndecoratedName);
	}
	else
	{
		ManagedUndName = gcnew String("");
	}

	if (UndecoratedName) 
	{	
		free(UndecoratedName);
	}

	if (PvDecoratedName)
	{
		Marshal::FreeHGlobal(IntPtr((void*)PvDecoratedName));
	}


	return ManagedUndName;
}