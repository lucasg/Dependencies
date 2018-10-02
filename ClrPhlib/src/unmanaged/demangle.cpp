#include <UnmanagedSymPrv.h>
#include <llvm/Demangle/Demangle.h>
#include <stdlib.h>

extern "C" {
	char* __cxa_demangle(const char* mangled_name,
		char* buf,
		size_t* n,
		int* status);
}

bool UnmanagedSymPrv::DemangleName(
	wchar_t* DecoratedName,
	size_t DecoratedNameLen,
	wchar_t** UndecoratedName,
	size_t* UndecoratedNameLen
)
{
	int status;
	PPH_STRING PhUndecoratedName = NULL;

	// try to undecorate GCC/LLVM symbols using demumble
	char *DecoratedNameAscii = (char*)malloc(DecoratedNameLen * sizeof(wchar_t));
	sprintf_s(DecoratedNameAscii, DecoratedNameLen * sizeof(wchar_t), "%ws", DecoratedName);

	char *UndecoratedNameAscii = __cxa_demangle(
		DecoratedNameAscii,
		NULL,
		NULL,
		&status
	);

	if (!status)
	{
		*UndecoratedNameLen = strlen(UndecoratedNameAscii) * sizeof(wchar_t);
		*UndecoratedName = (wchar_t*) malloc(*UndecoratedNameLen + sizeof(wchar_t));

		swprintf_s(*UndecoratedName, *UndecoratedNameLen, L"%hs", UndecoratedNameAscii);
		free(DecoratedNameAscii);
		return true;	
	}

	// try llvm-demangler. the heuristic is copied from .\llvm-7.0.0.src\lib\DebugInfo\Symbolize\Symbolize.cpp: LLVMSymbolizer::DemangleName
	if (!strncmp(DecoratedNameAscii, "_Z", 2))
	{ 
		int LLVMDemanglerStatus = 0;
		char *LLVMItaniumDemangled = llvm::itaniumDemangle(DecoratedNameAscii, nullptr, nullptr, &LLVMDemanglerStatus);
		if (!LLVMDemanglerStatus)
		{
			size_t MbstowcsStatus = 0;

			*UndecoratedNameLen = strlen(LLVMItaniumDemangled) * sizeof(wchar_t);
			*UndecoratedName = (wchar_t*)malloc(*UndecoratedNameLen + sizeof(wchar_t));

			mbstowcs_s(
				&MbstowcsStatus,
				*UndecoratedName, 
				*UndecoratedNameLen, 
				LLVMItaniumDemangled,
				*UndecoratedNameLen * sizeof(wchar_t)
			);

			free(DecoratedNameAscii);
			return true;
		}
	}

	// TODO : use llvm::microsoftDemangle if necessary
	/*if (!strncmp(DecoratedNameAscii, "?", 1))
	{
		int LLVMDemanglerStatus = 0;
		char *LLVMMicrosoftDemangled = llvm::microsoftDemangle(DecoratedNameAscii, nullptr, nullptr, &LLVMDemanglerStatus);
		if (!LLVMDemanglerStatus)
		{
			size_t MbstowcsStatus = 0;

			*UndecoratedNameLen = strlen(LLVMMicrosoftDemangled) + 1;
			*UndecoratedName = (wchar_t*)malloc(*UndecoratedNameLen * sizeof(wchar_t));

			mbstowcs_s(
				&MbstowcsStatus,
				*UndecoratedName,
				*UndecoratedNameLen,
				LLVMMicrosoftDemangled,
				*UndecoratedNameLen * sizeof(wchar_t)
			);

			free(DecoratedNameAscii);
			return true;
		}
	}*/


	free(DecoratedNameAscii);

	// try to undecorate MSVC symbols using UndecorateName  
	PhUndecoratedName = PhUndecorateNameW(
		this->m_SymbolProvider,
		DecoratedName
	);

	if (PhUndecoratedName) {
		*UndecoratedNameLen = PhUndecoratedName->Length;
		*UndecoratedName = (wchar_t*) malloc(PhUndecoratedName->Length + sizeof(wchar_t));

		memset(*UndecoratedName, 0, PhUndecoratedName->Length + sizeof(wchar_t));
		memcpy(*UndecoratedName, PhUndecoratedName->Buffer, PhUndecoratedName->Length);
		
		PhDereferenceObject(UndecoratedName);
		return true;
	}


	// Could not demangle name
	*UndecoratedNameLen =  0;
	*UndecoratedName = NULL;
	return false;
}



