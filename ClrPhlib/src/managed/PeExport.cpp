#include <ClrPhlib.h>
#include <UnmanagedPh.h>


using namespace Dependencies;
using namespace ClrPh;
PeExport::PeExport(
)
{
}

PeExport^ PeExport::FromMapimg (
	_In_ const UnmanagedPE &refPe,
	_In_ size_t Index
)
{
	PH_MAPPED_IMAGE_EXPORT_ENTRY exportEntry;
	PH_MAPPED_IMAGE_EXPORT_FUNCTION exportFunction;

	PeExport^ exp = nullptr;

	if (
		NT_SUCCESS(PhGetMappedImageExportEntry((PPH_MAPPED_IMAGE_EXPORTS)&refPe.m_PvExports, (ULONG) Index, &exportEntry)) &&
		NT_SUCCESS(PhGetMappedImageExportFunction((PPH_MAPPED_IMAGE_EXPORTS)&refPe.m_PvExports, NULL, exportEntry.Ordinal, &exportFunction))
		)
	{
		exp = gcnew PeExport();

		exp->Ordinal = exportEntry.Ordinal;
		exp->ExportByOrdinal = (exportEntry.Name == nullptr);
		exp->Name = gcnew String(exportEntry.Name);
		exp->ForwardedName = gcnew String(exportFunction.ForwardedName);
		
		if (exportEntry.Name == nullptr)
			exp->VirtualAddress = (Int64)exportFunction.Function;

		exp->VirtualAddress = (Int64) exportFunction.Function;

	}

	return exp;
}

PeExport::PeExport(
	_In_ const PeExport ^ other
)
{
	this->Ordinal = Ordinal;
	this->ExportByOrdinal = ExportByOrdinal;
	this->Name = gcnew String(other->Name);
	this->ForwardedName = gcnew String(other->ForwardedName);
	this->VirtualAddress = other->VirtualAddress;
}

PeExport::~PeExport()
{

}