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
private:
    bool            m_bImageLoaded;
    PH_MAPPED_IMAGE m_PvMappedImage;

};

namespace ClrPh {
	
	public ref class Phlib {
	public:
		static bool InitializePhLib();
	};
	

	public ref class PE
	{
	public:
		PE();
        PE(_In_ String^ Filepath);
        
        ~PE();

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