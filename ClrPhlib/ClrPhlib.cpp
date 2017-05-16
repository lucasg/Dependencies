// Il s'agit du fichier DLL principal.

#include "stdafx.h"
#include "ClrPhlib.h"
#include <vcclr.h> 
#include <atlstr.h>

VOID PvpLoadDbgHelpFromPath(
	_In_ PWSTR DbgHelpPath
)
{
	HMODULE dbghelpModule;

	if (dbghelpModule = LoadLibrary(DbgHelpPath))
	{
		PPH_STRING fullDbghelpPath;
		ULONG indexOfFileName;
		PH_STRINGREF dbghelpFolder;
		PPH_STRING symsrvPath;

		fullDbghelpPath = PhGetDllFileName(dbghelpModule, &indexOfFileName);

		if (fullDbghelpPath)
		{
			if (indexOfFileName != 0)
			{
				static PH_STRINGREF symsrvString = PH_STRINGREF_INIT(L"\\symsrv.dll");

				dbghelpFolder.Buffer = fullDbghelpPath->Buffer;
				dbghelpFolder.Length = indexOfFileName * sizeof(WCHAR);

				symsrvPath = PhConcatStringRef2(&dbghelpFolder, &symsrvString);

				LoadLibrary(symsrvPath->Buffer);

				PhDereferenceObject(symsrvPath);
			}

			PhDereferenceObject(fullDbghelpPath);
		}
	}
	else
	{
		dbghelpModule = LoadLibrary(L"dbghelp.dll");
	}

	PhSymbolProviderCompleteInitialization(dbghelpModule);
}

BOOLEAN PvpLoadDbgHelp(
	_Inout_ PPH_SYMBOL_PROVIDER *SymbolProvider
)
{
	static UNICODE_STRING symbolPathVarName = RTL_CONSTANT_STRING(L"_NT_SYMBOL_PATH");
	PPH_STRING symbolSearchPath;
	PPH_SYMBOL_PROVIDER symbolProvider;
	WCHAR buffer[512] = L"";
	UNICODE_STRING symbolPathUs;

	
	symbolPathUs.Buffer = buffer;
	symbolPathUs.Length = sizeof(buffer) - sizeof(UNICODE_NULL);
	symbolPathUs.MaximumLength = sizeof(buffer);
	

	if (!PhSymbolProviderInitialization())
		return FALSE;

	PvpLoadDbgHelpFromPath(L"C:\\Program Files (x86)\\Windows Kits\\10\\Debuggers\\x64\\dbghelp.dll");
	symbolProvider = PhCreateSymbolProvider(NULL);

	// Load symbol path from _NT_SYMBOL_PATH if configured by the user.    
	if (NT_SUCCESS(RtlQueryEnvironmentVariable_U(NULL, &symbolPathVarName, &symbolPathUs)))
	{
		symbolSearchPath = PhFormatString(L"SRV*%s*http://msdl.microsoft.com/download/symbols", symbolPathUs.Buffer);
	}
	else
	{
		symbolSearchPath = PhCreateString(L"SRV**http://msdl.microsoft.com/download/symbols");
	}

	PhSetSearchPathSymbolProvider(symbolProvider, symbolSearchPath->Buffer);
	PhDereferenceObject(symbolSearchPath);

	*SymbolProvider = symbolProvider;
	return TRUE;
}
namespace System
{

	UnmanagedSymPrv* UnmanagedSymPrv::Create()
	{
		UnmanagedSymPrv *Instance = new UnmanagedSymPrv;
		
		if (!PvpLoadDbgHelp(&Instance->m_SymbolProvider)) {
			delete Instance;
			return NULL;
		}

		return Instance;
	}


    using namespace ClrPh;

	static bool bInitializedPhLib = false;

	bool Phlib::InitializePhLib()
	{
		if (!bInitializedPhLib)
		{
			bInitializedPhLib = NT_SUCCESS(PhInitializePhLibEx(0, 0, 0));
		}

		return bInitializedPhLib;
	}

    UnmanagedPE::UnmanagedPE()
        :m_bImageLoaded(false)
    {
        memset(&m_PvMappedImage, 0, sizeof(PH_MAPPED_IMAGE));
    }

    bool UnmanagedPE::LoadPE(LPWSTR Filepath)
    {
        if (m_bImageLoaded)
        {
            PhUnloadMappedImage(&m_PvMappedImage);
        }

        memset(&m_PvMappedImage, 0, sizeof(PH_MAPPED_IMAGE));

        m_bImageLoaded = NT_SUCCESS(PhLoadMappedImage(
            Filepath,
            NULL,
            TRUE,
            &m_PvMappedImage
        ));

        return m_bImageLoaded;
    }

