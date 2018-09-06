// Native.h
#pragma once

#include <UnmanagedPh.h>
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

	};

} /* namespace ClrPh */
} /* namespace System */
