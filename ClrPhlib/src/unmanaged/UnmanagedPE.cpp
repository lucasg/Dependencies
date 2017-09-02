#include <ClrPhlib.h>
#include <UnmanagedPh.h>
#include <phnt_ntdef.h>

using namespace System;
using namespace ClrPh;


UnmanagedPE::UnmanagedPE()
        :m_bImageLoaded(false)
{
    memset(&m_PvMappedImage, 0, sizeof(PH_MAPPED_IMAGE));
}

bool UnmanagedPE::LoadPE(LPWSTR Filepath)
{
    if (m_bImageLoaded)
    {
        PhUnloadMappedImage(&m_PvMappedImage);
    }

    memset(&m_PvMappedImage, 0, sizeof(PH_MAPPED_IMAGE));

    m_bImageLoaded = NT_SUCCESS(PhLoadMappedImage(
        Filepath,
        NULL,
        TRUE,
        &m_PvMappedImage
    ));

    return m_bImageLoaded;
}

UnmanagedPE::~UnmanagedPE()
{
	if (m_bImageLoaded)
	{
		PhUnloadMappedImage(&m_PvMappedImage);
	}
}


NTSTATUS PhGetMappedImageResourceRoot(
	_In_ PPH_MAPPED_IMAGE MappedImage,
	_Out_ PIMAGE_RESOURCE_DIRECTORY *RootDirectory
)
{
	NTSTATUS status;
	PIMAGE_DATA_DIRECTORY entry;
	PVOID resourceRootDir;

	status = PhGetMappedImageDataEntry(MappedImage, IMAGE_DIRECTORY_ENTRY_RESOURCE, &entry);
	if (!NT_SUCCESS(status))
		return status;

	if (!entry->VirtualAddress)
		return STATUS_RESOURCE_DATA_NOT_FOUND;

	resourceRootDir = PhMappedImageRvaToVa(MappedImage, entry->VirtualAddress, NULL);
	if (!resourceRootDir)
		return STATUS_RESOURCE_DATA_NOT_FOUND;

	__try
	{
		//	PhpMappedImageProbe(&m_PvMappedImage, resourceRootDir, sizeof(IMAGE_RESOURCE_DIRECTORY));
		PhProbeAddress(resourceRootDir, sizeof(IMAGE_RESOURCE_DIRECTORY), MappedImage->ViewBase, MappedImage->Size, 1);
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		return GetExceptionCode();
	}

	*RootDirectory = (PIMAGE_RESOURCE_DIRECTORY) resourceRootDir;
	return STATUS_SUCCESS;
}


NTSTATUS PhGetMappedImageResourceDirectoryNamedEntry(
	_In_ PPH_MAPPED_IMAGE MappedImage,
	_In_ PIMAGE_RESOURCE_DIRECTORY RootDirectory,
	_In_opt_ LPTSTR  Name,
	_Out_ PIMAGE_RESOURCE_DIRECTORY_ENTRY *Entry
)
{
	IMAGE_RESOURCE_DIRECTORY_ENTRY *NamedEntriesArray = (IMAGE_RESOURCE_DIRECTORY_ENTRY*)((ULONG_PTR) RootDirectory + sizeof(IMAGE_RESOURCE_DIRECTORY));
	
	__try
	{
		//	PhpMappedImageProbe(&m_PvMappedImage, resourceRootDir, ProbeLength);
		PhProbeAddress(NamedEntriesArray, sizeof(IMAGE_RESOURCE_DIRECTORY_ENTRY) * RootDirectory->NumberOfNamedEntries, MappedImage->ViewBase, MappedImage->Size, 1);
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		return GetExceptionCode();
	}

	for (int i = 0; i < RootDirectory->NumberOfNamedEntries; i++)
	{
		if (NamedEntriesArray[i].NameIsString)
		{
			UNICODE_STRING *UnicodeName = (UNICODE_STRING*) ((BYTE*)RootDirectory +  NamedEntriesArray[i].NameOffset);
			if (0 == wcscmp(UnicodeName->Buffer, Name))
			{
				*Entry = &NamedEntriesArray[i];
				return STATUS_SUCCESS;
			}
		}
	}

	return STATUS_RESOURCE_NAME_NOT_FOUND;
}

NTSTATUS PhGetMappedImageResourceDirectoryTypedEntry(
	_In_ PPH_MAPPED_IMAGE MappedImage,
	_In_ PIMAGE_RESOURCE_DIRECTORY RootDirectory,
	_In_opt_ ULONG TypeId,
	_Out_ PIMAGE_RESOURCE_DIRECTORY_ENTRY *Entry
)
{
	
	// Typed entries are always after named entries
	IMAGE_RESOURCE_DIRECTORY_ENTRY *EntriesArray = (IMAGE_RESOURCE_DIRECTORY_ENTRY*) ((ULONG_PTR)RootDirectory + sizeof(IMAGE_RESOURCE_DIRECTORY));
	IMAGE_RESOURCE_DIRECTORY_ENTRY *TypedEntriesArray = EntriesArray + RootDirectory->NumberOfNamedEntries;

	__try
	{
		//	PhpMappedImageProbe(&m_PvMappedImage, resourceRootDir, ProbeLength);
		PhProbeAddress(TypedEntriesArray, sizeof(IMAGE_RESOURCE_DIRECTORY_ENTRY) * RootDirectory->NumberOfIdEntries, MappedImage->ViewBase, MappedImage->Size, 1);
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		return GetExceptionCode();
	}

	for (int i = 0; i < RootDirectory->NumberOfIdEntries; i++)
	{
		if (TypedEntriesArray[i].Id == TypeId)
		{
			*Entry = &TypedEntriesArray[i];
			return STATUS_SUCCESS;
		}
	}

	return STATUS_RESOURCE_TYPE_NOT_FOUND;
}


