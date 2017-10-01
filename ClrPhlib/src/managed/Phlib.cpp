#include <ClrPhlib.h>
#include <UnmanagedPh.h>
#include <ApiSet.h>


#include <phnative.h>
#include <ntpsapi.h>


using namespace System;
using namespace ClrPh;

// Private : build the known dlls list
List<String^>^ BuildKnownDllList(_In_ bool Wow64Dlls);
static bool bInitializedPhLib = false;


bool Phlib::InitializePhLib()
{
	if (!bInitializedPhLib)
	{
		bInitializedPhLib = NT_SUCCESS(PhInitializePhLib());
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




ApiSetSchema^ Phlib::GetApiSetSchema()
{
	// Api set schema resolution adapted from https://github.com/zodiacon/WindowsInternals/blob/master/APISetMap/APISetMap.cpp
	ApiSetSchema^ ApiSets = gcnew ApiSetSchema();

	ULONG	ReturnLength;
	PROCESS_BASIC_INFORMATION ProcessInformation;

	//	Retrieve PEB address
	if (!NT_SUCCESS(NtQueryInformationProcess(
		GetCurrentProcess(),
		ProcessBasicInformation,
		&ProcessInformation,
		sizeof(PROCESS_BASIC_INFORMATION),
		&ReturnLength
	)))
	{
		return ApiSets;
	}

	//	Parsing PEB structure and locating api set map
	PPEB peb = static_cast<PPEB>(ProcessInformation.PebBaseAddress);
	PAPI_SET_NAMESPACE apiSetMap = static_cast<PAPI_SET_NAMESPACE>(peb->ApiSetMap);
	auto apiSetMapAsNumber = reinterpret_cast<ULONG_PTR>(apiSetMap);
	

	auto ApiSetEntryIterator = reinterpret_cast<PAPI_SET_NAMESPACE_ENTRY>((apiSetMap->EntryOffset + apiSetMapAsNumber));
	for (ULONG i = 0; i < apiSetMap->Count; i++) {
		//
		// Build a UNICODE_STRING for this contract
		//
		/*nameString.MaximumLength = static_cast<USHORT>(ApiSetEntryIterator->NameLength);
		nameString.Length = static_cast<USHORT>(ApiSetEntryIterator->NameLength);
		nameString.Buffer = reinterpret_cast<PWCHAR>(apiSetMapAsNumber + ApiSetEntryIterator->NameOffset);
		printf("%56wZ.dll -> {", &nameString);*/

		PWCHAR ApiSetEntryNameBuffer = reinterpret_cast<PWCHAR>(apiSetMapAsNumber + ApiSetEntryIterator->NameOffset);

		String^ ApiSetEntryName = gcnew String(ApiSetEntryNameBuffer, 0, ApiSetEntryIterator->NameLength/sizeof(WCHAR));
		ApiSetTarget^ ApiSetEntryTargets = gcnew ApiSetTarget();

		//
		// Iterate the values (i.e.: the hosts for this set)
		//
		auto valueEntry = reinterpret_cast<PAPI_SET_VALUE_ENTRY>(apiSetMapAsNumber + ApiSetEntryIterator->ValueOffset);
		for (ULONG j = 0; j < ApiSetEntryIterator->ValueCount; j++) {
			//
			// Build a UNICODE_STRING for this host
			//
			/*valueString.Buffer = reinterpret_cast<PWCHAR>(apiSetMapAsNumber + valueEntry->ValueOffset);
			valueString.MaximumLength = static_cast<USHORT>(valueEntry->ValueLength);
			valueString.Length = static_cast<USHORT>(valueEntry->ValueLength);
			printf("%wZ", &valueString);*/

			PWCHAR ApiSetEntryTargetBuffer = reinterpret_cast<PWCHAR>(apiSetMapAsNumber + valueEntry->ValueOffset);
			ApiSetEntryTargets->Add(gcnew String(ApiSetEntryTargetBuffer, 0, valueEntry->ValueLength / sizeof(WCHAR)));

			//
			// If there's more than one, add a comma
			//
			/*if ((j + 1) != ApiSetEntryIterator->ValueCount) {
				printf(", ");
			}*/

			//
			// If there's an alias...
			//
			if (valueEntry->NameLength != 0) {
				//
				// Build a UNICODE_STRING for it
				//
				/*nameString.MaximumLength = static_cast<USHORT>(valueEntry->NameLength);
				nameString.Length = static_cast<USHORT>(valueEntry->NameLength);
				nameString.Buffer = reinterpret_cast<PWCHAR>(apiSetMapAsNumber + valueEntry->NameOffset);
				printf(" [%wZ]", &nameString);*/
				PWCHAR ApiSetEntryAliasBuffer = reinterpret_cast<PWCHAR>(apiSetMapAsNumber + valueEntry->NameOffset);
				ApiSetEntryTargets->Add(gcnew String(ApiSetEntryAliasBuffer, 0, valueEntry->NameLength / sizeof(WCHAR)));
			}

			valueEntry++;
		}

		//
		// Next contract
		//
		ApiSets->Add(ApiSetEntryName, ApiSetEntryTargets);
		ApiSetEntryIterator++;
	}

	return ApiSets;
}
