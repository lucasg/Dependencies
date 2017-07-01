#include <ClrPhlib.h>
#include <UnmanagedPh.h>
#include <atlstr.h>

using namespace System;
using namespace ClrPh;

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
	CString PvDecoratedName(DecoratedName);

	if (!m_Impl) {
		return gcnew String("");
	}
		

	
	UndecoratedName = PhUndecorateNameW(
		m_Impl->m_SymbolProvider->ProcessHandle,
		PvDecoratedName.GetBuffer()
	);

	if (!UndecoratedName) {
		return gcnew String("");
	}

	ManagedUndName = gcnew String(UndecoratedName->Buffer);
	PhDereferenceObject(UndecoratedName);

	return ManagedUndName;
}