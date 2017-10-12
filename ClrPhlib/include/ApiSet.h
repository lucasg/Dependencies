#pragma once
#include <ph.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct _API_SET_NAMESPACE {
	ULONG Version;
	ULONG Size;
	ULONG Flags;
	ULONG Count;
	ULONG EntryOffset;
	ULONG HashOffset;
	ULONG HashFactor;
} API_SET_NAMESPACE, *PAPI_SET_NAMESPACE;

typedef struct _API_SET_HASH_ENTRY {
	ULONG Hash;
	ULONG Index;
} API_SET_HASH_ENTRY, *PAPI_SET_HASH_ENTRY;

typedef struct _API_SET_NAMESPACE_ENTRY {
	ULONG Flags;
	ULONG NameOffset;
	ULONG NameLength;
	ULONG HashedLength;
	ULONG ValueOffset;
	ULONG ValueCount;
} API_SET_NAMESPACE_ENTRY, *PAPI_SET_NAMESPACE_ENTRY;

typedef struct _API_SET_VALUE_ENTRY {
	ULONG Flags;
	ULONG NameOffset;
	ULONG NameLength;
	ULONG ValueOffset;
	ULONG ValueLength;
} API_SET_VALUE_ENTRY, *PAPI_SET_VALUE_ENTRY;


/*NTSTATUS
NTAPI
ApiSetResolveToHost(
	_In_ PAPI_SET_NAMESPACE ApiSetSchema,
	_In_ PCUNICODE_STRING FileNameIn,
	_In_opt_ PCUNICODE_STRING ParentName,
	_Out_ PBOOLEAN Resolved,
	_Out_ PUNICODE_STRING HostBinary
);*/

#ifdef __cplusplus
}
#endif