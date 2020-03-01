#include <UnmanagedSymPrv.h>
#include <llvm/Demangle/Demangle.h>
#include <stdlib.h>
#using <System.dll>

extern "C" {
	char* __cxa_demangle(const char* mangled_name,
		char* buf,
		size_t* n,
		int* status);
}

#define DEMANGLER_DEBUGLOG_ON		(false)
#define DEMANGLER_DEBUGLOG_CAT ("demangler")
#define DEMANGLER_DEBUGLOG_ONE (DemanglerDebugOneArg)
#define DEMANGLER_DEBUGLOG_TWO (DemanglerDebugTwoArg)

void DemanglerDebugOneArg(wchar_t *Format, wchar_t *Arg0)
{
	if (!DEMANGLER_DEBUGLOG_ON)
		return;

	do																							
	{																							
		System::Diagnostics::Debug::WriteLine(													
			System::String::Format(																
				gcnew System::String(Format),													
				gcnew System::String(Arg0)
			),													
			gcnew System::String(DEMANGLER_DEBUGLOG_CAT)
		);																						
	} while (false);																				
}

void DemanglerDebugTwoArg(wchar_t *Format, wchar_t *Arg0, wchar_t *Arg1)
{
	if (!DEMANGLER_DEBUGLOG_ON)
		return;

	do
	{
		System::Diagnostics::Debug::WriteLine(
			System::String::Format(
				gcnew System::String(Format),
				gcnew System::String(Arg0),
				gcnew System::String(Arg1)
			),
			gcnew System::String(DEMANGLER_DEBUGLOG_CAT)
		);
	} while (false);
}


bool DemumbleDemangleName(
	_In_ UnmanagedSymPrv* obj,
	_In_ wchar_t* DecoratedName,
	_In_ size_t DecoratedNameLen,
	_Out_ wchar_t** UndecoratedName,
	_Out_ size_t* UndecoratedNameLen
)
{
	size_t NameLen;
	int status;
	size_t MbstowcsStatus = 0;
	char *AsciiUndecoratedName = NULL;


	char *DecoratedNameAscii = (char*)malloc(DecoratedNameLen + 1);
	sprintf_s(DecoratedNameAscii, DecoratedNameLen + 1, "%ws", DecoratedName);

	if ((!UndecoratedName) || (!UndecoratedNameLen)) {
		return false;
	}

	*UndecoratedNameLen = 0;
	NameLen = DecoratedNameLen;

	AsciiUndecoratedName = __cxa_demangle(
		DecoratedNameAscii,
		NULL,
		&NameLen,
		&status
	);

	if (!status) {
		*UndecoratedName = (wchar_t*)malloc(NameLen * sizeof(wchar_t) + sizeof(wchar_t));

		mbstowcs_s(
			&MbstowcsStatus,
			*UndecoratedName,
			NameLen,
			AsciiUndecoratedName,
			NameLen
		);

		*UndecoratedNameLen = NameLen * sizeof(wchar_t);
	}

	free(DecoratedNameAscii);

	// UNIX-style error code
	return status == 0;
}

bool LLVMItaniumDemangleName(
	_In_ UnmanagedSymPrv* obj,
	_In_ wchar_t* DecoratedName,
	_In_ size_t DecoratedNameLen,
	_Out_ wchar_t** UndecoratedName,
	_Out_ size_t* UndecoratedNameLen
)
{
	size_t NameLen;
	int status;
	size_t MbstowcsStatus = 0;
	char *AsciiUndecoratedName = NULL;


	char *DecoratedNameAscii = (char*)malloc(DecoratedNameLen + 1);
	sprintf_s(DecoratedNameAscii, DecoratedNameLen + 1, "%ws", DecoratedName);

	if ((!UndecoratedName) || (!UndecoratedNameLen)) {
		return false;
	}

	*UndecoratedNameLen = 0;
	NameLen = DecoratedNameLen;

	AsciiUndecoratedName = llvm::itaniumDemangle(
		DecoratedNameAscii,
		nullptr,
		&NameLen,
		&status
	);

	if (!status) {
		*UndecoratedName = (wchar_t*)malloc(NameLen * sizeof(wchar_t) + sizeof(wchar_t));

		mbstowcs_s(
			&MbstowcsStatus,
			*UndecoratedName,
			NameLen,
			AsciiUndecoratedName,
			NameLen
		);

		*UndecoratedNameLen = NameLen * sizeof(wchar_t);
	}
	
	free(DecoratedNameAscii);

	// UNIX-style error code
	return status == 0;
}