NTSTATUS PhGetMappedImageResourceDirectoryEntry(
	_In_ PPH_MAPPED_IMAGE MappedImage,
	_In_ PIMAGE_RESOURCE_DIRECTORY RootDirectory,
	_In_opt_ ULONG TypeId,
	_In_opt_ LPTSTR Name,
	_Out_ PIMAGE_RESOURCE_DIRECTORY_ENTRY *Entry
)
{
	if (Name)
	{
		return PhGetMappedImageResourceDirectoryNamedEntry(
			MappedImage,
			RootDirectory,
			Name,
			Entry
		);
	}

	return PhGetMappedImageResourceDirectoryTypedEntry(
		MappedImage,
		RootDirectory,
		TypeId,
		Entry
	);
}

bool
UnmanagedPE::GetPeManifest(
	_Out_ PSTR *manifest,
	_Out_ INT  *manfestLen 
)
{
	NTSTATUS status;
	PIMAGE_RESOURCE_DIRECTORY ResourceRootDir;
	PIMAGE_RESOURCE_DIRECTORY_ENTRY ManifestDirEntry;
	PIMAGE_RESOURCE_DIRECTORY_ENTRY ManifestEntry;

	if (!m_bImageLoaded)
		return false;

	if (!NT_SUCCESS(status = PhGetMappedImageResourceRoot(&m_PvMappedImage, &ResourceRootDir)))
		return false;

	

	// look for RT_MANIFEST type entry
	WORD ManifestTypeId = (WORD) (ULONG_PTR) RT_MANIFEST;
	if (!NT_SUCCESS(status = PhGetMappedImageResourceDirectoryEntry(
		&m_PvMappedImage,
		ResourceRootDir,
		ManifestTypeId,
		NULL,
		&ManifestDirEntry
	)))
	{
		return false;
	}

	// RT_MANIFEST is a directory entry with only one LANG_ID entry 
	if (!ManifestDirEntry->DataIsDirectory)
		return false;

	ULONG FirstResourceIndex = 1;
	PIMAGE_RESOURCE_DIRECTORY ManifestDir = (PIMAGE_RESOURCE_DIRECTORY)((ULONG_PTR)ResourceRootDir + ManifestDirEntry->OffsetToDirectory);
	while (!NT_SUCCESS(status = PhGetMappedImageResourceDirectoryEntry(
		&m_PvMappedImage,
		ManifestDir,
		FirstResourceIndex,
		NULL,
		&ManifestEntry
	)))
	{
		FirstResourceIndex++;

		if (FirstResourceIndex == 65535) // we need to stop at some point in order to avoid an infinite loop
			return false;
	}

	if (!ManifestEntry->DataIsDirectory)
		return false;

	PIMAGE_RESOURCE_DIRECTORY SubManifestDir = (PIMAGE_RESOURCE_DIRECTORY)((ULONG_PTR)ResourceRootDir + ManifestEntry->OffsetToDirectory);
	if (!NT_SUCCESS(status = PhGetMappedImageResourceDirectoryEntry(
		&m_PvMappedImage,
		SubManifestDir,
		0x409, // EN-US LANG ID
		NULL,
		&ManifestEntry
	)))
	{
		return false;
	}

	if (ManifestEntry->DataIsDirectory)
		return false;

	
	/*PULONG ManifestDataPtr  = (PULONG) PhMappedImageRvaToVa(
		&m_PvMappedImage,
		ManifestEntry->OffsetToData,
		NULL
	);*/

	
	PULONG ManifestDataPtr = (PULONG)((ULONG_PTR)ResourceRootDir + ManifestEntry->OffsetToData);

	__try
	{
		//	PhpMappedImageProbe(&m_PvMappedImage, ManifestData, sizeof(IMAGE_RESOURCE_DATA_ENTRY) );
		PhProbeAddress(ManifestDataPtr, sizeof(IMAGE_RESOURCE_DATA_ENTRY) , m_PvMappedImage.ViewBase, m_PvMappedImage.Size, 1);
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		return false;
	}


	// Manifest entry is utf-8 only
	PIMAGE_RESOURCE_DATA_ENTRY ManifestData = (PIMAGE_RESOURCE_DATA_ENTRY) ManifestDataPtr;
	
	*manifest = (char*) PhMappedImageRvaToVa(&m_PvMappedImage, ManifestData->OffsetToData, NULL);
	*manfestLen = ManifestData->Size;

	return true;
}