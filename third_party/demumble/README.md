# demumble

`demumble` demangles both POSIX and Visual Studio symbols. It runs on both
POSIX and Windows.

    $ ./demumble _Z4funcPci
    func(char*, int)
    $ ./demumble "?Fx_i@@YAHP6AHH@Z@Z"
    int __cdecl Fx_i(int (__cdecl*)(int))
    
## Download

There are prebuilt x64 binaries for Linux, Mac (10.9+), and Windows on the
[releases page](https://github.com/nico/demumble/releases).

## But why

It has several nice features that c++filt lacks (and lacks many of c++filt's
features I never use).

Smart about underscores: C++ symbols have an additional leading underscore on
OS X. `operator new` is mangled as `_Znw` on Linux but `__Znw` on Mac. OS X's
c++filt automatically strips one leading underscore, but Linux's c++filt
doesn't. So if you want to demangle a Linux symbol on OS X, you need to pass
`-n` to tell it to not strip the underscore, and if you want to demangle an OS
X symbol on Linux you likewise need to pass `-_`. demumble just does the right
thing:

    $ c++filt _Znw
    _Znw
    $ c++filt __Znw
    operator new
    $ demumble _Znw
    operator new
    $ demumble __Znw
    operator new

Smart about filtering: Both c++filt and demumble can work as a stdin filter.
demumble only demangles function symbols (which never look like other words),
while c++filt defaults to demangling type names too, which are likely to look
like regular words. demumble does demangle types when they're passed as args:

    $ echo 'I like Pi and _Znw' | c++filt
    I like int* and _Znw
    $ echo 'I like Pi and _Znw' | demumble
    I like Pi and operator new
    $ c++filt Pi
    int*
    $ demumble Pi
    int*

Cross-platform: demumble runs on Windows. demumble can demangle Windows-style
symbols (also when running on non-Windows).

    $ demumble '??2@YAPEAX_K@Z'
    void * __ptr64 __cdecl operator new(unsigned __int64)
    $ c++filt '??2@YAPEAX_K@Z'
    ??2@YAPEAX_K@Z

demumble also has a flag to make it print only things that look like symbols.
For example, print demangled names of all functions defined in a bitcode file:

    $ grep '^define' bitcode-win.ll  | demumble -m | head -1
    unsigned int __cdecl v8::RoundUpToPowerOfTwo32(unsigned int)

## Build instructions

Use cmake to build: `cmake -G Ninja && ninja`

Run tests after building: `python demumble_test.py`

`cxa_demangle.cpp` needs more C++11 than Visual Studio 2013 supports, so
to build on Windows you need to use Visual Studion 2015 or use clang-cl
as C++ compiler like so:

    cmake -G Ninja -DCMAKE_CXX_COMPILER=path/to/llvm-build/bin/clang-cl.exe
