using System;
using System.Runtime.InteropServices;

namespace Popcorn.Vlc.Interop.MediaPlayer
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct TrackDescription
    {
        public int Id;

        public IntPtr Name;

        public IntPtr Next;
    }

    public struct Rectangle
    {
        public int Top;
        public int Left;
        public int Bottom;
        public int Right;
    }

    public enum VideoMarqueeOption
    {
        Enable = 0,
        Text,
        Color,
        Opacity,
        Position,
        Refresh,
        Size,
        Timeout,
        X,
        Y
    }

    public enum NavigateMode
    {
        Activate = 0,
        Up,
        Down,
        Left,
        Right
    }

    public enum Position
    {
        Disable = -1,
        Center,
        Left,
        Right,
        Top,
        TopLeft,
        TopRight,
        Bottom,
        BottomLeft,
        BottomRight
    }
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr VideoLockCallback(IntPtr opaque, ref IntPtr planes);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void VideoUnlockCallback(IntPtr opaque, IntPtr picture, ref IntPtr planes);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void VideoDisplayCallback(IntPtr opaque, IntPtr picture);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint VideoFormatCallback(
        ref IntPtr opaque, ref uint chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void VideoCleanupCallback(IntPtr opaque);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AudioPlayCallback(IntPtr opaque, IntPtr sample, uint count, Int64 pts);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AudioPauseCallback(IntPtr opaque, Int64 pts);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AudioResumeCallback(IntPtr opaque, Int64 pts);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AudioFlushCallback(IntPtr opaque, Int64 pts);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AudioDrainCallback(IntPtr opaque, Int64 pts);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int AudioSetupCallback(ref IntPtr opaque, IntPtr format, ref uint rate, ref uint channels);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AudioCleanupCallback(IntPtr opaque);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AudioSetVolumeCallback(IntPtr opaque, float volume, bool mute);
    
    [LibVlcFunction("libvlc_media_player_new")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr CreateMediaPlayer(IntPtr instance);
    
    [LibVlcFunction("libvlc_media_player_new_from_media")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr CreateMediaPlayerFromMedia(IntPtr media);
    
    [LibVlcFunction("libvlc_media_player_release")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReleaseMediaPlayer(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_retain")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void RetainMediaPlayer(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_set_media")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetMedia(IntPtr mediaPlayer, IntPtr media);
    
    [LibVlcFunction("libvlc_media_player_get_media")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetMedia(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_event_manager")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetEventManager(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_is_playing")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool IsPlaying(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_play")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int Play(IntPtr mediaPlayer);

    [LibVlcFunction("libvlc_media_player_set_pause")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetPause(IntPtr mediaPlayer, bool pause);
    
    [LibVlcFunction("libvlc_media_player_set_position")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetPosition(IntPtr mediaPlayer, float pos);
    
    [LibVlcFunction("libvlc_media_player_stop")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Stop(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_video_set_callbacks", "1.1.1")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetVideoCallback(
        IntPtr mediaPlayer, VideoLockCallback lockCallback, VideoUnlockCallback unlockCallback,
        VideoDisplayCallback displayCallback, IntPtr userData);
    
    [LibVlcFunction("libvlc_video_set_format", "1.1.1")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void SetVideoFormat(IntPtr mediaPlayer, IntPtr chroma, uint width, uint height, uint pitch);
    
    [LibVlcFunction("libvlc_video_set_format_callbacks", "2.0.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetVideoFormatCallback(
        IntPtr mediaPlayer, VideoFormatCallback formatCallback, VideoCleanupCallback cleanupCallback);
    
    [LibVlcFunction("libvlc_media_player_set_hwnd")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetHwnd(IntPtr mediaPlayer, IntPtr hwnd);
    
    [LibVlcFunction("libvlc_media_player_set_hwnd")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetHwnd(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_audio_set_callbacks", "2.0.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetAudioCallback(
        IntPtr mediaPlayer, AudioPlayCallback playCallback, AudioPauseCallback pauseCallback,
        AudioResumeCallback resumeCallback, AudioFlushCallback flushCallback, AudioDrainCallback drainCallback);
    
    [LibVlcFunction("libvlc_audio_set_format", "2.0.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetAudioFormat(IntPtr mediaPlayer, [MarshalAs(UnmanagedType.LPArray)] byte[] format, uint rate, uint channels);
    
    [LibVlcFunction("libvlc_audio_set_format_callbacks", "2.0.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetAudioFormatCallback(
        IntPtr mediaPlayer, AudioSetupCallback setupCallback, AudioCleanupCallback cheanupCallback);
    
    [LibVlcFunction("libvlc_audio_set_volume_callback", "2.0.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetAudioVolumeCallback(IntPtr mediaPlayer, AudioSetVolumeCallback volumeCallback);
    
    [LibVlcFunction("libvlc_media_player_get_length")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int64 GetLength(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_get_time")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int64 GetTime(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_set_time")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetTime(IntPtr mediaPlayer, Int64 time);
    
    [LibVlcFunction("libvlc_media_player_get_position")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate float GetPosition(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_set_chapter")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetChapter(IntPtr mediaPlayer, int chapter);
    
    [LibVlcFunction("libvlc_media_player_get_chapter")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetChapter(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_get_chapter_count")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetChapterCount(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_will_play")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CanPlay(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_get_chapter_count_for_title")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetTitleChapterCount(IntPtr mediaPlayer, int title);
    
    [LibVlcFunction("libvlc_media_player_set_title")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetTitle(IntPtr mediaPlayer, int title);
    
    [LibVlcFunction("libvlc_media_player_get_title")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetTitle(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_get_title_count")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetTitleCount(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_previous_chapter")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PreviousChapter(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_next_chapter")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void NextChapter(IntPtr mediaPlayer);

    [LibVlcFunction("libvlc_media_player_get_rate")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate float GetRate(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_set_rate")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetRate(IntPtr mediaPlayer, float rate);
    
    [LibVlcFunction("libvlc_media_player_get_state")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Media.MediaState GetState(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_get_fps")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate float GetFps(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_has_vout")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint HasVout(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_is_seekable")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool IsSeekable(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_can_pause")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CanPause(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_next_frame")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void NextFrame(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_media_player_navigate", "2.2.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Navigate(IntPtr mediaPlayer, NavigateMode navigate);
    
    [LibVlcFunction("libvlc_media_player_navigate", "2.1.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetVideoTitleDisplay(IntPtr mediaPlayer, Position pos, uint timeout);
    
    [LibVlcFunction("libvlc_track_description_list_release")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReleaseTrackDescription(IntPtr track);

    [LibVlcFunction("libvlc_video_get_spu")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetSubtitle(IntPtr mediaPlayer);
    
    [LibVlcFunction("libvlc_video_get_spu_count")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetSubtitleCount(IntPtr mediaPlayer);

    [LibVlcFunction("libvlc_video_get_spu_description")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetSubtitleDescription(IntPtr mediaPlayer);

    [LibVlcFunction("libvlc_video_set_spu")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetSubtitle(IntPtr mediaPlayer, int sub);

    [LibVlcFunction("libvlc_video_set_subtitle_file")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetSubtitleFile(IntPtr mediaPlayer, IntPtr subtiltieFile);

    [LibVlcFunction("libvlc_video_get_spu_delay", "2.0.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate long GetSubtitleDelay(IntPtr mediaPlayer);

    [LibVlcFunction("libvlc_video_set_spu_delay", "2.0.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetSubtitleDelay(IntPtr mediaPlayer,long delay);
}