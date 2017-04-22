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
			if (ExportByOrdinal)
			{
				Name = gcnew String("");
			}
			else
			{
				Name = gcnew String(exportEntry.Name);
			}

			if (!exportFunction.ForwardedName)
			{
				ForwardedName = gcnew String("");
			}
			else
			{
				ForwardedName = gcnew String(exportFunction.ForwardedName);
			}

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

};
