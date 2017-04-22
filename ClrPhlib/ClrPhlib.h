// ClrPhlib.h

#pragma once

#include <ph.h>
#include <mapimg.h>

#using <System.dll>  

namespace System
{ 

class UnmanagedPE {
public:
    UnmanagedPE();
    
    bool LoadPE(LPWSTR Filepath);

    ~UnmanagedPE();


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

namespace ClrPh {
	
	public ref class Phlib {
	public:
		static bool InitializePhLib();
	};

	public ref struct PeImport {
		Int16 Hint;
		Int16 Ordinal;
		String ^  Name; // may be NULL.
		String ^ ModuleName;
		Boolean ImportByOrdinal;
		Boolean	DelayImport;

		PeImport(const PH_MAPPED_IMAGE_IMPORT_DLL &importDll, size_t Index, Boolean DelayImport);
		PeImport(const PeImport ^ other);
		~PeImport();

	};

	public ref struct PeExport {
		Int16 Ordinal;
		String ^  Name; // may be NULL.
		Boolean ExportByOrdinal;
		Int64	VirtualAddress;
		String ^  ForwardedName;

		PeExport(const UnmanagedPE &refPe, size_t Index);
		PeExport(const PeExport ^ other);
		~PeExport();

	};

	public ref class PE
	{
	public:
        PE(_In_ String^ Filepath);
        
        ~PE();

		Collections::Generic::List<PeExport ^>^ GetExports();
		Collections::Generic::List<PeImport ^>^ GetImports();

    protected:
        // Deallocate the native object on the finalizer just in case no destructor is called  
        !PE() {
            delete m_Impl;
        }

    private:
        UnmanagedPE * m_Impl;
	};
}

}