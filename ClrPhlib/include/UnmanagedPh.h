#pragma once

#include <ph.h>
#include <mapimg.h>


// C++ part of the PE class interfacing with phlib.
// Responsible for mapping/unmapping the PE file in memory and
// parse the NT headers and Directory entries.
class UnmanagedPE {

public:

    UnmanagedPE();
    ~UnmanagedPE();
    
    // Try to load the PE pointed by Filepath in memmory
    bool LoadPE(LPWSTR Filepath);

    // Unload the memory mapped PE in order to release the FS lock
    void UnloadPE();
    
    // Extract the manifest embedded within the mapped PE
    //
    // /param  manifest variable pointing to the part of the mapped file holding the manifest (no allocations here)
    // /param  manifestLen variable returning the length of the embedded binary manifest
    // /return false if there is none
    bool GetPeManifest(
		_Out_ BYTE* *manifest,
        _Out_ INT  *manifestLen
    );
    

    PH_MAPPED_IMAGE m_PvMappedImage;
    PH_MAPPED_IMAGE_EXPORTS m_PvExports;
    PH_MAPPED_IMAGE_IMPORTS m_PvImports;
    PH_MAPPED_IMAGE_IMPORTS m_PvDelayImports;

    union {
        PIMAGE_LOAD_CONFIG_DIRECTORY32 m_PvConfig32;
        PIMAGE_LOAD_CONFIG_DIRECTORY64 m_PvCconfig64;
    };

private:
    bool            m_bImageLoaded;
};
