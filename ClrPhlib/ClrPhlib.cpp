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


	PE::PE()
	:m_Impl(new UnmanagedPE)
	{

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

};