using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

using NDesk.Options;
using Dependencies.ClrPh;

namespace Dependencies
{
    namespace Test
    {
        public class Demangler :  PhSymbolProvider
        {
            public Demangler(CLRPH_DEMANGLER demangler = CLRPH_DEMANGLER.Default)
            {
                _demangler = demangler;
            }

            public override Tuple<CLRPH_DEMANGLER, string> UndecorateName(string DecoratedName)
            {

                switch (_demangler)
                {
                    case CLRPH_DEMANGLER.Demumble:
                        return new Tuple<CLRPH_DEMANGLER, string>(CLRPH_DEMANGLER.Demumble, base.UndecorateNameDemumble(DecoratedName));
                    case CLRPH_DEMANGLER.LLVMItanium:
                        return new Tuple<CLRPH_DEMANGLER, string>(CLRPH_DEMANGLER.LLVMItanium, base.UndecorateNameLLVMItanium(DecoratedName));
                    case CLRPH_DEMANGLER.LLVMMicrosoft:
                        return new Tuple<CLRPH_DEMANGLER, string>(CLRPH_DEMANGLER.LLVMMicrosoft, base.UndecorateNameLLVMMicrosoft(DecoratedName));
                    case CLRPH_DEMANGLER.Microsoft:
                        return new Tuple<CLRPH_DEMANGLER, string>(CLRPH_DEMANGLER.Microsoft, base.UndecorateNamePh(DecoratedName));

                    default:
                    case CLRPH_DEMANGLER.Default:
                        return base.UndecorateName(DecoratedName);
                }

            }

            private CLRPH_DEMANGLER _demangler;
        }

        class Program
        {
            static void ShowHelp(OptionSet p)
            {
                Console.WriteLine("Usage: demangler [options] FILE_OR_SYMBOL");
                Console.WriteLine();
                Console.WriteLine("Options:");
                p.WriteOptionDescriptions(Console.Out);
            }

            static CLRPH_DEMANGLER ParseDemanglerName(string v)
            {
                switch (v)
                {
                    case "Default":
                        return CLRPH_DEMANGLER.Default;
                    case "Demumble":
                        return CLRPH_DEMANGLER.Demumble;
                    case "LLVMItanium":
                        return CLRPH_DEMANGLER.LLVMItanium;
                    case "LLVMMicrosoft":
                        return CLRPH_DEMANGLER.LLVMMicrosoft;
                    case "Microsoft":
                        return CLRPH_DEMANGLER.Microsoft;

                    default:
                        string msg = String.Format("Unknown demangler '{0}'. Only 'Default', 'Demumble', 'LLVMItanium', 'LLVMMicrosoft' and 'Microsoft' are supported.", v);
                        throw new OptionException(msg, "severity");
                }
            }

