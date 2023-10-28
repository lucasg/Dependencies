#include <ClrPhlib.h>
#include <UnmanagedPh.h>
#include <ApiSet.h>


#include <phnative.h>
#include <ntpsapi.h>


using namespace Dependencies;
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
	
	const PWCHAR KnownDllObjectName = (Wow64Dlls) ? (PWCHAR)L"\\KnownDlls32" : (PWCHAR)L"\\KnownDlls";

	name.Length = (USHORT) wcslen(KnownDllObjectName) * sizeof(wchar_t);
	name.MaximumLength = (USHORT) wcslen(KnownDllObjectName) * sizeof(wchar_t);
	name.Buffer = KnownDllObjectName;
	

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

struct ApiSetSchemaImpl
{
    static ApiSetSchema^ ParseApiSetSchema(API_SET_NAMESPACE const * apiSetMap);

private:
    // private implementation of ApiSet schema parsing
    static ApiSetSchema^ GetApiSetSchemaV2(API_SET_NAMESPACE_V2 const * map);
    static ApiSetSchema^ GetApiSetSchemaV4(API_SET_NAMESPACE_V4 const * map);
    static ApiSetSchema^ GetApiSetSchemaV6(API_SET_NAMESPACE_V6 const * map);
};

private ref class EmptyApiSetSchema sealed : ApiSetSchema
{
public:
    List<KeyValuePair<String^, ApiSetTarget^>>^ GetAll() override { return gcnew List<KeyValuePair<String^, ApiSetTarget^>>(); }
    ApiSetTarget^ Lookup(String^) override { return nullptr; }
};

private ref class V2V4ApiSetSchema sealed : ApiSetSchema
{
public:
    List<KeyValuePair<String^, ApiSetTarget^>>^ const All = gcnew List<KeyValuePair<String^, ApiSetTarget^>>();

    List<KeyValuePair<String^, ApiSetTarget^>>^ GetAll() override { return All; }
    ApiSetTarget^ Lookup(String^ name) override
    {
		// TODO : check if ext- is not present on win7 and 8.1
        if (!name->StartsWith("api-", System::StringComparison::CurrentCultureIgnoreCase))
            return nullptr;

		// Force lowercase name
		name = name->ToLower();

		// remove "api-" or "ext-" prefix
		name = name->Substring(4);

        // Note: The list is initially alphabetically sorted!!!
        auto min = 0;
        auto max = All->Count - 1;
		while (min <= max)
		{
			auto const cur = (min + max) / 2;
			auto pair = All[cur];

			if (name->StartsWith(pair.Key, System::StringComparison::CurrentCultureIgnoreCase))
				return pair.Value;

			if (String::CompareOrdinal(name, pair.Key) < 0)
				max = cur - 1;
			else
				min = cur + 1;
		}
        return nullptr;
    }
};

ApiSetSchema^ ApiSetSchemaImpl::GetApiSetSchemaV2(API_SET_NAMESPACE_V2 const * const map)
{
	auto const base = reinterpret_cast<ULONG_PTR>(map);
	auto const schema = gcnew V2V4ApiSetSchema();
	for (auto it = map->Array, eit = it + map->Count; it < eit; ++it)
	{
		// Retrieve DLLs names implementing the contract
		auto const targets = gcnew ApiSetTarget();
		auto const value_entry = reinterpret_cast<PAPI_SET_VALUE_ENTRY_V2>(base + it->DataOffset);
		for (auto it2 = value_entry->Redirections, eit2 = it2 + value_entry->NumberOfRedirections; it2 < eit2; ++it2)
		{
			auto const value_buffer = reinterpret_cast<PWCHAR>(base + it2->ValueOffset);
			auto const value = gcnew String(value_buffer, 0, it2->ValueLength / sizeof(WCHAR));
			targets->Add(value);
		}

		// Retrieve api min-win contract name
		auto const name_buffer = reinterpret_cast<PWCHAR>(base + it->NameOffset);
		auto const name = gcnew String(name_buffer, 0, it->NameLength / sizeof(WCHAR));

		// force storing lowercase variant for comparison
		auto const lower_name = name->ToLower();

		schema->All->Add(KeyValuePair<String^, ApiSetTarget^>(lower_name, targets));
	}
	return schema;
}

ApiSetSchema^ ApiSetSchemaImpl::GetApiSetSchemaV4(API_SET_NAMESPACE_V4 const * const map)
{
	auto const base = reinterpret_cast<ULONG_PTR>(map);
	auto const schema = gcnew V2V4ApiSetSchema();
	for (auto it = map->Array, eit = it + map->Count; it < eit; ++it)
	{
		// Retrieve DLLs names implementing the contract
		auto const targets = gcnew ApiSetTarget();
		auto const value_entry = reinterpret_cast<PAPI_SET_VALUE_ENTRY_V4>(base + it->DataOffset);
		for (auto it2 = value_entry->Redirections, eit2 = it2 + value_entry->NumberOfRedirections; it2 < eit2; ++it2)
		{
			auto const value_buffer = reinterpret_cast<PWCHAR>(base + it2->ValueOffset);
			auto const value = gcnew String(value_buffer, 0, it2->ValueLength / sizeof(WCHAR));
			targets->Add(value);
		}

		// Retrieve api min-win contract name
		auto const name_buffer = reinterpret_cast<PWCHAR>(base + it->NameOffset);
		auto const name = gcnew String(name_buffer, 0, it->NameLength / sizeof(WCHAR));

		// force storing lowercase variant for comparison
		auto const lower_name = name->ToLower();

		schema->All->Add(KeyValuePair<String^, ApiSetTarget^>(lower_name, targets));
	}
	return schema;
}

