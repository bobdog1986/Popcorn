using System;
using System.Runtime.InteropServices;

namespace Popcorn.Vlc.Interop.VLM
{
    [LibVlcFunction("libvlc_vlm_release")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReleaseVlmInstance(IntPtr instance);
    
    [LibVlcFunction("libvlc_vlm_add_broadcast")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int NewBroadCastInput(
        IntPtr instance, IntPtr broadcastName, IntPtr inputMRL, IntPtr outputMRl, int options, IntPtr IntPtrOptions,
        int boolNewBorodcast, int ifLoopBroadcast);
    
    [LibVlcFunction("libvlc_vlm_add_vod")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int NewVodInput(
        IntPtr instance, IntPtr mediaWork, IntPtr inputMRL, int numberOptions, IntPtr addOptions, int boolNewVod,
        IntPtr vodMuxer);
    
    [LibVlcFunction("libvlc_vlm_del_media")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int DelBoroadcastOrOvd(IntPtr instance, IntPtr delBroadcastName);
    
    [LibVlcFunction("libvlc_vlm_set_enabled")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MediaSwitch(IntPtr instance, IntPtr mediaWork, int boolNewBorodcast);
    
    [LibVlcFunction("libvlc_vlm_set_output")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetMediaOutput(IntPtr instance, IntPtr mediaWork, IntPtr outputMRl);
    
    [LibVlcFunction("libvlc_vlm_set_input")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetMediaInput(IntPtr instance, IntPtr mediaWork, IntPtr inputMRl);
    
    [LibVlcFunction("libvlc_vlm_add_input")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int AddMediaInput(IntPtr instance, IntPtr mediaWork, IntPtr inputMRl);
    
    [LibVlcFunction("libvlc_vlm_set_loop")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetMediaLoop(IntPtr instance, IntPtr mediaWork, int newStatus);
    
    [LibVlcFunction("libvlc_vlm_set_mux")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetVodMuxer(IntPtr instance, IntPtr mediaWork, IntPtr newMuxer);
    
    [LibVlcFunction("libvlc_vlm_change_media")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int EditMediaParas(
        IntPtr instance, IntPtr newBroadcastName, IntPtr inPutMRL, IntPtr outPutMRL, int numberOptains,
        IntPtr addOptains, int boolNewBroadcast, int ifLoopBroadcast);
    
    [LibVlcFunction("libvlc_vlm_play_media")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int PlayNamedBroadcast(IntPtr instance, IntPtr mediaName);
    
    [LibVlcFunction("libvlc_vlm_stop_media")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int StopNamedBroadcast(IntPtr instance, IntPtr mediaName);
    
    [LibVlcFunction("libvlc_vlm_pause_media")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int PauseNamedBroadcast(IntPtr instance, IntPtr mediaName);
    
    [LibVlcFunction("libvlc_vlm_seek_media")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SeekInNamedBroadcast(IntPtr instance, IntPtr mediaName, float seekPercent);
    
    [LibVlcFunction("libvlc_vlm_show_media")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr ReturnJsonMessage(IntPtr instance, IntPtr namedMediaName);
    
    [LibVlcFunction("libvlc_vlm_get_media_instance_position")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate float GetMediaPosition(IntPtr instance, IntPtr mediaName, int id);
    
    [LibVlcFunction("libvlc_vlm_get_media_instance_time")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetMediaTime(IntPtr instance, IntPtr mediaName, int id);
    
    [LibVlcFunction("libvlc_vlm_get_media_instance_length")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetMediaLength(IntPtr instance, IntPtr mediaName, int id);
    
    [LibVlcFunction("libvlc_vlm_get_media_instance_rate")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetMediaBackRate(IntPtr instance, IntPtr mediaName, int id);
    
    [LibVlcFunction("libvlc_vlm_get_event_manager")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetMediaEventManager(IntPtr instance);
}