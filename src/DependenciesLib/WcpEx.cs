using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dependencies;

public static class WcpExConstants
{
    public static readonly IReadOnlyCollection<byte> DCMHeader = Array.AsReadOnly(new byte[] { 0x44, 0x43, 0x4d, 0x01 });
}
/// <summary>
/// Rewrite of https://github.com/smx-smx/wcpex, used to decompress manifest files in WinSxS which start with 'DCM'
/// </summary>
public abstract class WcpEx
{
    public static byte[] DecompressManifest(byte[] manifestData)
    {
        return Instance.DecompressManifestInner(manifestData);
    }

    static WcpEx Instance { get; }

    static WcpEx()
    {
        // C:\Windows\WinSxS\amd64_microsoft-windows-servicingstack_31bf3856ad364e35_10.0.19041.1220_none_7e21bc567c7ed16b\wcp.dll
        var arch = GetProcessArchitectureFolderName();
        var name = "Microsoft-Windows-ServicingStack";
        var publicKeyToken = "31bf3856ad364e35";
        var major = 10;
        var minor = 0;

        var sxsdir = SxsSearcher.GetMatchPathes(false, true, arch, name, publicKeyToken, major, minor);
        if (sxsdir.Count == 0)
        {
            throw new Exception($"cannot find {name} in WinSxS which contains wcp.dll");
        }
        var wcpDll = LoadLibraryW(Path.Combine(sxsdir[0], "wcp.dll"));
        if (wcpDll == IntPtr.Zero)
        {
            throw new Exception($"cannot load wcp.dll from {sxsdir[0]}");
        }

        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
                Instance = new WcpExX86(wcpDll);
                break;
            case Architecture.X64:
            case Architecture.Arm64:
                Instance = new WcpExX64(wcpDll);
                break;
            case Architecture.Arm:
            default:
                throw new PlatformNotSupportedException($"Unsupported process architecture: {RuntimeInformation.ProcessArchitecture}");
        }
    }

    public abstract byte[] DecompressManifestInner(byte[] manifestData);

    public IntPtr WcpDll { get; protected set; }

    protected abstract Dictionary<string, string> ProcNames { get; }

    public T GetProcDelegate<T>(string name) where T : Delegate
    {
        string lpProcName = ProcNames[name];
        var funcPtr = GetProcAddress(WcpDll, lpProcName);
        if (funcPtr == IntPtr.Zero)
        {
            throw new Exception($"failed to load {name} in wcp.dll");
        }
        return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
    }

    public static string GetProcessArchitectureFolderName()
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                return "amd64";
            case Architecture.X86:
                return "x86";
            case Architecture.Arm64:
                return "arm64";
            case Architecture.Arm:
                return "arm";
            default:
                throw new PlatformNotSupportedException($"Unsupported process architecture: {RuntimeInformation.ProcessArchitecture}");
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibraryW(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
}

public class WcpExX64 : WcpEx
{
    // amd64/arm64: long __stdcall Windows::Rtl::InitializeDeltaCompressor(void * __ptr64)
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate int InitializeDeltaCompressorDelegate(IntPtr arg);
    InitializeDeltaCompressorDelegate InitializeDeltaCompressor { get; }

    // amd64/arm64: unsigned long __stdcall Windows::WCP::Rtl::GetCompressedFileType(struct _LBLOB const * __ptr64)
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate uint GetCompressedFileTypeDelegate(ref BlobData blobData);
    GetCompressedFileTypeDelegate GetCompressedFileType { get; }

    // amd64/arm64: long __stdcall Windows::Rtl::DeltaDecompressBuffer(unsigned long,struct _LBLOB * __ptr64,unsigned __int64,struct _LBLOB * __ptr64,class Windows::Rtl::AutoDeltaBlob * __ptr64)
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate int DeltaDecompressBufferDelegate(
        uint deltaFlagType,
        ref BlobData pDictionary,
        ulong headerSize,
        ref BlobData inData,
        IntPtr outData
    );
    DeltaDecompressBufferDelegate DeltaDecompressBuffer { get; }

