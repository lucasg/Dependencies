using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Data;
using Microsoft.Win32;

using Mono.Cecil;
using Dependencies.ClrPh;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Runtime.InteropServices;

namespace Dependencies
{
    
    public class ReplaceRuleLoader
    {
        [JsonProperty]
        public Dictionary<char,string> ReplaceRules { get; set; }
        public static ReplaceRuleLoader GetReplaceRules(string cfg)
        {
            ReplaceRuleLoader loader = new ReplaceRuleLoader();
            try
            {
                var s = File.ReadAllText(cfg);
                loader.ReplaceRules = JsonConvert.DeserializeObject<Dictionary<char, string>>(s);
            }
            catch
            {
                loader.ReplaceRules = new Dictionary<char, string>();
                loader.ReplaceRules['?'] = "_H3F_";//ascii number
                loader.ReplaceRules['@'] = "_H40_";
                loader.ReplaceRules['$'] = "_H24_";

                var save=JsonConvert.SerializeObject(loader.ReplaceRules,Formatting.Indented);
                File.WriteAllText(cfg, save);

            }
            return loader;
        }
        public string Get_NameInSourceCode_From_Name(string name)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var c in name)
            {
                if(
                    ((c>='0')&&(c<='9'))||
                    ((c >= 'A') && (c <= 'Z'))||
                    ((c >= 'a') && (c <= 'z'))||
                    (c=='_')
                    )
                {
                    sb.Append(c);
                    continue;
                }
                if(ReplaceRules.ContainsKey(c)==false)
                {
                    throw new Exception($"please edit the rule file,add new string of {c},example:");
                }
                sb.Append(ReplaceRules[c]);
            }
            return sb.ToString();
        }
    }
    public class AheadlibFunction
    {
        
        public ushort Ordinal { get; set; }

        public string Name { get; set; }//dll name

        private string _UndecorateName;
        public CallingConvention CallingConvention { get; private set; }//only c++ export
        public bool IsCppExport { get; set; } = false;
        public string UndecorateName
        {
            get
            {
                return _UndecorateName;
            }
            set
            {
                _UndecorateName = value;
                if(value.IndexOf("__cdecl") !=-1)
                {
                    IsCppExport = true;
                    CallingConvention = CallingConvention.Cdecl;
                }
                else if (value.IndexOf("__stdcall") != -1)
                {
                    IsCppExport = true;
                    CallingConvention = CallingConvention.StdCall;
                }
                else if (value.IndexOf("__thiscall") != -1)
                {
                    IsCppExport = true;
                    CallingConvention = CallingConvention.ThisCall;
                }
                else if (value.IndexOf("__fastcall") != -1)
                {
                    IsCppExport = true;
                    CallingConvention = CallingConvention.FastCall;
                }
                else
                {
                    IsCppExport = false;
                }
            }
        }
        public bool ExportByOrdinal { get; set; }
        public long VirtualAddress { get; set; }

        public string NameInSourceCode { get; set; }    
    }
    public class AheadlibCodeGenerator
    {
        private string CodeGenPath { get; set; }
        private string OldDllName { get; set; }
        private bool IsCodegenFunctionTrace { get; set; }
        private string LogPath { get; set; }
        private List<AheadlibFunction> Functions { get; set; }
        private DisplayModuleInfo DllTarget { get; set; }

        private ReplaceRuleLoader ReplaceRuleLoader;
        public AheadlibCodeGenerator(string codeGenPath,string oldDllName,bool isCodegenFunctionTrace,string logPath, DisplayModuleInfo dlltarget,string cfg)
        {
            CodeGenPath = codeGenPath;
            OldDllName = oldDllName;
            IsCodegenFunctionTrace = isCodegenFunctionTrace;
            LogPath = logPath;

            DllTarget = dlltarget;
            Functions = new List<AheadlibFunction>();
            ReplaceRuleLoader = ReplaceRuleLoader.GetReplaceRules(cfg);
            PhSymbolProvider symbolProvider = new PhSymbolProvider();
            foreach (var expfunction in dlltarget.Exports)
            {
                AheadlibFunction function = new AheadlibFunction();
                function.Ordinal = expfunction.Ordinal;
                function.Name = expfunction.Name;
                var dx = new DisplayPeExport(expfunction, symbolProvider);
                function.UndecorateName = dx.Name;
                function.NameInSourceCode = ReplaceRuleLoader.Get_NameInSourceCode_From_Name(function.Name);
                function.ExportByOrdinal = expfunction.ExportByOrdinal;
                function.VirtualAddress = expfunction.VirtualAddress;
                Functions.Add(function);
            }
            
        }

        
        public async void CodeGen()
        {
            bool IsCodeGenSuccess = false;
            string errorstring = string.Empty;
            await Task.Run(() =>
            {
                try
                {
                    //codegen cpp
                    FileInfo dllfi = new FileInfo(DllTarget.ModuleName);
                    string dllfn = dllfi.Name.Split('.')[0];

                    StreamWriter dllcppsw = new StreamWriter(CodeGenPath + "/" + dllfn + ".c", false, new UTF8Encoding(false));

                    StreamWriter dllhsw = new StreamWriter(CodeGenPath + "/" + dllfn + ".h", false, new UTF8Encoding(false));
                    dllhsw.WriteLine($"#ifndef {dllfn}_H");
                    dllhsw.WriteLine($"#define {dllfn}_H");
                    dllhsw.WriteLine($"//aheadlib plugin for csharp.by snikeguo,email:408260925@qq.com");
                    dllhsw.WriteLine($"//codegen time:{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss:fff")}");
                    //#ifndef __cplusplus
                    dllhsw.WriteLine("#ifdef __cplusplus");
                    dllhsw.WriteLine("extern \"C\" {");
                    dllhsw.WriteLine("#endif");
                    dllhsw.WriteLine("#include<Windows.h>");
                    dllhsw.WriteLine("#include<Shlwapi.h>");

                    foreach (var func in Functions)
                    {
                        dllhsw.WriteLine($"extern PVOID pfnAL_{func.NameInSourceCode};//{func.Name}");
                    }

                    foreach (var func in Functions)
                    {
                        dllhsw.WriteLine($"extern PVOID Old_pfnAL_{func.NameInSourceCode};//{func.Name}");
                    }

                    dllhsw.WriteLine($"extern BOOL WINAPI {dllfn}_Init" +
                       "();");

                    dllhsw.WriteLine("#ifdef __cplusplus");
                    dllhsw.WriteLine("}");
                    dllhsw.WriteLine("#endif");

                    dllhsw.WriteLine("#endif");
                    dllhsw.Flush();
                    dllhsw.Close();

                    dllcppsw.WriteLine($"#include\"{dllfn}.h\"");
                    dllcppsw.WriteLine("#pragma comment( lib, \"Shlwapi.lib\")");

                    if (DllTarget.Cpu.ToLower() == "i386")
                    {
                        foreach (var func in Functions)
                        {
                            dllcppsw.WriteLine($"#pragma comment(linker,\"/EXPORT:{func.Name}=_AL_{func.Name},@{func.Ordinal}\")");
                        }
                    }
                    else if (DllTarget.Cpu.ToLower() == "amd64")
                    {
                        foreach (var func in Functions)
                        {
                            dllcppsw.WriteLine($"#pragma comment(linker,\"/EXPORT:{func.Name}=AL_{func.Name},@{func.Ordinal}\")");
                        }
                    }


                    foreach (var func in Functions)
                    {
                        dllcppsw.WriteLine($"PVOID pfnAL_{func.NameInSourceCode};");
                    }
                    foreach (var func in Functions)
                    {
                        dllcppsw.WriteLine($"PVOID Old_pfnAL_{func.NameInSourceCode};");//define old dll function point
                    }
                    //load function
                    dllcppsw.WriteLine($"HMODULE {dllfn}_Old_Module;");
                    dllcppsw.WriteLine("BOOL WINAPI Load()\r\n{");
                    dllcppsw.WriteLine($"{dllfn}_Old_Module=LoadLibrary(L\"{OldDllName}\");");
                    dllcppsw.Write($"if({dllfn}_Old_Module==NULL)");
                    dllcppsw.WriteLine("{return FALSE;}");
                    dllcppsw.WriteLine("else\r\n{return TRUE;}");
                    dllcppsw.WriteLine("}");

                    dllcppsw.WriteLine("void GetAddresses()\r\n{");

                    foreach (var func in Functions)
                    {
                        dllcppsw.WriteLine($"   pfnAL_{func.NameInSourceCode}=(PVOID)GetProcAddress({dllfn}_Old_Module,\"{func.Name}\");");
                    }

                    foreach (var func in Functions)
                    {
                        dllcppsw.WriteLine($"   Old_pfnAL_{func.NameInSourceCode}=pfnAL_{func.NameInSourceCode};");
                    }

                    dllcppsw.WriteLine("}");

                    if (DllTarget.Cpu.ToLower() == "i386")
                    {
                        StreamWriter x32asmjmpcodesw = new StreamWriter(CodeGenPath + "/" + dllfn + "_jump.asm", false, new UTF8Encoding(false));

                        x32asmjmpcodesw.WriteLine("; 把 .asm 文件添加到工程一次");
                        x32asmjmpcodesw.WriteLine("; 右键单击文件-属性-常规-");
                        x32asmjmpcodesw.WriteLine("; 项类型:自定义生成工具");
                        x32asmjmpcodesw.WriteLine("; 从生成中排除:否");
                        x32asmjmpcodesw.WriteLine("; 然后复制下面命令填入");
                        x32asmjmpcodesw.WriteLine("; 命令行: ml /Fo $(IntDir)%(fileName).obj /c /Cp %(fileName).asm");
                        x32asmjmpcodesw.WriteLine("; 输出: $(IntDir)%(fileName).obj;%(Outputs)");
                        x32asmjmpcodesw.WriteLine("; 链接对象: 是");


                        x32asmjmpcodesw.WriteLine(".686P");
                        x32asmjmpcodesw.WriteLine(".MODEL FLAT,C");
                        x32asmjmpcodesw.WriteLine(".DATA");

                        foreach (var func in Functions)
                        {
                            x32asmjmpcodesw.WriteLine($"EXTERN pfnAL_{func.NameInSourceCode}:DWORD");
                        }
                        x32asmjmpcodesw.WriteLine("\r\n\r\n;jmp code");
                        x32asmjmpcodesw.WriteLine(".CODE");
                        foreach (var func in Functions)
                        {
                            string s = $"AL_{func.Name} PROC\r\n";
                            s += $"jmp pfnAL_{func.NameInSourceCode}\r\n";
                            s += $"AL_{func.Name} ENDP\r\n";
                            x32asmjmpcodesw.WriteLine(s);
                        }
                        x32asmjmpcodesw.WriteLine("END");
                        x32asmjmpcodesw.Flush();
                        x32asmjmpcodesw.Close();
                    }
                    else if (DllTarget.Cpu.ToLower() == "amd64")
                    {
                        StreamWriter x64asmjmpcodesw = new StreamWriter(CodeGenPath + "/" + dllfn + "_jump.asm", false, new UTF8Encoding(false));

                        x64asmjmpcodesw.WriteLine("; 把 .asm 文件添加到工程一次");
                        x64asmjmpcodesw.WriteLine("; 右键单击文件-属性-常规-");
                        x64asmjmpcodesw.WriteLine("; 项类型:自定义生成工具");
                        x64asmjmpcodesw.WriteLine("; 从生成中排除:否");
                        x64asmjmpcodesw.WriteLine("; 然后复制下面命令填入");
                        x64asmjmpcodesw.WriteLine("; 命令行: ml64 /Fo $(IntDir)%(fileName).obj /c /Cp %(fileName).asm");
                        x64asmjmpcodesw.WriteLine("; 输出: $(IntDir)%(fileName).obj;%(Outputs)");
                        x64asmjmpcodesw.WriteLine("; 链接对象: 是");


                        x64asmjmpcodesw.WriteLine(".DATA");
                        foreach (var func in Functions)
                        {
                            x64asmjmpcodesw.WriteLine($"EXTERN pfnAL_{func.NameInSourceCode}:QWORD");
                        }
                        x64asmjmpcodesw.WriteLine("\r\n\r\n;jmp code");
                        foreach (var func in Functions)
                        {
                            string s = $"AL_{func.Name} PROC\r\n";
                            s += $"jmp pfnAL_{func.NameInSourceCode}\r\n";
                            s += $"AL_{func.Name} ENDP\r\n";
                            x64asmjmpcodesw.WriteLine(s);
                        }
                        x64asmjmpcodesw.WriteLine("END");
                        x64asmjmpcodesw.Flush();
                        x64asmjmpcodesw.Close();
                    }


                    dllcppsw.WriteLine($"BOOL WINAPI {dllfn}_Init" +
                        "(){");

                    dllcppsw.WriteLine("BOOL loadresult=Load();");
                    dllcppsw.WriteLine("if(!loadresult){return FALSE;}");
                    dllcppsw.WriteLine("GetAddresses();");
                    dllcppsw.WriteLine("DWORD old = 0;");
                    dllcppsw.WriteLine($"UINT64 LowAddr = (UINT64)&Old_pfnAL_{Functions[Functions.Count - 1].NameInSourceCode};");
                    dllcppsw.WriteLine($"UINT64 HighAddr = (UINT64)&pfnAL_{Functions[0].NameInSourceCode};");
                    dllcppsw.WriteLine($"int VirtualProtectResult=VirtualProtect((LPVOID)LowAddr," +
                        $"(HighAddr-LowAddr+(UINT64)(sizeof(PVOID)))," +
                        $"PAGE_EXECUTE_READWRITE," +
                        $"&old);");
                    dllcppsw.WriteLine("return TRUE;\r\n}");

                    dllcppsw.Flush();
                    dllcppsw.Close();

                    if(IsCodegenFunctionTrace==true)
                    {
                        StreamWriter tracehsw = new StreamWriter(CodeGenPath + "/" + dllfn + ".trace.h", false, new UTF8Encoding(false));
                        tracehsw.WriteLine($"#ifndef {dllfn}_TRACE_H");
                        tracehsw.WriteLine($"#define {dllfn}_TRACE_H");

                        tracehsw.WriteLine("#ifdef __cplusplus");
                        tracehsw.WriteLine("extern \"C\" {");
                        tracehsw.WriteLine("#endif");

                        tracehsw.WriteLine($"void {dllfn}_TraceInit();");
                        tracehsw.WriteLine($"#endif");

                        tracehsw.WriteLine("#ifdef __cplusplus");
                        tracehsw.WriteLine("}");
                        tracehsw.WriteLine("#endif");

                        tracehsw.Close();

                        StreamWriter tracecsw = new StreamWriter(CodeGenPath + "/" + dllfn + ".trace.c", false, new UTF8Encoding(false));
                        tracecsw.WriteLine($"#include\"{dllfn}.h\"");
                        tracecsw.WriteLine("#include<stdio.h>");
                        tracecsw.WriteLine("static char LogFileName[256];");

                        tracecsw.WriteLine("static char *GetDateTimeString(void)");
                        tracecsw.WriteLine("{");
                        tracecsw.WriteLine("    static char s[128];");
                        tracecsw.WriteLine("    SYSTEMTIME sys;");
                        tracecsw.WriteLine("    GetLocalTime(&sys);");
                        tracecsw.WriteLine("    sprintf_s(s,128, \" %4d-%2d-%2d %2d:%2d:%2d:%4d\", sys.wYear, sys.wMonth, sys.wDay, sys.wHour, sys.wMinute, sys.wSecond, sys.wMilliseconds);");
                        tracecsw.WriteLine("    return s;");
                        tracecsw.WriteLine("}");

                        tracecsw.WriteLine("static void TraceImpl(char *tracefuntion,char *NameInSourceCode,char *dllUndecorateName,char *dllfunction)");
                        tracecsw.WriteLine("{");
                        tracecsw.WriteLine("    FILE* f;");
                        tracecsw.WriteLine("    f = fopen(LogFileName, \"at\");");
                        tracecsw.WriteLine("    fprintf(f, \"%-64s%-100s%-100s%-100s%-100s\\n\", GetDateTimeString(),tracefuntion,NameInSourceCode,dllUndecorateName,dllfunction);");
                        tracecsw.WriteLine("    fclose(f);");
                        tracecsw.WriteLine("}");

                        foreach (var func in Functions)
                        {
                            tracecsw.WriteLine($"__declspec(naked) void Trace_{func.NameInSourceCode}(void)");
                            tracecsw.WriteLine("{");
                            tracecsw.WriteLine($"   TraceImpl(__FUNCTION__,\"{func.NameInSourceCode}\",\"{func.UndecorateName}\",\"{func.Name}\");");
                            string jmptemp = "{"+$"jmp Old_pfnAL_{func.NameInSourceCode}"+"}";
                            tracecsw.WriteLine($"    __asm {jmptemp}");
                            tracecsw.WriteLine("}");
                        }

                        tracecsw.WriteLine($"void {dllfn}_TraceInit()");
                        tracecsw.WriteLine("{");
                        tracecsw.WriteLine( "   SYSTEMTIME sys;");
                        tracecsw.WriteLine( "   GetLocalTime(&sys);");
                        tracecsw.WriteLine($"   sprintf_s(LogFileName,256, \"{LogPath}{dllfn}_%4d-%2d-%2d__%2d_%2d_%2d_%4d.txt\",sys.wYear, sys.wMonth, sys.wDay, sys.wHour, sys.wMinute, sys.wSecond, sys.wMilliseconds);");

                        tracecsw.WriteLine("    FILE* f;");
                        tracecsw.WriteLine("    f = fopen(LogFileName, \"at\");");
                        tracecsw.WriteLine("    fprintf(f, \"%-64s%-100s%-100s%-100s%-100s\\n\",\"time\",\"trace function name\",\"NameInSourceCode\",\"UndecorateName\",\"Dll Name\");");
                        tracecsw.WriteLine("    fclose(f);");

                        foreach (var func in Functions)
                        {
                            tracecsw.WriteLine($"   pfnAL_{func.NameInSourceCode}=Trace_{func.NameInSourceCode};");
                        }
                        tracecsw.WriteLine("}");
                        tracecsw.Close();
                    }

                    IsCodeGenSuccess = true;
                }
                catch (Exception ex)
                {
                    errorstring = ex.Message;
                }

            });
            if(IsCodeGenSuccess==true)
            {
                MessageBox.Show("success!");
            }
            else
            {
                MessageBox.Show(errorstring, "error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    public partial class ModuleTreeViewItem 
    {
        private RelayCommand _AheadlibCommnad;
        //AheadLibCommnad
        public RelayCommand AheadLibCommnad
        {
            get
            {
                if (_AheadlibCommnad == null)
                {
                    _AheadlibCommnad = new RelayCommand((param) => this.AheadLibCodeGen((object)param));
                }

                return _AheadlibCommnad;
            }
        }
        private string _CodeGenPath;
        private string _OldDllFullName;
        private bool _IsCodegenFunctionTrace;
        private string _LogPath;
        private string ModuleName;
        public bool AheadLibCodeGen(object Context)
        {
            var dnc = (DependencyNodeContext)Context;
            var target = (DisplayModuleInfo)dnc.ModuleInfo.Target;
            FileInfo dllfi = new FileInfo(target.Filepath);
            AheadLibConfig cfgfrm = new AheadLibConfig();
            if(ModuleName != target.ModuleName)
            {
                _OldDllFullName = dllfi.Name+".old";
                _IsCodegenFunctionTrace = true;
                _LogPath = "D:/";
                ModuleName = target.ModuleName;
            }
            cfgfrm.CodeGenPath = _CodeGenPath;
            cfgfrm.OldDllFullName = _OldDllFullName;
            cfgfrm.IsCodegenFunctionTrace = _IsCodegenFunctionTrace;
            cfgfrm.LogPath = _LogPath;
            if (cfgfrm.ShowDialog() == MessageBoxResult.Cancel)
            {
                return false;
            }
            _CodeGenPath = cfgfrm.CodeGenPath;
            _OldDllFullName = cfgfrm.OldDllFullName;
            _IsCodegenFunctionTrace = cfgfrm.IsCodegenFunctionTrace;
            _LogPath = cfgfrm.LogPath;

            var logpath = cfgfrm.LogPath.Replace('\\', '/');
            if(logpath[logpath.Length-1]!='/')
            {
                logpath += '/';
            }
            AheadlibCodeGenerator generator = new AheadlibCodeGenerator(cfgfrm.CodeGenPath, 
                cfgfrm.OldDllFullName.Replace('\\','/'),
                cfgfrm.IsCodegenFunctionTrace,
                logpath,
                target,
                "aheadlib.rules");
            generator.CodeGen();

            return true;
        }
    }
}