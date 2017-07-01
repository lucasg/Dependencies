// ClrPhlib.h

#pragma once

#include <UnmanagedPh.h>


#using <System.dll>  

namespace System {

    namespace ClrPh {

        public ref class Phlib {
        public:
            static bool InitializePhLib();
        };



        public ref struct PeImport {
            Int16 Hint;
            Int16 Ordinal;
            String ^ Name;
            String ^ ModuleName;
            Boolean ImportByOrdinal;
            Boolean	DelayImport;

            PeImport(const PPH_MAPPED_IMAGE_IMPORT_DLL importDll, size_t Index);
            PeImport(const PeImport ^ other);
            ~PeImport();

        };

        public ref struct PeImportDll {
        public:
            Int64 Flags;
            String ^Name;
            Int64 NumberOfEntries;

            Collections::Generic::List<PeImport^>^ ImportList;

            PeImportDll(const PPH_MAPPED_IMAGE_IMPORTS &PvMappedImports, size_t ImportDllIndex);
            PeImportDll(const PeImportDll ^ other);
            ~PeImportDll();
        protected:
            !PeImportDll();

        private:
            PPH_MAPPED_IMAGE_IMPORT_DLL ImportDll;
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

        public ref struct PeProperties {
            Int16 Machine;
            DateTime ^ Time;
            Int16 Magic;

            IntPtr ImageBase;
            Int32  SizeOfImage;
            IntPtr EntryPoint;


            Int32 Checksum;
            Boolean CorrectChecksum;

            Int16 Subsystem;
            Int16 Characteristics;
            Int16 DllCharacteristics;
        };



        public ref class PE
        {
        public:
            PE(_In_ String^ Filepath);

            ~PE();

            Collections::Generic::List<PeExport ^>^ GetExports();
            Collections::Generic::List<PeImportDll ^>^ GetImports();

            PeProperties ^Properties;
            Boolean LoadSuccessful;
            String^ Filepath;

        protected:
            // Deallocate the native object on the finalizer just in case no destructor is called  
            !PE() {
                delete m_Impl;
            }

            void InitProperties();

        private:
            UnmanagedPE * m_Impl;
        };

        public ref class PhSymbolProvider
        {
        public:
            PhSymbolProvider();
            ~PhSymbolProvider();
            !PhSymbolProvider();

            String^ UndecorateName(_In_ String ^DecoratedName);

        private:
            UnmanagedSymPrv *m_Impl;

        };
    }

}