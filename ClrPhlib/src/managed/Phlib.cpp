#include <ClrPhlib.h>
#include <UnmanagedPh.h>


using namespace System;
using namespace ClrPh;

static bool bInitializedPhLib = false;

bool Phlib::InitializePhLib()
{
	if (!bInitializedPhLib)
	{
		bInitializedPhLib = NT_SUCCESS(PhInitializePhLibEx(0, 0, 0));
	}

	return bInitializedPhLib;
}