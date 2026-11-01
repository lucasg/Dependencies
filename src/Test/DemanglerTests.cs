using NUnit.Framework;

namespace Dependencies.Test;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class DemanglerTests
{
    private static MicrosoftDemangler demangler;

    [OneTimeSetUp]
    public static void CreateSymbolProvider()
    {
        // Creating a provider initializes global DbgHelp state. The native library
        // synchronizes subsequent demangling calls, so the tests can share it.
        demangler = new MicrosoftDemangler();
    }

    [OneTimeTearDown]
    public static void DisposeSymbolProvider()
    {
        demangler.Dispose();
    }

    [TestCase("??1type_info@@UEAA@XZ", "public: virtual __cdecl type_info::~type_info(void) __ptr64")]
    [TestCase("?setbuf@strstreambuf@@UEAAPEAVstreambuf@@PEADH@Z", "public: virtual class streambuf * __ptr64 __cdecl strstreambuf::setbuf(char * __ptr64,int) __ptr64")]
    [TestCase("?CreateXBaby@XProvider@DirectUI@@UEAAJPEAVIXElementCP@2@PEAUHWND__@@PEAVElement@2@PEAKPEAPEAUIXBaby@2@@Z", "public: virtual long __cdecl DirectUI::XProvider::CreateXBaby(class DirectUI::IXElementCP * __ptr64,struct HWND__ * __ptr64,class DirectUI::Element * __ptr64,unsigned long * __ptr64,struct DirectUI::IXBaby * __ptr64 * __ptr64) __ptr64")]
    [TestCase("??0exception@@QEAA@AEBQEBDH@Z", "public: __cdecl exception::exception(char const * __ptr64 const & __ptr64,int) __ptr64")]
    [TestCase("?what@exception@@UEBAPEBDXZ", "public: virtual char const * __ptr64 __cdecl exception::what(void)const __ptr64")]
    [TestCase("?_Execute_once@std@@YAHAEAUonce_flag@1@P6AHPEAX1PEAPEAX@Z1@Z", "int __cdecl std::_Execute_once(struct std::once_flag & __ptr64,int (__cdecl*)(void * __ptr64,void * __ptr64,void * __ptr64 * __ptr64),void * __ptr64)")]
    [TestCase("?swap@?$basic_streambuf@_WU?$char_traits@_W@std@@@std@@IEAAXAEAV12@@Z", "protected: void __cdecl std::basic_streambuf<wchar_t,struct std::char_traits<wchar_t> >::swap(class std::basic_streambuf<wchar_t,struct std::char_traits<wchar_t> > & __ptr64) __ptr64")]
    public void DemanglesKnownMicrosoftSymbols(string decoratedName, string expectedName)
    {
        Assert.That(demangler.Demangle(decoratedName), Is.EqualTo(expectedName));
    }

    private sealed class MicrosoftDemangler : PhSymbolProvider
    {
        public string Demangle(string decoratedName)
        {
            return UndecorateNamePh(decoratedName);
        }
    }
}
