#include <ClrPhlib.h>

using namespace Dependencies;

ClrPh::CLRPH_ARCH ClrPh::Phlib::GetClrPhArch()
{
#if _WIN64
	return ClrPh::CLRPH_ARCH::x64;
#else
	if (PhIsExecutingInWow64())
		return ClrPh::CLRPH_ARCH::WOW64;
	
	return ClrPh::CLRPH_ARCH::x86;
#endif // _WIN64
}