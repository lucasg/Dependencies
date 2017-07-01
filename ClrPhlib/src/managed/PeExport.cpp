#include <ClrPhlib.h>
#include <UnmanagedPh.h>


using namespace System;
using namespace ClrPh;

PeExport::PeExport(
	_In_ const UnmanagedPE &refPe,
	_In_ size_t Index
)
{
	PH_MAPPED_IMAGE_EXPORT_ENTRY exportEntry;
	PH_MAPPED_IMAGE_EXPORT_FUNCTION exportFunction;

	if (
		NT_SUCCESS(PhGetMappedImageExportEntry((PPH_MAPPED_IMAGE_EXPORTS)&refPe.m_PvExports, (ULONG) Index, &exportEntry)) &&
		NT_SUCCESS(PhGetMappedImageExportFunction((PPH_MAPPED_IMAGE_EXPORTS)&refPe.m_PvExports, NULL, exportEntry.Ordinal, &exportFunction))
		)
	{
		Ordinal = exportEntry.Ordinal;
		ExportByOrdinal = (exportEntry.Name == nullptr);
		Name = gcnew String(exportEntry.Name);
		ForwardedName = gcnew String(exportFunction.ForwardedName);
		
		if (exportEntry.Name == nullptr)
			VirtualAddress = (Int64)exportFunction.Function;

		VirtualAddress = (Int64) exportFunction.Function;
	}

	
}

PeExport::PeExport(
	_In_ const PeExport ^ other
)
{
	this->Ordinal = Ordinal;
	this->ExportByOrdinal = ExportByOrdinal;
	this->Name = String::Copy(other->Name);
	this->ForwardedName = String::Copy(other->ForwardedName);
	this->VirtualAddress = other->VirtualAddress;
}

PeExport::~PeExport()
{

}