    // amd64/arm64: long __stdcall Windows::Rtl::LoadFirstResourceLanguageAgnostic(unsigned long,struct HINSTANCE__ * __ptr64,unsigned short const * __ptr64,unsigned short const * __ptr64,struct _LBLOB * __ptr64)
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate int LoadFirstResourceLanguageAgnosticDelegate(
        uint unused,
        IntPtr hModule,
        IntPtr lpType,
        IntPtr lpName,
        ref BlobData pDictionary
    );
    LoadFirstResourceLanguageAgnosticDelegate LoadFirstResourceLanguageAgnostic { get; }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlobData
    {
        public UIntPtr length;
        public UIntPtr fill;
        public IntPtr pData;
    }

    protected override Dictionary<string, string> ProcNames { get; } = new Dictionary<string, string>()
    {
        { "GetCompressedFileType", "?GetCompressedFileType@Rtl@WCP@Windows@@YAKPEBU_LBLOB@@@Z" },
        { "InitializeDeltaCompressor", "?InitializeDeltaCompressor@Rtl@Windows@@YAJPEAX@Z" },
        { "DeltaDecompressBuffer", "?DeltaDecompressBuffer@Rtl@Windows@@YAJKPEAU_LBLOB@@_K0PEAVAutoDeltaBlob@12@@Z" },
        { "LoadFirstResourceLanguageAgnostic", "?LoadFirstResourceLanguageAgnostic@Rtl@Windows@@YAJKPEAUHINSTANCE__@@PEBG1PEAU_LBLOB@@@Z" },

        // same with amd64
        { "arm64:GetCompressedFileType", "?GetCompressedFileType@Rtl@WCP@Windows@@YAKPEBU_LBLOB@@@Z" },
        { "arm64:InitializeDeltaCompressor", "?InitializeDeltaCompressor@Rtl@Windows@@YAJPEAX@Z" },
        { "arm64:DeltaDecompressBuffer", "?DeltaDecompressBuffer@Rtl@Windows@@YAJKPEAU_LBLOB@@_K0PEAVAutoDeltaBlob@12@@Z" },
        { "arm64:LoadFirstResourceLanguageAgnostic", "?LoadFirstResourceLanguageAgnostic@Rtl@Windows@@YAJKPEAUHINSTANCE__@@PEBG1PEAU_LBLOB@@@Z" },
    };

    public WcpExX64(IntPtr wcpDll)
    {
        WcpDll = wcpDll;
        GetCompressedFileType = GetProcDelegate<GetCompressedFileTypeDelegate>("GetCompressedFileType");
        InitializeDeltaCompressor = GetProcDelegate<InitializeDeltaCompressorDelegate>("InitializeDeltaCompressor");
        //DeltaCompressFile = GetProcDelegate<>("?DeltaCompressFile@Rtl@Windows@@YAJKPEAUIRtlFile@12@PEBU_LBLOB@@00@Z");
        DeltaDecompressBuffer = GetProcDelegate<DeltaDecompressBufferDelegate>("DeltaDecompressBuffer");
        LoadFirstResourceLanguageAgnostic = GetProcDelegate<LoadFirstResourceLanguageAgnosticDelegate>("LoadFirstResourceLanguageAgnostic");
    }