    UnmanagedPE::~UnmanagedPE()
    {
		if (m_bImageLoaded)
		{
			PhUnloadMappedImage(&m_PvMappedImage);
		}
    }


    PE::PE(
        _In_ String ^ Filepath
    )
    {
        CString PvFilePath(Filepath);
        m_Impl = new UnmanagedPE();

		LoadSuccessful = m_Impl->LoadPE(PvFilePath.GetBuffer());

		if (LoadSuccessful)
			InitProperties();
		 
    }

	void PE::InitProperties()
	{
		LARGE_INTEGER time;
		SYSTEMTIME systemTime;

		PH_MAPPED_IMAGE PvMappedImage = m_Impl->m_PvMappedImage;
		
		Properties = gcnew PeProperties();
		Properties->Machine = PvMappedImage.NtHeaders->FileHeader.Machine;
		Properties->Magic = m_Impl->m_PvMappedImage.Magic;
		Properties->Checksum = PvMappedImage.NtHeaders->OptionalHeader.CheckSum;
		Properties->CorrectChecksum = (Properties->Checksum == PhCheckSumMappedImage(&PvMappedImage));

		RtlSecondsSince1970ToTime(PvMappedImage.NtHeaders->FileHeader.TimeDateStamp, &time);
		PhLargeIntegerToLocalSystemTime(&systemTime, &time);
		Properties->Time = gcnew DateTime (systemTime.wYear, systemTime.wMonth, systemTime.wDay, systemTime.wHour, systemTime.wMinute, systemTime.wSecond, systemTime.wMilliseconds, DateTimeKind::Local);

		if (PvMappedImage.Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
		{
			PIMAGE_OPTIONAL_HEADER32 OptionalHeader = (PIMAGE_OPTIONAL_HEADER32) &PvMappedImage.NtHeaders->OptionalHeader;
			
			Properties->ImageBase = (IntPtr) (Int32) OptionalHeader->ImageBase;
			Properties->SizeOfImage = OptionalHeader->SizeOfImage;
			Properties->EntryPoint = (IntPtr) (Int32) OptionalHeader->AddressOfEntryPoint;
		}
		else
		{
			PIMAGE_OPTIONAL_HEADER64 OptionalHeader = (PIMAGE_OPTIONAL_HEADER64)&PvMappedImage.NtHeaders->OptionalHeader;

			Properties->ImageBase = (IntPtr)(Int64)OptionalHeader->ImageBase;
			Properties->SizeOfImage = OptionalHeader->SizeOfImage;
			Properties->EntryPoint = (IntPtr)(Int64)OptionalHeader->AddressOfEntryPoint;

		}

		Properties->Subsystem = PvMappedImage.NtHeaders->OptionalHeader.Subsystem;
		Properties->Characteristics = PvMappedImage.NtHeaders->FileHeader.Characteristics;
		Properties->DllCharacteristics = PvMappedImage.NtHeaders->OptionalHeader.DllCharacteristics;
	}

    PE::~PE()
    {
        delete m_Impl;
    }

	Collections::Generic::List<PeExport^> ^ PE::GetExports()
	{
		Collections::Generic::List<PeExport^> ^Exports = gcnew Collections::Generic::List<PeExport^>();

		if (NT_SUCCESS(PhGetMappedImageExports(&m_Impl->m_PvExports, &m_Impl->m_PvMappedImage)))
		{
			for (size_t Index = 0; Index < m_Impl->m_PvExports.NumberOfEntries; Index++)
			{
				Exports->Add(gcnew PeExport(*m_Impl, Index));
			}
		}

		return Exports;
	}

	PeExport::PeExport(const UnmanagedPE &refPe, size_t Index)
	{
		PH_MAPPED_IMAGE_EXPORT_ENTRY exportEntry;
		PH_MAPPED_IMAGE_EXPORT_FUNCTION exportFunction;

		if (
			NT_SUCCESS(PhGetMappedImageExportEntry((PPH_MAPPED_IMAGE_EXPORTS)&refPe.m_PvExports, (ULONG) Index, &exportEntry)) &&
			NT_SUCCESS(PhGetMappedImageExportFunction((PPH_MAPPED_IMAGE_EXPORTS)&refPe.m_PvExports, NULL, exportEntry.Ordinal, &exportFunction))
			)
		{
			Ordinal = exportEntry.Ordinal;
			ExportByOrdinal = (exportEntry.Name == nullptr);
			Name = gcnew String(exportEntry.Name);
			ForwardedName = gcnew String(exportFunction.ForwardedName);
			
			if (exportEntry.Name == nullptr)
				VirtualAddress = (Int64)exportFunction.Function;

			VirtualAddress = (Int64) exportFunction.Function;
		}

		
	}

	PeExport::PeExport(const PeExport ^ other)
	{
		this->Ordinal = Ordinal;
		this->ExportByOrdinal = ExportByOrdinal;
		this->Name = String::Copy(other->Name);
		this->ForwardedName = String::Copy(other->ForwardedName);
		this->VirtualAddress = other->VirtualAddress;
	}

	PeExport::~PeExport()
	{

	}

	Collections::Generic::List<PeImportDll^> ^ PE::GetImports()
	{
		Collections::Generic::List<PeImportDll^> ^Imports = gcnew Collections::Generic::List<PeImportDll^>();

		// Standard Imports
		if (NT_SUCCESS(PhGetMappedImageImports(&m_Impl->m_PvImports, &m_Impl->m_PvMappedImage)))
		{
			for (size_t IndexDll = 0; IndexDll< m_Impl->m_PvImports.NumberOfDlls; IndexDll++)
			{
				Imports->Add(gcnew PeImportDll(&m_Impl->m_PvImports, IndexDll));
			}
		}

		// Delayed Imports
		if (NT_SUCCESS(PhGetMappedImageDelayImports(&m_Impl->m_PvDelayImports, &m_Impl->m_PvMappedImage)))
		{
			for (size_t IndexDll = 0; IndexDll< m_Impl->m_PvImports.NumberOfDlls; IndexDll++)
			{
				Imports->Add(gcnew PeImportDll(&m_Impl->m_PvDelayImports, IndexDll));
			}
		}

		return Imports;
	}

	PeImport::PeImport(const PPH_MAPPED_IMAGE_IMPORT_DLL importDll, size_t Index)
	{
		PH_MAPPED_IMAGE_IMPORT_ENTRY importEntry;

		if (NT_SUCCESS(PhGetMappedImageImportEntry((PPH_MAPPED_IMAGE_IMPORT_DLL) importDll, (ULONG)Index, &importEntry)))
		{
			this->Hint = importEntry.NameHint;
			this->Ordinal = importEntry.Ordinal;
			this->DelayImport = (importDll->Flags) & PH_MAPPED_IMAGE_DELAY_IMPORTS;
			this->Name = gcnew String(importEntry.Name);
			this->ModuleName = gcnew String(importDll->Name);
			this->ImportByOrdinal = (importEntry.Name == nullptr);
		}


	}

	PeImport::PeImport(const PeImport ^ other)
	{
		this->Hint = other->Hint;
		this->Ordinal = other->Ordinal;
		this->DelayImport = other->DelayImport;
		this->Name = String::Copy(other->Name);
		this->ModuleName = String::Copy(other->ModuleName);
		this->ImportByOrdinal = other->ImportByOrdinal;
	}

	PeImport::~PeImport()
	{
	}


	PeImportDll::PeImportDll(const PPH_MAPPED_IMAGE_IMPORTS &PvMappedImports, size_t ImportDllIndex)
	: ImportDll (new PH_MAPPED_IMAGE_IMPORT_DLL)
	{
		ImportList = gcnew Collections::Generic::List<PeImport^>();

		if (!NT_SUCCESS(PhGetMappedImageImportDll(PvMappedImports, (ULONG)ImportDllIndex, ImportDll)))
		{
			return;
		}

		Flags = ImportDll->Flags;
		Name = gcnew String(ImportDll->Name);
		NumberOfEntries = ImportDll->NumberOfEntries;

		for (size_t IndexImport = 0; IndexImport < (size_t) NumberOfEntries; IndexImport++)
		{
			ImportList->Add(gcnew PeImport(ImportDll, IndexImport));
		}
	}

	PeImportDll::~PeImportDll()
	{
		delete ImportDll;
	}

	PeImportDll::!PeImportDll()
	{
		delete ImportDll;
	}

	PeImportDll::PeImportDll(const PeImportDll ^ other)
	: ImportDll(new PH_MAPPED_IMAGE_IMPORT_DLL)
	{
		ImportList = gcnew Collections::Generic::List<PeImport^>();

		memcpy(ImportDll, other->ImportDll, sizeof(PH_MAPPED_IMAGE_IMPORT_DLL));

		Flags = other->Flags;
		Name = String::Copy(other->Name);
		NumberOfEntries = other->NumberOfEntries;

		for (size_t IndexImport = 0; IndexImport < (size_t)NumberOfEntries; IndexImport++)
		{
			ImportList->Add(gcnew PeImport(other->ImportList[IndexImport]));
		}

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

	String^ PhSymbolProvider::UndecorateName(_In_ String ^DecoratedName)
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

	

};