            static bool TestKnownInputs(Demangler SymPrv)
            {
                Debug.Assert(SymPrv.UndecorateName("??1type_info@@UEAA@XZ").Item2 == "public: virtual __cdecl type_info::~type_info(void) __ptr64");
                Debug.Assert(SymPrv.UndecorateName("?setbuf@strstreambuf@@UEAAPEAVstreambuf@@PEADH@Z").Item2 == "public: virtual class streambuf * __ptr64 __cdecl strstreambuf::setbuf(char * __ptr64,int) __ptr64");
                Debug.Assert(SymPrv.UndecorateName("?CreateXBaby@XProvider@DirectUI@@UEAAJPEAVIXElementCP@2@PEAUHWND__@@PEAVElement@2@PEAKPEAPEAUIXBaby@2@@Z").Item2 == "public: virtual long __cdecl DirectUI::XProvider::CreateXBaby(class DirectUI::IXElementCP * __ptr64,struct HWND__ * __ptr64,class DirectUI::Element * __ptr64,unsigned long * __ptr64,struct DirectUI::IXBaby * __ptr64 * __ptr64) __ptr64");
                Debug.Assert(SymPrv.UndecorateName("??0exception@@QEAA@AEBQEBDH@Z").Item2 == "public: __cdecl exception::exception(char const * __ptr64 const & __ptr64,int) __ptr64");
                Debug.Assert(SymPrv.UndecorateName("?what@exception@@UEBAPEBDXZ").Item2 == "public: virtual char const * __ptr64 __cdecl exception::what(void)const __ptr64");
                Debug.Assert(SymPrv.UndecorateName("?_Execute_once@std@@YAHAEAUonce_flag@1@P6AHPEAX1PEAPEAX@Z1@Z").Item2 == "int __cdecl std::_Execute_once(struct std::once_flag & __ptr64,int (__cdecl*)(void * __ptr64,void * __ptr64,void * __ptr64 * __ptr64),void * __ptr64)");
                Debug.Assert(SymPrv.UndecorateName("?swap@?$basic_streambuf@_WU?$char_traits@_W@std@@@std@@IEAAXAEAV12@@Z").Item2 == "protected: void __cdecl std::basic_streambuf<wchar_t,struct std::char_traits<wchar_t> >::swap(class std::basic_streambuf<wchar_t,struct std::char_traits<wchar_t> > & __ptr64) __ptr64");

                Console.WriteLine("demangler-test : all known inputs OK");
                return true;
            }

            static bool TestFilepath(string Filepath, Demangler SymPrv)
            {

                PE Pe = new PE(Filepath);
                if (!Pe.Load())
                {
                    Console.Error.WriteLine("[x] Could not load file {0:s} as a PE", Filepath);
                    return false;
                }

                foreach (PeExport Export in Pe.GetExports())
                {
                    if (Export.Name.Length > 0)
                    {
                        Console.Write("\t Export : {0:s} -> ", Export.Name);
                        Console.Out.Flush();
                        Console.WriteLine("{0:s}", SymPrv.UndecorateName(Export.Name));
                    }
                        
                }

                foreach (PeImportDll DllImport in Pe.GetImports())
                {
                    foreach (PeImport Import in DllImport.ImportList)
                    {
                        if (!Import.ImportByOrdinal)
                        {
                            Console.Write("\t Import from {0:s} : {1:s} -> ", DllImport.Name, Import.Name);
                            Console.Out.Flush();
                            Console.WriteLine("{0:s}", SymPrv.UndecorateName(Import.Name));
                        }

                    }
                }

                return true;
            }

            static void Main(string[] args)
            {
                bool is_verbose = false;
                bool show_help = false;
                CLRPH_DEMANGLER demangler_name = CLRPH_DEMANGLER.Default;

                OptionSet opts = new OptionSet() {
                            { "v|verbose", "redirect debug traces to console", v => is_verbose = v != null },
                            { "h|help",  "show this message and exit", v => show_help = v != null },
                            { "d=|demangler=",  "Choose demangler name", v => demangler_name = ParseDemanglerName(v) },
                        };

                List<string> eps = opts.Parse(args);

                if (show_help)
                {
                    ShowHelp(opts);
                    return;
                }

                if (is_verbose)
                {
                    // Redirect debug log to the console
                    Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
                    Debug.AutoFlush = true;
                }

                // always the first call to make
                Phlib.InitializePhLib();
                Demangler demangler;

                switch(args.Length)
                {
                    case 0:
                        demangler = new Demangler(CLRPH_DEMANGLER.Microsoft);
                        TestKnownInputs(demangler);
                        break;

                    default:
                    case 1:
                        demangler = new Demangler(demangler_name);
                        string Filepath = args[1];

                        if (NativeFile.Exists(Filepath))
                        {
                            TestFilepath(Filepath, demangler);
                        }
                        else
                        {
                            string undecoratedName = demangler.UndecorateName(args[1]).Item2;
                            Console.WriteLine(undecoratedName);
                        }

                        break;
                }

                // Force flushing out buffer
                Console.Out.Flush();
            }
        }
    }
}