    public override byte[] DecompressManifestInner(byte[] manifestData)
    {
        IntPtr pManifest = Marshal.AllocHGlobal(manifestData.Length);
        Marshal.Copy(manifestData, 0, pManifest, manifestData.Length);

        int blobSize = Marshal.SizeOf<BlobData>();
        IntPtr pOutData = Marshal.AllocHGlobal(blobSize);
        for (int i = 0; i < blobSize; i++)
        {
            Marshal.WriteByte(pOutData, i, 0);
        }

        try
        {
            var inData = new BlobData
            {
                length = (UIntPtr)manifestData.Length,
                fill = (UIntPtr)manifestData.Length,
                pData = pManifest
            };

            uint type = GetCompressedFileType(ref inData);
            //Console.Error.WriteLine($"Type is {type}");
            if (type != 4)
            {
                //Console.Error.WriteLine($"WARNING: Untested compression type '{type}'");
            }

            int result = InitializeDeltaCompressor(IntPtr.Zero);
            //Console.Error.WriteLine($"InitializeDeltaCompressor: 0x{result:X8}");
            if (result < 0)
            {
                //Console.Error.WriteLine("InitializeDeltaCompressor failed");
                return null;
            }

            var dictData = new BlobData();
            result = LoadFirstResourceLanguageAgnostic(
                0,                    // unused
                WcpDll,               // HMODULE of wcp.dll
                (IntPtr)0x266,        // lpType Resources Type
                (IntPtr)1,            // lpName Resources Name
                ref dictData);

            //Console.Error.WriteLine($"LoadFirstResourceLanguageAgnostic: 0x{result:X8}");
            if (result < 0)
            {
                //Console.Error.WriteLine("LoadFirstResourceLanguageAgnostic failed");
                return null;
            }

            result = DeltaDecompressBuffer(
                2,          // DeltaFlagType
                ref dictData,
                4,          // headerSize
                ref inData,
                pOutData);  // AutoDeltaBlob*

            //Console.Error.WriteLine($"DeltaDecompressBuffer: 0x{result:X8}");
            if (result < 0)
            {
                //Console.Error.WriteLine("DeltaDecompressBuffer failed");
                return null;
            }

            var outData = Marshal.PtrToStructure<BlobData>(pOutData);
            long outLen = (long)(ulong)outData.length;
            byte[] output = new byte[outLen];
            Marshal.Copy(outData.pData, output, 0, (int)outLen);
            return output;
        }
        finally
        {
            Marshal.FreeHGlobal(pManifest);
            Marshal.FreeHGlobal(pOutData);
        }
    }
}

public class WcpExX86 : WcpEx
{
    // x86: long __stdcall Windows::Rtl::InitializeDeltaCompressor(void *)
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate int InitializeDeltaCompressorDelegate(uint arg);
    InitializeDeltaCompressorDelegate InitializeDeltaCompressor { get; }

    // x86: unsigned long __stdcall Windows::WCP::Rtl::GetCompressedFileType(struct _LBLOB const *)
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate uint GetCompressedFileTypeDelegate(ref BlobData blobData);
    GetCompressedFileTypeDelegate GetCompressedFileType { get; }

    // x86: long __stdcall Windows::Rtl::DeltaDecompressBuffer(unsigned long,struct _LBLOB *,unsigned long,struct _LBLOB *,class Windows::Rtl::AutoDeltaBlob *)
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate int DeltaDecompressBufferDelegate(
        uint deltaFlagType,
        ref BlobData pDictionary,
        uint headerSize,
        ref BlobData inData,
        uint outData
    );
    DeltaDecompressBufferDelegate DeltaDecompressBuffer { get; }

    // x86: long __stdcall Windows::Rtl::LoadFirstResourceLanguageAgnostic(unsigned long,struct HINSTANCE__ *,unsigned short const *,unsigned short const *,struct _LBLOB *)
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate int LoadFirstResourceLanguageAgnosticDelegate(
        uint unused,
        uint hModule,
        uint lpType,
        uint lpName,
        ref BlobData pDictionary
    );
    LoadFirstResourceLanguageAgnosticDelegate LoadFirstResourceLanguageAgnostic { get; }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlobData
    {
        public uint length;
        public uint fill;
        public uint pData;
    }

