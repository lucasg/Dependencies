#include <NativeFile.h>
#include <phconfig.h>
#include <vcclr.h> 
#include <bcrypt.h>

using namespace System;
using namespace ClrPh;
using namespace System::Text;

bool NativeFile::Exists(_In_ String^ Path)
{
    bool bFileExists = false;
    PVOID OldRedirectionValue = NULL;
    BOOLEAN isWow64 = PhIsExecutingInWow64();

    // Ignore system32 folder redirection since we may analyzing 64-bit binaries on a 32-bit Dependencies
    if (isWow64)
    {
        BOOL bWow64RedirectionDisabled = Wow64DisableWow64FsRedirection(
            &OldRedirectionValue
        );
    }

    pin_ptr<const wchar_t> RawPath = PtrToStringChars(Path);
    DWORD FileAttributes = GetFileAttributes(RawPath);

    bFileExists = (FileAttributes != INVALID_FILE_ATTRIBUTES && 
                 !(FileAttributes & FILE_ATTRIBUTE_DIRECTORY));

    if (isWow64)
    {
        Wow64RevertWow64FsRedirection(OldRedirectionValue);
    }

    return bFileExists;
}

void NativeFile::Copy(_In_ String^ sourceFileName, _In_ String^ destFileName)
{
    bool bFileExists = false;
    PVOID OldRedirectionValue = NULL;
    BOOLEAN isWow64 = PhIsExecutingInWow64();

    // Ignore system32 folder redirection since we may analyzing 64-bit binaries on a 32-bit Dependencies
    if (isWow64)
    {
        BOOL bWow64RedirectionDisabled = Wow64DisableWow64FsRedirection(
            &OldRedirectionValue
        );
    }

    pin_ptr<const wchar_t> RawSourceFilepath = PtrToStringChars(sourceFileName);
    pin_ptr<const wchar_t> RawDestFilepath = PtrToStringChars(destFileName);
    
    CopyFile(
        RawSourceFilepath,
        RawDestFilepath,
        false
    );

    if (isWow64)
    {
        Wow64RevertWow64FsRedirection(OldRedirectionValue);
    }

}

String^ NativeFile::GetPartialHashFile(_In_ String^ Path, _In_ size_t FileSize)
{
    PVOID hash = NULL;
    PVOID hashObject = NULL;
    PVOID fileBuffer = NULL;
    ULONG hashSize = 0;
    ULONG hashObjectSize = 0;
    ULONG FileSizeRead = 0;
	ULONG querySize;

    NTSTATUS status;
    BCRYPT_ALG_HANDLE hashAlgHandle = NULL;
    BCRYPT_HASH_HANDLE hashHandle = NULL;
	HANDLE fileHandle = INVALID_HANDLE_VALUE;

    PVOID OldRedirectionValue = NULL;
    BOOLEAN isWow64 = false;
    BOOLEAN success = false;

    String^ PartialHash;
    UTF8Encoding Utf8Decoder;

    if (!NT_SUCCESS(status = BCryptOpenAlgorithmProvider(&hashAlgHandle, BCRYPT_SHA256_ALGORITHM, NULL, 0)))
        goto CleanupExit;

    if (!NT_SUCCESS(status = BCryptGetProperty(hashAlgHandle, BCRYPT_OBJECT_LENGTH,
        (PUCHAR)&hashObjectSize, sizeof(ULONG), &querySize, 0)))
        goto CleanupExit;

    if (!NT_SUCCESS(status = BCryptGetProperty(hashAlgHandle, BCRYPT_HASH_LENGTH, (PUCHAR)&hashSize,
        sizeof(ULONG), &querySize, 0)))
        goto CleanupExit;

    if (!(hashObject = malloc(hashObjectSize)))
        goto CleanupExit;

    if (!(hash = malloc(hashSize)))
        goto CleanupExit;

    if (!(fileBuffer = malloc(FileSize)))
        goto CleanupExit;

    if (!NT_SUCCESS(status = BCryptCreateHash(
		hashAlgHandle, 
		&hashHandle, 
		(PUCHAR) hashObject, 
		hashObjectSize,
        NULL, 
		0, 
		0
	)))
        goto CleanupExit;

    isWow64 = PhIsExecutingInWow64();

    // Ignore system32 folder redirection since we may analyzing 64-bit binaries on a 32-bit Dependencies
    if (isWow64)
    {
        BOOL bWow64RedirectionDisabled = Wow64DisableWow64FsRedirection(
            &OldRedirectionValue
        );
    }

    pin_ptr<const wchar_t> RawPath = PtrToStringChars(Path);
    if (INVALID_HANDLE_VALUE == (fileHandle = CreateFile(
        RawPath, 
        FILE_GENERIC_READ, 
        FILE_SHARE_READ,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL
    )))
        goto CleanupExit;


    if (!ReadFile(
      fileHandle,
      fileBuffer,
      FileSize,
      &FileSizeRead,
      NULL
    ))
        goto CleanupExit;


    if (FileSizeRead != FileSize)
        goto CleanupExit;

    if (!NT_SUCCESS(status = BCryptHashData(hashHandle, (PUCHAR) fileBuffer, FileSizeRead, 0)))
        goto CleanupExit;

    if (!NT_SUCCESS(status = BCryptFinishHash(hashHandle, (PUCHAR) hash, hashSize, 0)))
        goto CleanupExit;

	// Convert to hex string
    array<unsigned char> ^hexBuffer = gcnew array<unsigned char>(2*hashSize);
    for (ULONG i = 0; i < hashSize; i++)
    {
        char hexNumber[2] = {0};
        sprintf(hexNumber, "%02X", ((unsigned char*)hash)[i]);

        hexBuffer[2*i] = hexNumber[0];
        hexBuffer[2*i + 1] = hexNumber[1];
    }

    PartialHash = Utf8Decoder.GetString(hexBuffer, 0, 2 * hashSize);
    success = TRUE;

CleanupExit:

    if (isWow64)
    {
        Wow64RevertWow64FsRedirection(OldRedirectionValue);
    }

    if (fileBuffer)
        free(fileBuffer);
    if (fileHandle)
        CloseHandle(fileHandle);
    
    if (hashHandle)
        BCryptDestroyHash(hashHandle);
    if (hashAlgHandle)
        BCryptCloseAlgorithmProvider(hashAlgHandle, 0);

    if (hash)
        free(hash);
    if (hashObject)
        free(hashObject);
    

    if (!success)
    {
        return gcnew String("");
    }

    return PartialHash;
}