bool LLVMMicrosoftDemangleName(
	_In_ UnmanagedSymPrv* obj,
	_In_ wchar_t* DecoratedName,
	_In_ size_t DecoratedNameLen,
	_Out_ wchar_t** UndecoratedName,
	_Out_ size_t* UndecoratedNameLen
)
{
	size_t NameLen;
	int status;
	size_t MbstowcsStatus = 0;
	char *AsciiUndecoratedName = NULL;


	char *DecoratedNameAscii = (char*)malloc(DecoratedNameLen + 1);
	sprintf_s(DecoratedNameAscii, DecoratedNameLen + 1, "%ws", DecoratedName);

	if ((!UndecoratedName) || (!UndecoratedNameLen)) {
		return false;
	}

	*UndecoratedNameLen = 0;
	NameLen = DecoratedNameLen;

	AsciiUndecoratedName = llvm::microsoftDemangle(
		DecoratedNameAscii,
		nullptr,
		&NameLen,
		&status
	);

	if (!status) {
		*UndecoratedName = (wchar_t*)malloc(NameLen * sizeof(wchar_t) + sizeof(wchar_t));

		mbstowcs_s(
			&MbstowcsStatus,
			*UndecoratedName,
			NameLen, 
			AsciiUndecoratedName,
			NameLen 
		);

		*UndecoratedNameLen = NameLen * sizeof(wchar_t);
	}

	free(DecoratedNameAscii);

	// UNIX-style error code
	return status == 0;
}

bool UndecorateSymbolDemangleName(
	_In_ UnmanagedSymPrv* obj,
	_In_ wchar_t* DecoratedName,
	_In_ size_t DecoratedNameLen,
	_Out_ wchar_t** UndecoratedName,
	_Out_ size_t* UndecoratedNameLen
)
{
	PPH_STRING PhUndecoratedName = NULL;
	wchar_t* Undname = NULL;

	if ((!UndecoratedName) || (!UndecoratedNameLen)) {
		return false;
	}

	PhUndecoratedName = PhUndecorateNameW(
		obj->m_SymbolProvider,
		DecoratedName
	);

	if (!PhUndecoratedName)
	{
		return false;
	}

	if (!wcsncmp(PhUndecoratedName->Buffer, DecoratedName, PhUndecoratedName->Length))
	{
		PhDereferenceObject(PhUndecoratedName);
		return false;
	}

	
	Undname = (wchar_t*)calloc(PhUndecoratedName->Length + sizeof(wchar_t), 1);
	if (!Undname)
	{
		PhDereferenceObject(PhUndecoratedName);
		return false;
	}

	memcpy(Undname, PhUndecoratedName->Buffer, PhUndecoratedName->Length);

	*UndecoratedNameLen = PhUndecoratedName->Length;
	*UndecoratedName = Undname;

	PhDereferenceObject(PhUndecoratedName);
	return true;
}

bool UnmanagedSymPrv::DemangleName(
	_In_ wchar_t* DecoratedName,
	_In_ size_t DecoratedNameLen,
	_Out_ wchar_t** UndecoratedName,
	_Out_ size_t* UndecoratedNameLen,
	_Out_ Dependencies::ClrPh::CLRPH_DEMANGLER *Demangler
)
{

	// try to undecorate GCC/LLVM symbols using demumble
	if (DemumbleDemangleName(
		this,
		DecoratedName,
		DecoratedNameLen,
		UndecoratedName,
		UndecoratedNameLen
	)) {
		*Demangler = Dependencies::ClrPh::CLRPH_DEMANGLER::Demumble;
		DEMANGLER_DEBUGLOG_TWO(L"Demumble {0:s} -> {1:s}", DecoratedName, *UndecoratedName);
		return true;
	}

	// try llvm-demangler. the heuristic is copied from .\llvm-7.0.0.src\lib\DebugInfo\Symbolize\Symbolize.cpp: LLVMSymbolizer::DemangleName
	if (!_wcsnicmp(DecoratedName, L"_Z", 2))
	{ 
		if (LLVMItaniumDemangleName(
			this,
			DecoratedName,
			DecoratedNameLen,
			UndecoratedName,
			UndecoratedNameLen
		)) {
			*Demangler = Dependencies::ClrPh::CLRPH_DEMANGLER::LLVMItanium;
			DEMANGLER_DEBUGLOG_TWO(L"LLVM Itanium {0:s} -> {1:s}", DecoratedName, *UndecoratedName);
			return true;
		}
	}

	// try to undecorate MSVC symbols using UndecorateName  
	if (UndecorateSymbolDemangleName(
		this,
		DecoratedName,
		DecoratedNameLen,
		UndecoratedName,
		UndecoratedNameLen
	)) {
		*Demangler = Dependencies::ClrPh::CLRPH_DEMANGLER::Microsoft;
		DEMANGLER_DEBUGLOG_TWO(L"Microsoft {0:s} -> {1:s}", DecoratedName, *UndecoratedName);
		return true;
	}

	// use llvm::microsoftDemangle as a last chance
	if (LLVMMicrosoftDemangleName(
		this,
		DecoratedName,
		DecoratedNameLen,
		UndecoratedName,
		UndecoratedNameLen
	)) {
		*Demangler = Dependencies::ClrPh::CLRPH_DEMANGLER::LLVMMicrosoft;
		DEMANGLER_DEBUGLOG_TWO(L"LLVM Microsoft {0:s} -> {1:s}", DecoratedName, *UndecoratedName);
		return true;
	}
	

	// Could not demangle name
	*UndecoratedNameLen =  0;
	*UndecoratedName = NULL;
	*Demangler = Dependencies::ClrPh::CLRPH_DEMANGLER::None;
	DEMANGLER_DEBUGLOG_ONE(L"Could not demangle \"{0:s}\" properly", DecoratedName);
	return false;
}

