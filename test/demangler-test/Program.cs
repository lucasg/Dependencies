using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ClrPh;
using System.Diagnostics;

namespace Dependencies
{
    namespace Test
    { 
        class Program
        {
            static bool TestKnownInputs()
            {
                PhSymbolProvider SymPrv = new PhSymbolProvider();

                Debug.Assert(SymPrv.UndecorateName("??1type_info@@UEAA@XZ") == "public: virtual __cdecl type_info::~type_info(void) __ptr64");
                Debug.Assert(SymPrv.UndecorateName("?setbuf@strstreambuf@@UEAAPEAVstreambuf@@PEADH@Z") == "public: virtual class streambuf * __ptr64 __cdecl strstreambuf::setbuf(char * __ptr64,int) __ptr64");
                Debug.Assert(SymPrv.UndecorateName("?CreateXBaby@XProvider@DirectUI@@UEAAJPEAVIXElementCP@2@PEAUHWND__@@PEAVElement@2@PEAKPEAPEAUIXBaby@2@@Z") == "public: virtual long __cdecl DirectUI::XProvider::CreateXBaby(class DirectUI::IXElementCP * __ptr64,struct HWND__ * __ptr64,class DirectUI::Element * __ptr64,unsigned long * __ptr64,struct DirectUI::IXBaby * __ptr64 * __ptr64) __ptr64");
                Debug.Assert(SymPrv.UndecorateName("??0exception@@QEAA@AEBQEBDH@Z") == "public: __cdecl exception::exception(char const * __ptr64 const & __ptr64,int) __ptr64");
                Debug.Assert(SymPrv.UndecorateName("?what@exception@@UEBAPEBDXZ") == "public: virtual char const * __ptr64 __cdecl exception::what(void)const __ptr64");
                Debug.Assert(SymPrv.UndecorateName("?_Execute_once@std@@YAHAEAUonce_flag@1@P6AHPEAX1PEAPEAX@Z1@Z") == "int __cdecl std::_Execute_once(struct std::once_flag & __ptr64,int (__cdecl*)(void * __ptr64,void * __ptr64,void * __ptr64 * __ptr64),void * __ptr64)");
                Debug.Assert(SymPrv.UndecorateName("?swap@?$basic_streambuf@_WU?$char_traits@_W@std@@@std@@IEAAXAEAV12@@Z") == "protected: void __cdecl std::basic_streambuf<wchar_t,struct std::char_traits<wchar_t> >::swap(class std::basic_streambuf<wchar_t,struct std::char_traits<wchar_t> > & __ptr64) __ptr64");

                Console.WriteLine("demangler-test : all known inputs OK");
                return true;
            }

            static bool TestFilepath(string Filepath)
            {
                PhSymbolProvider SymPrv = new PhSymbolProvider();

                PE Pe = new PE(Filepath);
                if (!Pe.Load())
                {
                    Console.Error.WriteLine("[x] Could not load file {0:s} as a PE", Filepath);
                    return false;
                }

                foreach (PeExport Export in Pe.GetExports())
                {
                    if (Export.Name.Length > 0)
                        Console.WriteLine("\t Export : {0:s} -> {1:s}", Export.Name, SymPrv.UndecorateName(Export.Name));
                }

                foreach (PeImportDll DllImport in Pe.GetImports())
                {
                    foreach (PeImport Import in DllImport.ImportList)
                    {
                        if (!Import.ImportByOrdinal)
                        {
                            Console.WriteLine("\t Import from {0:s} : {1:s} -> {2:s}", DllImport.Name, Import.Name, SymPrv.UndecorateName(Import.Name));
                        }
                    }
                }

                return true;
            }

            static void Main(string[] args)
            {
                // always the first call to make
                Phlib.InitializePhLib();

                if (args.Length == 0)
                {
                    TestKnownInputs();
                    return;
                }

                TestFilepath(args[0]);
            }
        }
    }
}
