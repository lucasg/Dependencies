using System;

[Flags]
public enum PeTypes
{
    None = 0,
    IMAGE_FILE_EXECUTABLE_IMAGE = 0x02,
    IMAGE_FILE_DLL = 0x2000,
}

[Flags]
public enum ModuleFlag
{
    NoFlag = 0x00,

    DelayLoad = 0x01,
    ClrReference = 0x02,
    ApiSet = 0x04,
    ApiSetExt = 0x08,
    NotFound = 0x10,
    MissingImports = 0x20,
    ChildrenError = 0x40,
    SXSManifestError = 0x80,
}
