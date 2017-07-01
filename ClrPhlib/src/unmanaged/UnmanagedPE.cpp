#include <ClrPhlib.h>
#include <UnmanagedPh.h>

using namespace System;
using namespace ClrPh;


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