#include <UnmanagedSymPrv.h>
#include <llvm/Demangle/Demangle.h>
#include <stdlib.h>

extern "C" {
	char* __cxa_demangle(const char* mangled_name,
		char* buf,
		size_t* n,
		int* status);
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


	char *DecoratedNameAscii = (char*)malloc(DecoratedNameLen * sizeof(wchar_t));
	sprintf_s(DecoratedNameAscii, DecoratedNameLen * sizeof(wchar_t), "%ws", DecoratedName);

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
			STRUNCATE
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


	char *DecoratedNameAscii = (char*)malloc(DecoratedNameLen * sizeof(wchar_t));
	sprintf_s(DecoratedNameAscii, DecoratedNameLen * sizeof(wchar_t), "%ws", DecoratedName);

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
			STRUNCATE
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


	char *DecoratedNameAscii = (char*)malloc(DecoratedNameLen * sizeof(wchar_t));
	sprintf_s(DecoratedNameAscii, DecoratedNameLen * sizeof(wchar_t), "%ws", DecoratedName);

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
			STRUNCATE
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

	if ((!UndecoratedName) || (!UndecoratedNameLen)) {
		return false;
	}

	PhUndecoratedName = PhUndecorateNameW(
		obj->m_SymbolProvider,
		DecoratedName
	);

	if (!PhUndecoratedName)
		return false;

	*UndecoratedNameLen = PhUndecoratedName->Length;
	*UndecoratedName = (wchar_t*)malloc(PhUndecoratedName->Length + sizeof(wchar_t));

	memset(*UndecoratedName, 0, PhUndecoratedName->Length + sizeof(wchar_t));
	memcpy(*UndecoratedName, PhUndecoratedName->Buffer, PhUndecoratedName->Length);

	PhDereferenceObject(UndecoratedName);
	return true;
}

bool UnmanagedSymPrv::DemangleName(
	_In_ wchar_t* DecoratedName,
	_In_ size_t DecoratedNameLen,
	_Out_ wchar_t** UndecoratedName,
	_Out_ size_t* UndecoratedNameLen
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
			return true;
		}
	}

	// TODO : use llvm::microsoftDemangle if necessary
	/*if (!strncmp(DecoratedNameAscii, "?", 1))
	{
		if (LLVMMicrosoftDemangleName(
			DecoratedNameAscii,
			DecoratedNameLen,
			&UndecoratedNameAscii,
			&UndecoratedNameAsciiLen
		))
		{
			size_t MbstowcsStatus = 0;

			*UndecoratedNameLen = strlen(UndecoratedNameAscii) + 1;
			*UndecoratedName = (wchar_t*)malloc(*UndecoratedNameLen * sizeof(wchar_t));

			mbstowcs_s(
				&MbstowcsStatus,
				*UndecoratedName,
				*UndecoratedNameLen,
				UndecoratedNameAscii,
				*UndecoratedNameLen * sizeof(wchar_t)
			);

			free(DecoratedNameAscii);
			return true;
		}
	}*/

	// try to undecorate MSVC symbols using UndecorateName  
	if (UndecorateSymbolDemangleName(
		this,
		DecoratedName,
		DecoratedNameLen,
		UndecoratedName,
		UndecoratedNameLen
	)) {
		return true;
	}


	// Could not demangle name
	*UndecoratedNameLen =  0;
	*UndecoratedName = NULL;
	return false;
}



