#include <NativeFile.h>
#include <ClrPhLib.h>
#include <phconfig.h>
#include <vcclr.h> 
#include <bcrypt.h>

using namespace Dependencies;
using namespace ClrPh;
using namespace System::Text;

static bool isCurrentProcessWow64 = CLRPH_ARCH::WOW64 == Phlib::GetClrPhArch();
static PVOID FsRedirectionValue = NULL;
#define WITH_WOW64_FS_REDIRECTION_DISABLED(action) do { \
    DisableWow64FsRedirection(); \
    action \
    RevertWow64FsRedirection(); \
} while (false)

bool NativeFile::DisableWow64FsRedirection()
{
	if (!isCurrentProcessWow64)
		return true;

	BOOL bWow64RedirectionDisabled = Wow64DisableWow64FsRedirection(
		&FsRedirectionValue
	);

	return (bWow64RedirectionDisabled == TRUE);
}

bool NativeFile::RevertWow64FsRedirection()
{
	if (!isCurrentProcessWow64)
		return true;

	BOOL bWow64RedirectionReverted = Wow64RevertWow64FsRedirection(
		FsRedirectionValue
	);

	return (bWow64RedirectionReverted == TRUE);
}


bool NativeFile::Exists(_In_ String^ Path)
{
    bool bFileExists = false;
	pin_ptr<const wchar_t> RawPath = PtrToStringChars(Path);


    WITH_WOW64_FS_REDIRECTION_DISABLED({
        
        DWORD FileAttributes = GetFileAttributes(RawPath);
        bFileExists = (FileAttributes != INVALID_FILE_ATTRIBUTES && 
                     !(FileAttributes & FILE_ATTRIBUTE_DIRECTORY));
    });

    return bFileExists;
}

void NativeFile::Copy(_In_ String^ sourceFileName, _In_ String^ destFileName)
{
    bool bFileExists = false;
    PVOID OldRedirectionValue = NULL;
    BOOLEAN isWow64 = PhIsExecutingInWow64();
	pin_ptr<const wchar_t> RawSourceFilepath = PtrToStringChars(sourceFileName);
	pin_ptr<const wchar_t> RawDestFilepath = PtrToStringChars(destFileName);

    WITH_WOW64_FS_REDIRECTION_DISABLED({
    
        CopyFile(
            RawSourceFilepath,
            RawDestFilepath,
            false
        );
    });
}

String^ NativeFile::GetPartialHashFile(_In_ String^ Path, _In_ size_t FileSize)
{
	bool read_success = false;
	bool success = false;
	void* hash = NULL;
	size_t hash_size = 0;

	HANDLE fileHandle = INVALID_HANDLE_VALUE;
	PVOID fileBuffer = NULL;
	ULONG FileSizeRead = 0;

    String^ PartialHash;
    pin_ptr<const wchar_t> RawPath = PtrToStringChars(Path);


	WITH_WOW64_FS_REDIRECTION_DISABLED({

		fileHandle = CreateFile(
			RawPath,
			FILE_GENERIC_READ,
			FILE_SHARE_READ,
			NULL,
			OPEN_EXISTING,
			FILE_ATTRIBUTE_NORMAL,
			NULL
		);
    });
    
    if ((INVALID_HANDLE_VALUE == fileHandle))
        goto CleanupExit;

	if (!(fileBuffer = malloc(FileSize)))
		goto CleanupExit;

    if (!ReadFile(
      fileHandle,
      fileBuffer,
      (DWORD) FileSize,
      &FileSizeRead,
      NULL
    ))
        goto CleanupExit;

    if (FileSizeRead != FileSize)
        goto CleanupExit;

	// hash it
	if (!HashBuffer((uint8_t *)fileBuffer, FileSize, BCRYPT_SHA256_ALGORITHM, &hash, &hash_size))
		goto CleanupExit;

	// Convert to hex string
    PartialHash = GetHexString((uint8_t *)hash, hash_size);
    success = TRUE;

CleanupExit:

    if (fileBuffer)
        free(fileBuffer);

    if (fileHandle)
        CloseHandle(fileHandle);
    
	if (hash)
		free(hash);

    if (!success)
    {
        return gcnew String("");
    }

    return PartialHash;
}


bool NativeFile::HashBuffer(_In_ uint8_t *Buffer, _In_ size_t BufferSize, _In_ wchar_t *HASH_ALGORITHM, _Outptr_ void **Hash, _Out_ size_t *HashSize)
{
	
	NTSTATUS status;
	BCRYPT_ALG_HANDLE hashAlgHandle = NULL;
	BCRYPT_HASH_HANDLE hashHandle = NULL;

	PVOID hash = NULL;
	PVOID hashObject = NULL;
	ULONG hashObjectSize = 0;
	ULONG hashSize = 0;
	ULONG querySize = 0;

	*Hash = NULL;
	*HashSize = 0;
	bool success = false;

	if (!NT_SUCCESS(status = BCryptOpenAlgorithmProvider(&hashAlgHandle, HASH_ALGORITHM, NULL, 0)))
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

	if (!NT_SUCCESS(status = BCryptCreateHash(
		hashAlgHandle,
		&hashHandle,
		(PUCHAR)hashObject,
		hashObjectSize,
		NULL,
		0,
		0
	)))
		goto CleanupExit;

	if (!NT_SUCCESS(status = BCryptHashData(hashHandle, (PUCHAR)Buffer, (ULONG)BufferSize, 0)))
		goto CleanupExit;

	if (!NT_SUCCESS(status = BCryptFinishHash(hashHandle, (PUCHAR)hash, hashSize, 0)))
		goto CleanupExit;

	*Hash = hash;
	*HashSize = hashSize;
	success = true;

CleanupExit:
	
	if (!success)
	{
		if (hash)
			free(hash);
	}

	if (hashHandle)
		BCryptDestroyHash(hashHandle);

	if (hashAlgHandle)
		BCryptCloseAlgorithmProvider(hashAlgHandle, 0);

	if (hashObject)
		free(hashObject);

	return success;
}

String^ NativeFile::GetHexString(_In_ uint8_t *Buffer, _In_ size_t BufferSize)
{
    ASCIIEncoding AsciiDecoder;
    array<unsigned char> ^hexBuffer = gcnew array<unsigned char>(2 * (int)BufferSize);

    for (ULONG i = 0; i < BufferSize; i++)
    {
        char hexNumber[2] = {0};
        sprintf(hexNumber, "%02X", ((unsigned char*)Buffer)[i]);

        hexBuffer[2*i] = hexNumber[0];
        hexBuffer[2*i + 1] = hexNumber[1];
    }

   return AsciiDecoder.GetString(hexBuffer, 0, 2 * (int)BufferSize);
}