using System;
using System.Runtime.InteropServices;

namespace Popcorn.Vlc.Interop.MediaPlayer
{
    [LibVlcFunction("libvlc_audio_toggle_mute")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ToggleMute(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_audio_get_mute")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetMute(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_audio_set_mute")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetMute(IntPtr mediaPlayer, int status);
    
    [LibVlcFunction("libvlc_audio_get_volume")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetVolume(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_audio_set_volume")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetVolume(IntPtr mediaPlayer, int volume);
    
    [LibVlcFunction("libvlc_audio_get_channel")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate AudioOutputChannel GetOutputChannel(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_audio_set_channel")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetOutputChannel(IntPtr mediaPlayer, AudioOutputChannel channel);
    
    [LibVlcFunction("libvlc_audio_get_track_count")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetAudioTrackCount(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_audio_get_track")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetAudioTrack(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_audio_set_track")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetAudioTrack(IntPtr mediaPlayer, int track);
    
    [LibVlcFunction("libvlc_audio_get_track_description")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetAudioTrackDescription(IntPtr mediaPlayer);

    public enum AudioOutputChannel
    {
        Error = -1,
        Stereo = 1,
        RStereo,
        Left,
        Right,
        Dolbys
    }
}