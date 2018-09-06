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

#ifdef __cplusplus
extern "C" {
#endif

PAPI_SET_NAMESPACE GetApiSetNamespace()
{
	ULONG	ReturnLength;
	PROCESS_BASIC_INFORMATION ProcessInformation;
	PAPI_SET_NAMESPACE apiSetMap = NULL;

	//	Retrieve PEB address
	if (!NT_SUCCESS(NtQueryInformationProcess(
		GetCurrentProcess(),
		ProcessBasicInformation,
		&ProcessInformation,
		sizeof(PROCESS_BASIC_INFORMATION),
		&ReturnLength
	)))
	{
		return NULL;
	}

	//	Parsing PEB structure and locating api set map
	PPEB peb = static_cast<PPEB>(ProcessInformation.PebBaseAddress);
	apiSetMap =  static_cast<PAPI_SET_NAMESPACE>(peb->ApiSetMap);

	return apiSetMap;
}

#ifdef __cplusplus
}
#endif

ApiSetSchema^ Phlib::GetApiSetSchemaV2(ULONG_PTR ApiSetMapBaseAddress, PAPI_SET_NAMESPACE_V2 ApiSetMap)
{
	ApiSetSchema^ ApiSets = gcnew ApiSetSchema();
	for (ULONG i=0; i < ApiSetMap->Count; i++)
	{
		auto ApiSetEntry = ApiSetMap->Array[i];

		// Retrieve api min-win contract name
		PWCHAR ApiSetEntryNameBuffer = reinterpret_cast<PWCHAR>(ApiSetMapBaseAddress + ApiSetEntry.NameOffset);
		String^ ApiSetEntryName = gcnew String(ApiSetEntryNameBuffer, 0, ApiSetEntry.NameLength/sizeof(WCHAR));

		// Strip the .dll extension and the last number (which is probably a build counter)
		String^ ApiSetEntryHashKey = ApiSetEntryName->Substring(0, ApiSetEntryName->LastIndexOf("-"));

		// Retrieve dlls names implementing the contract
		ApiSetTarget^ ApiSetEntryTargets = gcnew ApiSetTarget();
		PAPI_SET_VALUE_ENTRY_V2 ApiSetValueEntry = reinterpret_cast<PAPI_SET_VALUE_ENTRY_V2>(ApiSetMapBaseAddress + ApiSetEntry.DataOffset);
		for (ULONG j = 0; j < 2*(ApiSetValueEntry->NumberOfRedirections); j++) {
			auto Redirection = ApiSetValueEntry->Redirections[j];

			if (Redirection.NameLength) {
				PWCHAR ApiSetEntryTargetBuffer = reinterpret_cast<PWCHAR>(ApiSetMapBaseAddress + Redirection.NameOffset);
				String ^HostDllName = gcnew String(ApiSetEntryTargetBuffer, 0, Redirection.NameLength / sizeof(WCHAR));

				if (!ApiSetEntryTargets->Contains(HostDllName)) {
					ApiSetEntryTargets->Add(HostDllName);
				}
			}
		}

		ApiSets->Add(ApiSetEntryHashKey, ApiSetEntryTargets);
	}

	return ApiSets;
}

// TODO: Support ApiSet V4 (Win8.1)
ApiSetSchema^ Phlib::GetApiSetSchemaV4(ULONG_PTR ApiSetMapBaseAddress, PAPI_SET_NAMESPACE_V4 ApiSetMap)
{
	return gcnew ApiSetSchema();
}

ApiSetSchema^ Phlib::GetApiSetSchemaV6(ULONG_PTR ApiSetMapBaseAddress, PAPI_SET_NAMESPACE_V6 ApiSetMap)
{
	ApiSetSchema^ ApiSets = gcnew ApiSetSchema();

	auto ApiSetEntryIterator = reinterpret_cast<PAPI_SET_NAMESPACE_ENTRY_V6>((ApiSetMap->EntryOffset + ApiSetMapBaseAddress));
	for (ULONG i = 0; i < ApiSetMap->Count; i++) {

		// Retrieve api min-win contract name
		PWCHAR ApiSetEntryNameBuffer = reinterpret_cast<PWCHAR>(ApiSetMapBaseAddress + ApiSetEntryIterator->NameOffset);
		String^ ApiSetEntryName = gcnew String(ApiSetEntryNameBuffer, 0, ApiSetEntryIterator->NameLength/sizeof(WCHAR));

		// Strip the .dll extension and the last number (which is probably a build counter)
		String^ ApiSetEntryHashKey = ApiSetEntryName->Substring(0, ApiSetEntryName->LastIndexOf("-"));

		ApiSetTarget^ ApiSetEntryTargets = gcnew ApiSetTarget();

		// Iterate over all the host dll for this contract
		auto valueEntry = reinterpret_cast<PAPI_SET_VALUE_ENTRY_V6>(ApiSetMapBaseAddress + ApiSetEntryIterator->ValueOffset);
		for (ULONG j = 0; j < ApiSetEntryIterator->ValueCount; j++) {
			
			// Retrieve dll name implementing the contract
			PWCHAR ApiSetEntryTargetBuffer = reinterpret_cast<PWCHAR>(ApiSetMapBaseAddress + valueEntry->ValueOffset);
			ApiSetEntryTargets->Add(gcnew String(ApiSetEntryTargetBuffer, 0, valueEntry->ValueLength / sizeof(WCHAR)));


			// If there's an alias...
			if (valueEntry->NameLength != 0) {
				PWCHAR ApiSetEntryAliasBuffer = reinterpret_cast<PWCHAR>(ApiSetMapBaseAddress + valueEntry->NameOffset);
				ApiSetEntryTargets->Add(gcnew String(ApiSetEntryAliasBuffer, 0, valueEntry->NameLength / sizeof(WCHAR)));
			}

			valueEntry++;
		}


		ApiSets->Add(ApiSetEntryHashKey, ApiSetEntryTargets);
		ApiSetEntryIterator++;
	}

	return ApiSets;
}

ApiSetSchema^ Phlib::GetApiSetSchema()
{
	// Api set schema resolution adapted from https://github.com/zodiacon/WindowsInternals/blob/master/APISetMap/APISetMap.cpp
	// References :
	// 		* Windows Internals v7
	// 		* @aionescu's slides on "Hooking Nirvana" (RECON 2015)
	//		* Quarkslab blog posts : 
	// 				https://blog.quarkslab.com/runtime-dll-name-resolution-apisetschema-part-i.html
	// 				https://blog.quarkslab.com/runtime-dll-name-resolution-apisetschema-part-ii.html
	PAPI_SET_NAMESPACE apiSetMap = GetApiSetNamespace();
	auto apiSetMapAsNumber = reinterpret_cast<ULONG_PTR>(apiSetMap);

	// Check the returned api namespace is correct
	if (!apiSetMap) {
		return gcnew ApiSetSchema();
	}


	switch (apiSetMap->Version) 
	{
		case 2: // Win7
			return GetApiSetSchemaV2(apiSetMapAsNumber, &apiSetMap->ApiSetNameSpaceV2);

		case 4: // Win8.1
			return GetApiSetSchemaV4(apiSetMapAsNumber, &apiSetMap->ApiSetNameSpaceV4);

		case 6: // Win10
			return GetApiSetSchemaV6(apiSetMapAsNumber, &apiSetMap->ApiSetNameSpaceV6);

		default: // unsupported
			return gcnew ApiSetSchema();
	}
}