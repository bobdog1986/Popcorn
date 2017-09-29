using System;
using System.Runtime.InteropServices;

namespace Popcorn.Vlc.Interop.Time
{
    [LibVlcFunction("libvlc_clock")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int64 Clock();
    
    [LibVlcFunction("libvlc_clock")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int64 Delay(Int64 timestamp);
}