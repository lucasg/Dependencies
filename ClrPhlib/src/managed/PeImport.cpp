#include <ClrPhlib.h>
#include <UnmanagedPh.h>


using namespace Dependencies;
using namespace ClrPh;


PeImport::PeImport(
	_In_ const PPH_MAPPED_IMAGE_IMPORT_DLL importDll,
	_In_ size_t Index
)
{
	PH_MAPPED_IMAGE_IMPORT_ENTRY importEntry;

	if (NT_SUCCESS(PhGetMappedImageImportEntry((PPH_MAPPED_IMAGE_IMPORT_DLL) importDll, (ULONG)Index, &importEntry)))
	{
		this->Hint = importEntry.NameHint;
		this->Ordinal = importEntry.Ordinal;
		this->DelayImport = (importDll->Flags) & PH_MAPPED_IMAGE_DELAY_IMPORTS;
		this->Name = gcnew String(importEntry.Name);
		this->ModuleName = gcnew String(importDll->Name);
		this->ImportByOrdinal = (importEntry.Name == nullptr);
	}


}

PeImport::PeImport(
	_In_ const PeImport ^ other
)
{
	this->Hint = other->Hint;
	this->Ordinal = other->Ordinal;
	this->DelayImport = other->DelayImport;
	this->Name = gcnew String(other->Name);
	this->ModuleName = gcnew String(other->ModuleName);
	this->ImportByOrdinal = other->ImportByOrdinal;
}

PeImport::~PeImport()
{
}


PeImportDll::PeImportDll(
	_In_ const PPH_MAPPED_IMAGE_IMPORTS &PvMappedImports, 
	_In_ size_t ImportDllIndex
)
: ImportDll (new PH_MAPPED_IMAGE_IMPORT_DLL)
{
	ImportList = gcnew Collections::Generic::List<PeImport^>();

	if (!NT_SUCCESS(PhGetMappedImageImportDll(PvMappedImports, (ULONG)ImportDllIndex, ImportDll)))
	{
		Flags = 0;
		Name = gcnew String("## PeImportDll error: Invalid DllName ##");
		NumberOfEntries = 0;
		return;
	}

	Flags = ImportDll->Flags;
	Name = gcnew String(ImportDll->Name);
	NumberOfEntries = ImportDll->NumberOfEntries;

	for (size_t IndexImport = 0; IndexImport < (size_t) NumberOfEntries; IndexImport++)
	{
		ImportList->Add(gcnew PeImport(ImportDll, IndexImport));
	}
}

PeImportDll::~PeImportDll()
{
	delete ImportDll;
}

PeImportDll::!PeImportDll()
{
	delete ImportDll;
}

PeImportDll::PeImportDll(
	_In_ const PeImportDll ^ other
)
: ImportDll(new PH_MAPPED_IMAGE_IMPORT_DLL)
{
	ImportList = gcnew Collections::Generic::List<PeImport^>();

	memcpy(ImportDll, other->ImportDll, sizeof(PH_MAPPED_IMAGE_IMPORT_DLL));

	Flags = other->Flags;
	Name = gcnew String(other->Name);
	NumberOfEntries = other->NumberOfEntries;

	for (size_t IndexImport = 0; IndexImport < (size_t)NumberOfEntries; IndexImport++)
	{
		ImportList->Add(gcnew PeImport(other->ImportList[(int) IndexImport]));
	}

}


bool PeImportDll::IsDelayLoad()
{
	return this->Flags & PH_MAPPED_IMAGE_DELAY_IMPORTS;
}