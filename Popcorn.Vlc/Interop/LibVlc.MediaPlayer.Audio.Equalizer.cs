using System;
using System.Runtime.InteropServices;

namespace Popcorn.Vlc.Interop.MediaPlayer
{
    [LibVlcFunction("libvlc_audio_equalizer_get_preset_count", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint GetEqualizerPresetCount();
    
    [LibVlcFunction("libvlc_audio_equalizer_get_preset_name", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetEqualizerPresetName(uint index);
    
    [LibVlcFunction("libvlc_audio_equalizer_get_band_count", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint GetEqualizerBandCount();
    
    [LibVlcFunction("libvlc_audio_equalizer_get_band_frequency", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate float GetEqualizerBandFrequency(uint index);
    
    [LibVlcFunction("libvlc_audio_equalizer_new", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr CreateEqualizer();
    
    [LibVlcFunction("libvlc_audio_equalizer_new_from_preset", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr CreateEqualizerFromPreset(uint index);
    
    [LibVlcFunction("libvlc_audio_equalizer_release", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr ReleaseEqualizer(IntPtr equalizer);
    
    [LibVlcFunction("libvlc_audio_equalizer_set_preamp", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetEqualizerPreamp(IntPtr equalizer, float preamp);
    
    [LibVlcFunction("libvlc_audio_equalizer_get_preamp", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate float GetEqualizerPreamp(IntPtr equalizer);
    
    [LibVlcFunction("libvlc_audio_equalizer_set_amp_at_index", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetEqualizerAmplification(IntPtr equalizer, float preamp, uint band);
    
    [LibVlcFunction("libvlc_audio_equalizer_get_amp_at_index", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate float GetEqualizerAmplification(IntPtr equalizer, uint band);
    
    [LibVlcFunction("libvlc_media_player_set_equalizer", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetEqualizer(IntPtr meidaPlayer, IntPtr equalizer);
}