private ref class V6ApiSetSchema sealed : ApiSetSchema
{
public:
    List<KeyValuePair<String^, ApiSetTarget^>>^ const All = gcnew List<KeyValuePair<String^, ApiSetTarget^>>();
    List<KeyValuePair<String^, ApiSetTarget^>>^ HashedAll = gcnew List<KeyValuePair<String^, ApiSetTarget^>>();

    List<KeyValuePair<String^, ApiSetTarget^>>^ GetAll() override { return All; }
    ApiSetTarget^ Lookup(String^ name) override
    {
		// Force lowercase name
		name = name->ToLower();

        // Note: The list is initially alphabetically sorted!!!
        auto min = 0;
        auto max = HashedAll->Count - 1;
        while (min <= max)
        {
            auto const cur = (min + max) / 2;
            auto pair = HashedAll[cur];
            
			if (name->StartsWith(pair.Key, System::StringComparison::CurrentCultureIgnoreCase))
				return pair.Value;

            if (String::CompareOrdinal(name, pair.Key) < 0)
                max = cur - 1;
            else
                min = cur + 1;
        }
        return nullptr;
    }
};

ApiSetSchema^ ApiSetSchemaImpl::GetApiSetSchemaV6(API_SET_NAMESPACE_V6 const * const map)
{
	auto const base = reinterpret_cast<ULONG_PTR>(map);
	auto const schema = gcnew V6ApiSetSchema();
	for (auto it = reinterpret_cast<PAPI_SET_NAMESPACE_ENTRY_V6>(map->EntryOffset + base), eit = it + map->Count; it < eit; ++it)
	{
		// Iterate over all the host dll for this contract
		auto const targets = gcnew ApiSetTarget();
		for (auto it2 = static_cast<_API_SET_VALUE_ENTRY_V6*const>(reinterpret_cast<PAPI_SET_VALUE_ENTRY_V6>(base + it->ValueOffset)), eit2 = it2 + it->ValueCount; it2 < eit2; ++it2)
		{
			// Retrieve DLLs name implementing the contract
			auto const value_buffer = reinterpret_cast<PWCHAR>(base + it2->ValueOffset);
			auto const value = gcnew String(value_buffer, 0, it2->ValueLength / sizeof(WCHAR));
			targets->Add(value);
		}

		// Retrieve api min-win contract name
		auto const name_buffer = reinterpret_cast<PWCHAR>(base + it->NameOffset);
		auto const name = gcnew String(name_buffer, 0, it->NameLength / sizeof(WCHAR));
		auto const hash_name = gcnew String(name_buffer, 0, it->HashedLength / sizeof(WCHAR));

		// force storing lowercase variant for comparison
		auto const lower_name = name->ToLower();
		auto const lower_hash_name = hash_name->ToLower();

		schema->All->Add(KeyValuePair<String^, ApiSetTarget^>(lower_name, targets));
		schema->HashedAll->Add(KeyValuePair<String^, ApiSetTarget^>(lower_hash_name, targets));
	}
	return schema;
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
    return ApiSetSchemaImpl::ParseApiSetSchema(GetApiSetNamespace());
}

ApiSetSchema^ PE::GetApiSetSchema()
{
    PH_MAPPED_IMAGE mappedImage = m_Impl->m_PvMappedImage;
    for (auto n = 0u; n < mappedImage.NumberOfSections; ++n)
    {
        IMAGE_SECTION_HEADER const & section = mappedImage.Sections[n];
        if (strncmp(".apiset", reinterpret_cast<char const*>(section.Name), IMAGE_SIZEOF_SHORT_NAME) == 0)
            return ApiSetSchemaImpl::ParseApiSetSchema(reinterpret_cast<PAPI_SET_NAMESPACE>(PTR_ADD_OFFSET(mappedImage.ViewBase, section.PointerToRawData)));
    }
    return gcnew EmptyApiSetSchema();
}

ApiSetSchema^ ApiSetSchemaImpl::ParseApiSetSchema(API_SET_NAMESPACE const * const apiSetMap)
{
	// Check the returned api namespace is correct
	if (!apiSetMap)
		return gcnew EmptyApiSetSchema();

	switch (apiSetMap->Version) 
	{
		case 2: // Win7
			return GetApiSetSchemaV2(&apiSetMap->ApiSetNameSpaceV2);

		case 4: // Win8.1
			return GetApiSetSchemaV4(&apiSetMap->ApiSetNameSpaceV4);

		case 6: // Win10
			return GetApiSetSchemaV6(&apiSetMap->ApiSetNameSpaceV6);

		default: // unsupported
			return gcnew EmptyApiSetSchema();
	}
}