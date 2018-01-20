#include <ClrPhlib.h>
#include <UnmanagedPh.h>
#include <phnt_ntdef.h>

using namespace System;
using namespace ClrPh;


UnmanagedPE::UnmanagedPE()
        :m_bImageLoaded(false)
{
    memset(&m_PvMappedImage, 0, sizeof(PH_MAPPED_IMAGE));
}

UnmanagedPE::~UnmanagedPE()
{
    UnloadPE();
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

void UnmanagedPE::UnloadPE()
{
    if (m_bImageLoaded)
    {
        PhUnloadMappedImage(&m_PvMappedImage);
        m_bImageLoaded = false;
    }
}


bool UnmanagedPE::GetPeManifest(
	_Out_ BYTE* *manifest,
    _Out_ INT  *manifestLen 
)
{
    PH_MAPPED_IMAGE_RESOURCES resources;
    bool manifestFound = false;

    if (!m_bImageLoaded)
        return false;

    if (!NT_SUCCESS(PhGetMappedImageResources(&resources, &m_PvMappedImage)))
        return false;

    *manifest = NULL;
    *manifestLen = 0;


    for (ULONG i = 0; i < resources.NumberOfEntries; i++)
    {
        PH_IMAGE_RESOURCE_ENTRY entry;

        entry = resources.ResourceEntries[i];
        if (entry.Type == (ULONG_PTR) RT_MANIFEST)
        {  
            // Manifest entry is utf-8 only
            *manifest = (BYTE*)entry.Data;
            *manifestLen = entry.Size;

			manifestFound = true;
        }

        // stops on first manifest found
        if (manifestFound)
            break;
    }

    PhFree(resources.ResourceEntries);
    return manifestFound;
}