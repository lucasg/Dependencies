// Native.h
#pragma once

#include <UnmanagedPh.h>
#include <stdint.h>
#using <System.dll>

namespace System {
namespace ClrPh {

	// Partial rewrite of System.IO.File in order to circumvent the wow64 system folder redirection
	// that is not properly handled in .Net Core.
	public ref class NativeFile {
    public:

        // @return true if the path actually points to a file on the disk
        static bool Exists(_In_ String^ Path);

        // Copy a filename to a new location
        static void Copy(_In_ String^ sourceFileName, _In_ String^ destFileName);

        // Hash the first FileSize bytes of a file, using SHA256.
        static String^ GetPartialHashFile(_In_ String^ Path, _In_ size_t FileSize);

	private:

		// Hash buffer using bcrypt library
		static bool HashBuffer(_In_ uint8_t *Buffer, _In_ size_t BufferSize, _In_ wchar_t *HASH_ALGORITHM, _Outptr_ void **Hash, _Out_ size_t *HashSize);
		
		// @return a hex string representing the buffer values, kinda like Python's binascii.hexlify 
		static String^ GetHexString(_In_ uint8_t *Buffer, _In_ size_t BufferSize);
			
		// Ignore system32 folder redirection since we may analyzing 64-bit binaries on a 32-bit Dependencies
		static bool DisableWow64FsRedirection();

		// revert redirection since it may have unpredictible results further down the line
		static bool RevertWow64FsRedirection();
	};

} /* namespace ClrPh */
} /* namespace System */
