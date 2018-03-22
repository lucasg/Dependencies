#include <ClrPhlib.h>
#include <UnmanagedPh.h>

using namespace System;
using namespace ClrPh;
using namespace Runtime::InteropServices;


extern "C" {
char* __cxa_demangle(const char* mangled_name,
	char* buf,
	size_t* n,
	int* status);
}

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
	int status;
	String ^ManagedUndName;
	PPH_STRING UndecoratedName = NULL;
	
	if (!m_Impl || DecoratedName->Length == 0) {
		return gcnew String("");
	}
	
	// try to undecorate GCC/LLVM symbols
	wchar_t* PvDecoratedName = (wchar_t*)(Marshal::StringToHGlobalUni(DecoratedName)).ToPointer();
	size_t len = wcslen(PvDecoratedName);
	char *PvDecoratedNameAscii = (char*)malloc(len * 2);
	sprintf_s(PvDecoratedNameAscii, len * 2, "%ws", PvDecoratedName);

	char *UndecoratedNameAscii = __cxa_demangle(
		PvDecoratedNameAscii,
		NULL,
		NULL,
		&status
	);

	if (!status)
	{
		ManagedUndName = gcnew String(UndecoratedNameAscii);
		
		free(PvDecoratedNameAscii);
		Marshal::FreeHGlobal(IntPtr((void*)PvDecoratedName));
		return ManagedUndName;
	}

	// try to undecorate MSVC symbols
	UndecoratedName = PhUndecorateNameW(
		m_Impl->m_SymbolProvider,
		PvDecoratedName
	);

	if (UndecoratedName) {

		ManagedUndName = gcnew String(UndecoratedName->Buffer);
		PhDereferenceObject(UndecoratedName);
		Marshal::FreeHGlobal(IntPtr((void*)PvDecoratedName));

		return ManagedUndName;
	}



	Marshal::FreeHGlobal(IntPtr((void*)PvDecoratedName));
	return gcnew String("");
}