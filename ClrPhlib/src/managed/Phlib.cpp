#include <ClrPhlib.h>
#include <UnmanagedPh.h>

#include <phnative.h>


using namespace System;
using namespace ClrPh;

// Private : build the known dlls list
List<String^>^ BuildKnownDllList(_In_ bool Wow64Dlls);
static bool bInitializedPhLib = false;


bool Phlib::InitializePhLib()
{
	if (!bInitializedPhLib)
	{
		bInitializedPhLib = NT_SUCCESS(PhInitializePhLibEx(0, 0, 0));
	}

	KnownDll64List = BuildKnownDllList(false);
	KnownDll32List = BuildKnownDllList(true);

	return bInitializedPhLib;
}

BOOLEAN NTAPI PhEnumDirectoryObjectsCallback(
	_In_ PPH_STRINGREF Name,
    _In_ PPH_STRINGREF TypeName,
    _In_opt_ PVOID Context
)
{
	static PH_STRINGREF SectionTypeName = PH_STRINGREF_INIT(L"Section");
	List<String^>^ ReturnList = ((List<String^>^*) Context)[0];

	if (!PhCompareStringRef(&SectionTypeName, TypeName, TRUE)) {
		ReturnList->Add(gcnew String(Name->Buffer));
	}

	return TRUE;
}

List<String^>^ BuildKnownDllList(_In_ bool Wow64Dlls)
{
	List<String^>^ ReturnList = gcnew List<String^>();

	HANDLE KnownDllDir = INVALID_HANDLE_VALUE;
	OBJECT_ATTRIBUTES oa;
	UNICODE_STRING name;
	NTSTATUS status;


	name.Length = 20;
	name.MaximumLength = 20;
	name.Buffer = (Wow64Dlls) ? L"\\KnownDlls32" : L"\\KnownDlls";
	

	InitializeObjectAttributes(
		&oa,
		&name,
		0,
		NULL,
		NULL
	);

	status = NtOpenDirectoryObject(
		&KnownDllDir,
		DIRECTORY_QUERY,
		&oa
	);


	if (!NT_SUCCESS(status)) {
		return ReturnList;
	}

	status = PhEnumDirectoryObjects(
		KnownDllDir,
		(PPH_ENUM_DIRECTORY_OBJECTS) PhEnumDirectoryObjectsCallback,
		(PVOID) &ReturnList
	);

	if (!NT_SUCCESS(status)) {
		return ReturnList;
	}

	ReturnList->Sort();
	return ReturnList;
}

List<String^>^ Phlib::GetKnownDlls(_In_ bool Wow64Dlls)
{
	if (Wow64Dlls){
		return Phlib::KnownDll32List;
	}

	return Phlib::KnownDll64List;
}