#ifndef _PH_PH_H
#define _PH_PH_H

#pragma once

// /clr force /stdcall call prototype
// There are several __fastcall function in queuedlock and phbasesup.
#pragma warning( push )
#pragma warning(disable: 4561)

#include <phbase.h>
#include <phnative.h>
#include <phnativeinl.h>
#include <phutil.h>

#pragma warning (pop)

#endif
