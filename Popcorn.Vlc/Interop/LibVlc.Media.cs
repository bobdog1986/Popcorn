using System;
using System.Runtime.InteropServices;

namespace Popcorn.Vlc.Interop.Media
{
    [LibVlcFunction("libvlc_media_add_option")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void MediaAddOption(IntPtr media, IntPtr options);
    
    [LibVlcFunction("libvlc_media_add_option_flag")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void MediaAddOptionFlag(IntPtr media, IntPtr options, MediaOption flags);
    
    [LibVlcFunction("libvlc_media_duplicate")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr MediaDuplicate(IntPtr media);
    
    [LibVlcFunction("libvlc_media_event_manager")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetEventManager(IntPtr media);
    
    [LibVlcFunction("libvlc_media_get_codec_description", "3.0.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate IntPtr GetCodecDescription(TrackType type, int codec);
    
    [LibVlcFunction("libvlc_media_get_duration")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int64 GetDuration(IntPtr media);
    
    [LibVlcFunction("libvlc_media_get_meta")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate IntPtr GetMeta(IntPtr media, MetaDataType type);
    
    [LibVlcFunction("libvlc_media_get_mrl")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate IntPtr GetMrl(IntPtr media);

    [LibVlcFunction("libvlc_media_get_state")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate MediaState GetState(IntPtr media);
    
    [LibVlcFunction("libvlc_media_get_stats")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool GetStats(IntPtr media, ref MediaStats stats);
    
    [LibVlcFunction("libvlc_media_get_tracks_info")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetTracksInfo(IntPtr media, out IntPtr tracks);
    
    [LibVlcFunction("libvlc_media_get_user_data")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetUserData(IntPtr media);
    
    [LibVlcFunction("libvlc_media_is_parsed")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool IsParsed(IntPtr media);

    [LibVlcFunction("libvlc_media_new_as_node")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate IntPtr CreateMediaAsNewNode(IntPtr instance, IntPtr name);
    
    [LibVlcFunction("libvlc_media_new_fd", "1.1.5")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr CreateMediaFromFileDescriptor(IntPtr instance, int fileDescriptor);
    
    [LibVlcFunction("libvlc_media_new_location")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate IntPtr CreateMediaFromLocation(IntPtr instance, IntPtr url);
    
    [LibVlcFunction("libvlc_media_new_path")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public delegate IntPtr CreateMediaFromPath(IntPtr instance, IntPtr path);
    
    [LibVlcFunction("libvlc_media_parse")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ParseMedia(IntPtr media);
    
    [LibVlcFunction("libvlc_media_parse_async")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ParseMediaAsync(IntPtr media);
    
    [LibVlcFunction("libvlc_media_parse_with_options", "3.0.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int ParseMediaWithOptionAsync(IntPtr media, MediaParseFlag flag);
    
    [LibVlcFunction("libvlc_media_release")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReleaseMedia(IntPtr media);

    [LibVlcFunction("libvlc_media_retain")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void RetainMedia(IntPtr media);
    
    [LibVlcFunction("libvlc_media_save_meta")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool SaveMeta(IntPtr media);
    
    [LibVlcFunction("libvlc_media_set_meta")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void SetMeta(IntPtr media, MetaDataType type, IntPtr data);
    
    [LibVlcFunction("libvlc_media_set_user_data")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetUserData(IntPtr media, IntPtr userData);
    
    [LibVlcFunction("libvlc_media_subitems")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetSubitems(IntPtr media);
    
    [LibVlcFunction("libvlc_media_tracks_get", "2.1.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint GetTracks(IntPtr media, ref IntPtr tracks);
    
    [LibVlcFunction("libvlc_media_tracks_release", "2.1.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReleaseTracks(IntPtr tracks, uint count);

    [StructLayout(LayoutKind.Sequential)]
    public struct MediaStats
    {
        public int ReadBytes;
        public float InputBitrate;
        public int DemuxReadBytes;
        public float DemuxBitrate;
        public int DemuxCorrupted;
        public int DemuxDiscontinuity;
        public int DecodedVideo;
        public int DecodedAudio;
        public int DisplayedPictures;
        public int LostPictures;
        public int PlayedBbuffers;
        public int LostAbuffers;
        public int SentPackets;
        public int SentBytes;
        public float SendBitrate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MediaTrackInfo
    {
        public UInt32 Codec;
        public int Id;
        public TrackType Type;
        public int Profile;
        public int Level;
        
        public uint ChannelsOrHeight;
        
        public uint RateOrWidth;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AudioTrack
    {
        public uint Channels;
        
        public uint Rate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VideoTrack
    {
        public uint Height;
        
        public uint Width;

        public uint SarNum;
        public uint SarDen;
        public uint FrameRateNum;
        public uint FrameRateDen;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct SubtitleTrack
    {
        public IntPtr Encoding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MediaTrack
    {
        public uint Codec;
        public uint OriginalFourcc;
        public int Id;
        public TrackType Type;
        public int Profile;
        public int Level;
        
        public IntPtr Track;

        public uint Bitrate;

        public IntPtr Language;

        public IntPtr Description;
    }

    public enum TrackType
    {
        Unkown = -1,
        Audio = 0,
        Video = 1,
        Text = 2
    }

    public enum MediaState
    {
        NothingSpecial = 0,
        Opening,
        Buffering,
        Playing,
        Paused,
        Stopped,
        Ended,
        Error
    }

    public enum MediaOption
    {
        Trusted = 0x2,
        Unique = 0x100
    }

    [Flags]
    public enum MediaParseFlag
    {
        ParseLocal = 0x00,
        ParseNetwork = 0x01,
        FetchLocal = 0x02,
        FetchNetwork = 0x04
    }

    public enum MetaDataType
    {
        Title,
        Artist,
        Genre,
        Copyright,
        Album,
        TrackNumber,
        Description,
        Rating,
        Date,
        Setting,
        Url,
        Language,
        NowPlaying,
        Publisher,
        EncodedBy,
        ArtworkUrl,
        TrackID,
        TrackTotal,
        Director,
        Season,
        Episode,
        ShowName,
        Actors,
        AlbumArtist,
        DiscNumber
    }
}