    protected override Dictionary<string, string> ProcNames { get; } = new Dictionary<string, string>()
    {
        { "GetCompressedFileType", "?GetCompressedFileType@Rtl@WCP@Windows@@YGKPBU_LBLOB@@@Z" },
        { "InitializeDeltaCompressor", "?InitializeDeltaCompressor@Rtl@Windows@@YGJPAX@Z" },
        { "DeltaDecompressBuffer", "?DeltaDecompressBuffer@Rtl@Windows@@YGJKPAU_LBLOB@@K0PAVAutoDeltaBlob@12@@Z" },
        { "LoadFirstResourceLanguageAgnostic", "?LoadFirstResourceLanguageAgnostic@Rtl@Windows@@YGJKPAUHINSTANCE__@@PBG1PAU_LBLOB@@@Z" },
    };

    public WcpExX86(IntPtr wcpDll)
    {
        WcpDll = wcpDll;
        GetCompressedFileType = GetProcDelegate<GetCompressedFileTypeDelegate>("GetCompressedFileType");
        InitializeDeltaCompressor = GetProcDelegate<InitializeDeltaCompressorDelegate>("InitializeDeltaCompressor");
        //DeltaCompressFile = GetProcDelegate<>("?DeltaCompressFile@Rtl@Windows@@YAJKPEAUIRtlFile@12@PEBU_LBLOB@@00@Z");
        DeltaDecompressBuffer = GetProcDelegate<DeltaDecompressBufferDelegate>("DeltaDecompressBuffer");
        LoadFirstResourceLanguageAgnostic = GetProcDelegate<LoadFirstResourceLanguageAgnosticDelegate>("LoadFirstResourceLanguageAgnostic");
    }

    public override byte[] DecompressManifestInner(byte[] manifestData)
    {
        // TODO x86 is not working
        IntPtr pManifest = Marshal.AllocHGlobal(manifestData.Length);
        Marshal.Copy(manifestData, 0, pManifest, manifestData.Length);

        int blobSize = Marshal.SizeOf<BlobData>();
        IntPtr pOutData = Marshal.AllocHGlobal(blobSize);
        for (int i = 0; i < blobSize; i++)
        {
            Marshal.WriteByte(pOutData, i, 0);
        }

        try
        {
            var inData = new BlobData
            {
                length = (uint)manifestData.Length,
                fill = (uint)manifestData.Length,
                pData = (uint)pManifest
            };

            uint type = GetCompressedFileType(ref inData);
            //Console.Error.WriteLine($"Type is {type}");
            if (type != 4)
            {
                //Console.Error.WriteLine($"WARNING: Untested compression type '{type}'");
            }

            int result = InitializeDeltaCompressor(0);
            //Console.Error.WriteLine($"InitializeDeltaCompressor: 0x{result:X8}");
            if (result < 0)
            {
                //Console.Error.WriteLine("InitializeDeltaCompressor failed");
                return null;
            }

            var dictData = new BlobData();
            result = LoadFirstResourceLanguageAgnostic(
                0,                    // unused
                (uint)WcpDll,               // HMODULE of wcp.dll
                (uint)0x266,        // lpType Resources Type
                (uint)1,            // lpName Resources Name
                ref dictData);

            //Console.Error.WriteLine($"LoadFirstResourceLanguageAgnostic: 0x{result:X8}");
            if (result < 0)
            {
                //Console.Error.WriteLine("LoadFirstResourceLanguageAgnostic failed");
                return null;
            }

            result = DeltaDecompressBuffer(
                2,          // DeltaFlagType
                ref dictData,
                4,          // headerSize
                ref inData,
                (uint)pOutData);  // AutoDeltaBlob*

            //Console.Error.WriteLine($"DeltaDecompressBuffer: 0x{result:X8}");
            if (result < 0)
            {
                //Console.Error.WriteLine("DeltaDecompressBuffer failed");
                return null;
            }

            var outData = Marshal.PtrToStructure<BlobData>(pOutData);
            long outLen = (long)(ulong)outData.length;
            byte[] output = new byte[outLen];
            Marshal.Copy((IntPtr)outData.pData, output, 0, (int)outLen);
            return output;
        }
        finally
        {
            Marshal.FreeHGlobal(pManifest);
            Marshal.FreeHGlobal(pOutData);
        }
    }
}
