#include <ClrPhlib.h>
#include <UnmanagedPh.h>

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

String^ PhSymbolProvider::UndecorateName(
	_In_ String ^DecoratedName
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
	

	if (m_Impl->DemangleName(
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