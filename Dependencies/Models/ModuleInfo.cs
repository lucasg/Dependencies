using System;
using System.ClrPh;
using System.Collections.Generic;

[Flags]
public enum PeTypes
{
    None = 0,
    IMAGE_FILE_EXECUTABLE_IMAGE = 0x02,
    IMAGE_FILE_DLL = 0x2000,

}



public class DisplayErrorModuleInfo : DisplayModuleInfo
{
    public DisplayErrorModuleInfo(PeImportDll Module)
    : base(Module.Name)
    {
    }

    public override string Cpu { get { return null; } }
    public override string Type { get { return null; } }
    public override UInt64? Filesize { get { return null; } }
    public override UInt64? ImageBase { get { return null; } }
    public override int? VirtualSize { get { return null; } }
    public override UInt64? EntryPoint { get { return null; } }
    public override int? Subsystem { get { return null; } }
    public override string SubsystemVersion { get { return null; } }
    public override int? Checksum { get { return null; } }
    public override bool? CorrectChecksum { get { return null; } }

}

public class DisplayModuleInfo : DefaultSettingsBindingHandler
{
    public DisplayModuleInfo(string ModuleName)
    {
        Info.Name = ModuleName;
        Info.Filepath = "";

        AddNewEventHandler("FullPath", "FullPath", "Name", this.GetPathDisplayName);
    }

    public DisplayModuleInfo(PeImportDll Module, PE Pe)
    {   
        Info.Name = Module.Name;
        Info.Filepath = Pe.Filepath;

        Info.Machine = Pe.Properties.Machine;
        Info.Magic = Pe.Properties.Magic;

        Info.ImageBase = (UInt64) Pe.Properties.ImageBase;
        Info.SizeOfImage = Pe.Properties.SizeOfImage;
        Info.EntryPoint = (UInt64) Pe.Properties.EntryPoint;

        Info.Checksum = Pe.Properties.Checksum;
        Info.CorrectChecksum = Pe.Properties.CorrectChecksum;

        Info.Subsystem = Pe.Properties.Subsystem;
        Info.SubsystemVersion = Pe.Properties.SubsystemVersion;
        Info.Characteristics = Pe.Properties.Characteristics;
        Info.DllCharacteristics = Pe.Properties.DllCharacteristics;

        Info.Filesize = Pe.Properties.FileSize;

        Info.context.ImportProperties = Module;
        Info.context.PeProperties = Pe;

        AddNewEventHandler("FullPath", "FullPath", "ModuleName", this.GetPathDisplayName);
    }

    public virtual TreeViewItemContext Context { get { return Info.context; } }

    public string ModuleName
    {
        get { return GetPathDisplayName(Dependencies.Properties.Settings.Default.FullPath); }
    }

    //public virtual string Name { get { return Info.Name; } }
    public virtual string Cpu
    {
        get
        {
            int machine_id = Info.Machine & 0xffff;
            switch (machine_id)
            {
                case 0x014c: /*IMAGE_FILE_MACHINE_I386*/
                    return "i386";

                case 0x8664: /*IMAGE_FILE_MACHINE_AMD64*/
                    return "AMD64";

                case 0x0200:/*IMAGE_FILE_MACHINE_IA64*/
                    return "IA64";

                case 0x01c4:/*IMAGE_FILE_MACHINE_ARMNT*/
                    return "ARM Thumb-2";

                default:
                    return "Unknown";
            }
        }
    }
    public virtual string Type
    {
        get
        {
            List<String> TypeList = new List<String>();
            PeTypes Type = (PeTypes)Info.Characteristics;

            if ((Type & PeTypes.IMAGE_FILE_DLL) != PeTypes.None)/* IMAGE_FILE_DLL */
                TypeList.Add("Dll");

            if ((Type & PeTypes.IMAGE_FILE_EXECUTABLE_IMAGE) != PeTypes.None) /* IMAGE_FILE_EXECUTABLE_IMAGE */
                TypeList.Add("Executable");



            return String.Join("; ", TypeList.ToArray());
        }
    }
    public virtual UInt64? Filesize { get { return Info.Filesize; } }
    public virtual UInt64? ImageBase { get { return Info.ImageBase; } }
    public virtual int? VirtualSize { get { return Info.SizeOfImage; } }
    public virtual UInt64? EntryPoint { get { return Info.EntryPoint; } }
    public virtual int? Subsystem { get { return Info.Subsystem; } }
    public virtual string SubsystemVersion { get { return String.Format("{0:d}.{1:d}" , Info.SubsystemVersion.Item1, Info.SubsystemVersion.Item2); } }
    public virtual int? Checksum { get { return Info.Checksum; } }
    public virtual bool? CorrectChecksum { get { return Info.CorrectChecksum; } }


    protected string GetPathDisplayName(bool FullPath)
    {
        if ((FullPath) && (Info.Filepath.Length > 0))
            return Info.Filepath;

        return Info.Name;
    }

    private ModuleInfo Info;
}


public struct ModuleInfo
{
    // @TODO(Hack: refactor correctly for image generation)
    public TreeViewItemContext context;

    public string Name;
    public string Filepath;

    public Int16 Machine;
    public Int16 Magic;

    public UInt64 ImageBase;
    public Int32 SizeOfImage;
    public UInt64 EntryPoint;


    public Int32 Checksum;
    public Boolean CorrectChecksum;

    public Int16 Subsystem;
    public Tuple<Int16, Int16> SubsystemVersion;

    public Int16 Characteristics;
    public Int16 DllCharacteristics;

    public UInt64 Filesize;
}