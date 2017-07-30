#pragma once

#include <ph.h>
#include <mapimg.h>
#include <symprv.h>


class UnmanagedPE {

public:

	UnmanagedPE();
	~UnmanagedPE();
    
    
	bool LoadPE(LPWSTR Filepath);
	
	bool
	GetPeManifest(
			_Out_ PSTR *manifest,
			_Out_ INT  *manfestLen
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


class UnmanagedSymPrv {
public :

	static UnmanagedSymPrv* Create();

	PPH_SYMBOL_PROVIDER m_SymbolProvider;
};