#pragma once
#include <ph.h>

#ifdef __cplusplus
extern "C" {
#endif

///////////////////////////////////////////////////////////////////////////////
// ApiSet v2

typedef struct _API_SET_VALUE_ENTRY_REDIRECTION_V2 {
	ULONG  NameOffset;
	USHORT NameLength;
	ULONG  ValueOffset;
	USHORT ValueLength;
} API_SET_VALUE_ENTRY_REDIRECTION_V2, *PAPI_SET_VALUE_ENTRY_REDIRECTION_V2;

typedef struct _API_SET_VALUE_ENTRY_V2 {
	ULONG NumberOfRedirections;
	API_SET_VALUE_ENTRY_REDIRECTION_V2 Redirections[ANYSIZE_ARRAY];
} API_SET_VALUE_ENTRY_V2, *PAPI_SET_VALUE_ENTRY_V2;

typedef struct _API_SET_NAMESPACE_ENTRY_V2 {
	ULONG NameOffset;
	ULONG NameLength;
	ULONG DataOffset; // ===> _API_SET_VALUE_ENTRY_V2
} API_SET_NAMESPACE_ENTRY_V2, *PAPI_SET_NAMESPACE_ENTRY_V2;

typedef struct _API_SET_NAMESPACE_V2 { 
	ULONG Version; 
	ULONG Count; 
	API_SET_NAMESPACE_ENTRY_V2 Array[ANYSIZE_ARRAY]; 
} API_SET_NAMESPACE_V2, *PAPI_SET_NAMESPACE_V2;

///////////////////////////////////////////////////////////////////////////////
// ApiSet v4

typedef struct _API_SET_VALUE_ENTRY_REDIRECTION_V4 {
	ULONG Flags;
	ULONG NameOffset;
	ULONG NameLength;
	ULONG ValueOffset;
	ULONG ValueLength;
} API_SET_VALUE_ENTRY_REDIRECTION_V4, *PAPI_SET_VALUE_ENTRY_REDIRECTION_V4;

typedef struct _API_SET_VALUE_ENTRY_V4 {
	ULONG Flags;
	ULONG NumberOfRedirections;
	API_SET_VALUE_ENTRY_REDIRECTION_V4 Redirections[ANYSIZE_ARRAY];
} API_SET_VALUE_ENTRY_V4, *PAPI_SET_VALUE_ENTRY_V4;

typedef struct _API_SET_NAMESPACE_ENTRY_V4 {
	ULONG Flags;
	ULONG NameOffset;
	ULONG NameLength;
	ULONG AliasOffset;
	ULONG AliasLength;
	ULONG DataOffset; // ===> _API_SET_VALUE_ENTRY_V4
} API_SET_NAMESPACE_ENTRY_V4, *PAPI_SET_NAMESPACE_ENTRY_V4;

typedef struct _API_SET_NAMESPACE_V4 { 
	ULONG Version; 
	ULONG Size; 
	ULONG Flags; 
	ULONG Count; 
	API_SET_NAMESPACE_ENTRY_V4 Array[ANYSIZE_ARRAY];
} API_SET_NAMESPACE_V4, *PAPI_SET_NAMESPACE_V4;

///////////////////////////////////////////////////////////////////////////////
// ApiSet v6

typedef struct _API_SET_HASH_ENTRY_V6 {
	ULONG Hash;
	ULONG Index;
} API_SET_HASH_ENTRY_V6, *PAPI_SET_HASH_ENTRY_V6;

typedef struct _API_SET_NAMESPACE_ENTRY_V6 {
	ULONG Flags;
	ULONG NameOffset;
	ULONG NameLength;
	ULONG HashedLength;
	ULONG ValueOffset;
	ULONG ValueCount;
} API_SET_NAMESPACE_ENTRY_V6, *PAPI_SET_NAMESPACE_ENTRY_V6;

typedef struct _API_SET_VALUE_ENTRY_V6 {
	ULONG Flags;
	ULONG NameOffset;
	ULONG NameLength;
	ULONG ValueOffset;
	ULONG ValueLength;
} API_SET_VALUE_ENTRY_V6, *PAPI_SET_VALUE_ENTRY_V6;

typedef struct _API_SET_NAMESPACE_V6 {
	ULONG Version;
	ULONG Size;
	ULONG Flags;
	ULONG Count;
	ULONG EntryOffset;
	ULONG HashOffset;
	ULONG HashFactor;
} API_SET_NAMESPACE_V6, *PAPI_SET_NAMESPACE_V6;

///////////////////////////////////////////////////////////////////////////////

typedef struct _API_SET_NAMESPACE {
	union
	{
 		ULONG Version;
		API_SET_NAMESPACE_V2 ApiSetNameSpaceV2;
		API_SET_NAMESPACE_V4 ApiSetNameSpaceV4;
		API_SET_NAMESPACE_V6 ApiSetNameSpaceV6;
	};
} API_SET_NAMESPACE, *PAPI_SET_NAMESPACE;


#ifdef __cplusplus
}
#endif