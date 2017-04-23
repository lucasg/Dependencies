// Il s'agit du fichier DLL principal.

#include "stdafx.h"
#include "ClrPhlib.h"
#include <vcclr.h> 
#include <atlstr.h>


namespace System
{
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

        m_Impl->LoadPE(PvFilePath.GetBuffer());
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

};
