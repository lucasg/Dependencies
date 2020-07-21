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
        public bool ExportByOrdinal { get; set; }
        public long VirtualAddress { get; set; }

        public string NameInSourceCode { get; set; }    
    }
    public class AheadlibCodeGenerator
    {
        private string CodeGenPath { get; set; }
        private List<AheadlibFunction> Functions { get; set; }
        private DisplayModuleInfo DllTarget { get; set; }

        private ReplaceRuleLoader ReplaceRuleLoader;
        public AheadlibCodeGenerator(string path, DisplayModuleInfo dlltarget,string cfg)
        {
            CodeGenPath = path;
            DllTarget = dlltarget;
            Functions = new List<AheadlibFunction>();
            ReplaceRuleLoader = ReplaceRuleLoader.GetReplaceRules(cfg);
            foreach (var expfunction in dlltarget.Exports)
            {
                AheadlibFunction function = new AheadlibFunction();
                function.Ordinal = expfunction.Ordinal;
                function.Name = expfunction.Name;
                function.NameInSourceCode = ReplaceRuleLoader.Get_NameInSourceCode_From_Name(function.Name);
                function.ExportByOrdinal = expfunction.ExportByOrdinal;
                function.VirtualAddress = expfunction.VirtualAddress;
                Functions.Add(function);
            }
            
        }

        private void EmitLoad(StreamWriter sw)
        {
            FileInfo dllfi = new FileInfo(DllTarget.ModuleName);
            string dllfn = dllfi.Name.Split('.')[0];
            sw.WriteLine($"HMODULE {dllfn}_Old_Module;");
            sw.WriteLine("BOOL WINAPI Load()\r\n{");
            sw.WriteLine($"{dllfn}_Old_Module=LoadLibrary(\"{dllfi.Name}\");");
            sw.Write($"if({dllfn}_Old_Module==nullptr)");
            sw.WriteLine("{return false;}");
            sw.WriteLine("else\r\n{return true;}");
            sw.WriteLine("}");
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

                    StreamWriter dllcppsw = new StreamWriter(CodeGenPath + "/" + dllfn + ".cpp", false, Encoding.UTF8);

                    StreamWriter dllhsw = new StreamWriter(CodeGenPath + "/" + dllfn + ".h", false, Encoding.UTF8);
                    dllhsw.WriteLine($"#ifndef {dllfn}_H");
                    dllhsw.WriteLine($"#define {dllfn}_H");
                    dllhsw.WriteLine($"//aheadlib plugin for csharp.by snikeguo,email:408260925@qq.com");

                    dllhsw.WriteLine("#include<Windows.h>");
                    dllhsw.WriteLine("#include<Shlwapi.h>");

                    foreach (var func in Functions)
                    {
                        dllhsw.WriteLine($"extern PVOID pfnAheadLib_{func.NameInSourceCode};");
                    }
                    dllhsw.WriteLine($"extern BOOL WINAPI {dllfn}_Init" +
                       "();");
                    dllhsw.WriteLine("#endif");
                    dllhsw.Flush();
                    dllhsw.Close();

                    dllcppsw.WriteLine($"#include\"{dllfn}.h\"");
                    dllcppsw.WriteLine("#pragma comment( lib, \"Shlwapi.lib\")");

                    if (DllTarget.Cpu.ToLower() == "i386")
                    {
                        foreach (var func in Functions)
                        {
                            dllcppsw.WriteLine($"#pragma comment(linker,\"/EXPORT:{func.Name}=_AheadLib_{func.NameInSourceCode},@{func.Ordinal})\"");
                        }
                    }
                    else if (DllTarget.Cpu.ToLower() == "amd64")
                    {
                        foreach (var func in Functions)
                        {
                            dllcppsw.WriteLine($"#pragma comment(linker,\"/EXPORT:{func.Name}=AheadLib_{func.NameInSourceCode},@{func.Ordinal})\"");
                        }
                    }

                    foreach (var func in Functions)
                    {
                        dllcppsw.WriteLine($"PVOID pfnAheadLib_{func.NameInSourceCode};");
                    }

                    //load function
                    dllcppsw.WriteLine($"HMODULE {dllfn}_Old_Module;");
                    dllcppsw.WriteLine("BOOL WINAPI Load()\r\n{");
                    dllcppsw.WriteLine($"{dllfn}_Old_Module=LoadLibrary(\"{dllfi.Name}\");");
                    dllcppsw.Write($"if({dllfn}_Old_Module==nullptr)");
                    dllcppsw.WriteLine("{return false;}");
                    dllcppsw.WriteLine("else\r\n{return true;}");
                    dllcppsw.WriteLine("}");

                    dllcppsw.WriteLine("void GetAddresses()\r\n{");

                    foreach (var func in Functions)
                    {
                        dllcppsw.WriteLine($"   pfnAheadLib_{func.NameInSourceCode}=(PVOID)GetProcAddress({dllfn}_Old_Module,\"{func.Name}\");");
                    }

                    dllcppsw.WriteLine("}");

                    if (DllTarget.Cpu.ToLower() == "i386")
                    {
                        foreach (var func in Functions)
                        {
                            string s = $"EXTERN_C __declspec(naked) void __cdecl  AheadLib_{func.NameInSourceCode}(void)\r\n";
                            s += "{\r\n";
                            s += $"     __asm jmp pfnAheadLib_{func.NameInSourceCode};\r\n";
                            s += "}";
                            dllcppsw.WriteLine(s);
                        }
                    }
                    else if (DllTarget.Cpu.ToLower() == "amd64")
                    {
                        StreamWriter x64asmjmpcodesw = new StreamWriter(CodeGenPath + "/" + dllfn + "_jump.asm", false, Encoding.UTF8);

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
                            x64asmjmpcodesw.WriteLine($"EXTERN pfnAheadLib_{func.NameInSourceCode}:dq;");
                        }
                        x64asmjmpcodesw.WriteLine("\r\n\r\n;jmp code");
                        foreach (var func in Functions)
                        {
                            string s = $"AheadLib_{func.Name} PROC\r\n";
                            s += $"jmp pfnAheadLib_{func.NameInSourceCode}\r\n";
                            s += $"AheadLib_{func.Name} ENDP\r\n";
                            x64asmjmpcodesw.WriteLine(s);
                        }
                        x64asmjmpcodesw.Flush();
                        x64asmjmpcodesw.Close();
                    }


                    dllcppsw.WriteLine($"BOOL WINAPI {dllfn}_Init" +
                        "(){");

                    dllcppsw.WriteLine("bool loadresult=Load();");
                    dllcppsw.WriteLine("if(!loadresult){return false;}");
                    dllcppsw.WriteLine("GetAddresses();");
                    dllcppsw.WriteLine("DWORD old = 0;");
                    dllcppsw.WriteLine($"VirtualProtect((LPVOID)&pfnAheadLib_{Functions[0].NameInSourceCode}," +
                        $"((UINT64)&pfnAheadLib_{Functions[Functions.Count - 1].NameInSourceCode})-(UINT64)&pfnAheadLib_{Functions[0].NameInSourceCode})+(UINT64)(sizeof(PVOID))," +
                        $"PAGE_EXECUTE_READWRITE," +
                        $"&old);");
                    dllcppsw.WriteLine("return true;\r\n}");

                    dllcppsw.Flush();
                    dllcppsw.Close();

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
        string AheadLibCodeGenFoldPath;
        public bool AheadLibCodeGen(object Context)
        {
            bool openflag = true;
            //if 'AheadLibCodeGenFoldPath' is exist && no need choose the path,then use old path
            if (Directory.Exists(AheadLibCodeGenFoldPath) == true)
            {
                if (MessageBox.Show("aheadlib codegen path:\r\n  " + AheadLibCodeGenFoldPath + "\r\n" +
                    "Do you want to choose a new path?\r\n" +
                    "YES - choose a new path\r\n" +
                    "NO -use old path\r\n", "question",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No) == MessageBoxResult.No)
                {
                    openflag = false;
                }
            }
            if (openflag == true)
            {
                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "please codegen path";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    AheadLibCodeGenFoldPath = dialog.SelectedPath;
                }
                else
                {
                    return true;
                }
            }

            var dnc = (DependencyNodeContext)Context;
            var target = (DisplayModuleInfo)dnc.ModuleInfo.Target;

            AheadlibCodeGenerator generator = new AheadlibCodeGenerator(AheadLibCodeGenFoldPath, target,"aheadlib.rules");
            generator.CodeGen();

            return true;
        }
    }
}