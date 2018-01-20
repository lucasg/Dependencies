#include <ClrPhlib.h>
#include <UnmanagedPh.h>

using namespace System;
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
	PPH_STRING UndecoratedName = NULL;
	wchar_t* PvDecoratedName = (wchar_t*)(Marshal::StringToHGlobalUni(DecoratedName)).ToPointer();

	if (!m_Impl) {
		return gcnew String("");
	}
		

	
	UndecoratedName = PhUndecorateNameW(
		m_Impl->m_SymbolProvider,
		PvDecoratedName
	);

	if (!UndecoratedName) {
		return gcnew String("");
	}

	ManagedUndName = gcnew String(UndecoratedName->Buffer);
	PhDereferenceObject(UndecoratedName);
	Marshal::FreeHGlobal(IntPtr((void*)PvDecoratedName));

	return ManagedUndName;
}