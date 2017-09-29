



using System;
using System.Runtime.InteropServices;

namespace Popcorn.Vlc.Interop.Core
{
    [LibVlcFunction("libvlc_new")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate int AddInterface(IntPtr instance, IntPtr name);

    [LibVlcFunction("libvlc_audio_filter_list_get")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetAudioFilterList(IntPtr instance);
    
    [LibVlcFunction("libvlc_free")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Free(IntPtr pointer);
    
    [LibVlcFunction("libvlc_get_changeset")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate IntPtr GetChangeset();
    
    [LibVlcFunction("libvlc_get_compiler")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate IntPtr GetCompiler();
    
    [LibVlcFunction("libvlc_get_version")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate IntPtr GetVersion();
    
    [LibVlcFunction("libvlc_module_description_list_release")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReleaseLibVlcModuleDescription(IntPtr moduleDescription);
    
    [LibVlcFunction("libvlc_new")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr NewInstance(int argsCount, IntPtr argv);
    
    [LibVlcFunction("libvlc_release")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReleaseInstance(IntPtr instance);
    
    [LibVlcFunction("libvlc_retain")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void RetainInstance(IntPtr instance);
    
    [LibVlcFunction("libvlc_set_app_id", "2.1.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void SetAppId(IntPtr instance, IntPtr id, IntPtr version, IntPtr icon);
    
    [LibVlcFunction("libvlc_set_exit_handler")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetExitHandler(IntPtr instance, ExitHandler handler, IntPtr arg);

    public delegate void ExitHandler(IntPtr data);
    
    [LibVlcFunction("libvlc_set_user_agent", "2.1.0")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void SetUserAgent(IntPtr instance, IntPtr name, IntPtr http);
    
    [LibVlcFunction("libvlc_video_filter_list_get")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetVideoFilterList(IntPtr instance);
    
    [LibVlcFunction("libvlc_wait")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Wait(IntPtr instance);
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ModuleDescription
    {
        public IntPtr Name;

        public IntPtr ShortName;

        public IntPtr LongName;

        public IntPtr Help;

        public IntPtr Next;
    }

    namespace Error
    {
        [LibVlcFunction("libvlc_errmsg")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate IntPtr ErrorMessage();
        
        [LibVlcFunction("libvlc_clearerr")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CleanError();
    }

    namespace Events
    {
        [LibVlcFunction("libvlc_event_attach")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int EventAttach(IntPtr manager, EventTypes type, LibVlcEventCallBack callback, IntPtr userData);
        
        [LibVlcFunction("libvlc_event_detach")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void EventDetach(IntPtr manager, EventTypes type, LibVlcEventCallBack callback,
            IntPtr userData);
        
        [LibVlcFunction("libvlc_event_type_name")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate IntPtr GetTypeName(EventTypes type);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LibVlcEventCallBack(ref LibVlcEventArgs eventArgs, IntPtr userData);
        
        public enum EventTypes : uint
        {
            MediaMetaChanged = 0,
            
            MediaSubItemAdded,
            
            MediaDurationChanged,
            
            MediaParsedChanged,
            
            MediaFreed,
            
            MediaStateChanged,
            
            MediaPlayerMediaChanged = 0x100,

            MediaPlayerNothingSpecial,
            
            MediaPlayerOpening,
            
            MediaPlayerBuffering,
            
            MediaPlayerPlaying,
            
            MediaPlayerPaused,
            
            MediaPlayerStopped,
            
            MediaPlayerForward,
            
            MediaPlayerBackward,
            
            MediaPlayerEndReached,
            
            MediaPlayerEncounteredError,
            
            MediaPlayerTimeChanged,
            
            MediaPlayerPositionChanged,
            
            MediaPlayerSeekableChanged,
            
            MediaPlayerPausableChanged,
            
            MediaPlayerTitleChanged,
            
            MediaPlayerSnapshotTaken,
            
            MediaPlayerLengthChanged,
            
            MediaPlayerVideoOutChanged,
            
            MediaListItemAdded = 0x200,
            
            MediaListWillAddItem,
            
            MediaListItemDeleted,
            
            MediaListWillDeleteItem,
            
            MediaListViewItemAdded = 0x300,
            
            MediaListViewWillAddItem,
            
            MediaListViewItemDeleted,
            
            MediaListViewWillDeleteItem,
            
            MediaListPlayerPlayed = 0x400,
            
            MediaListPlayerNextItemSet,
            
            MediaListPlayerStopped,
            
            MediaDiscovererStarted = 0x500,
            
            MediaDiscovererEnded,
            
            VlmMediaAdded = 0x600,

            VlmMediaRemoved,
            
            VlmMediaChanged,
            
            VlmMediaInstanceStarted,
            
            VlmMediaInstanceStopped,
            
            VlmMediaInstanceStatusInit,
            
            VlmMediaInstanceStatusOpening,
            
            VlmMediaInstanceStatusPlaying,
            
            VlmMediaInstanceStatusPause,
            
            VlmMediaInstanceStatusEnd,
            
            VlmMediaInstanceStatusError
        }

#if AnyCPU || X86
        [StructLayout(LayoutKind.Explicit)]
        public struct LibVlcEventArgs
        {
            [FieldOffset(0)] public EventTypes Type;

            [FieldOffset(4)] public IntPtr ObjectHandle;

            #region media descriptor

            [FieldOffset(8)] public MediaMetaChangedArgs MediaMetaChanged;

            [FieldOffset(8)] public MediaSubitemAddedArgs MediaSubitemAdded;

            [FieldOffset(8)] public MediaDurationChangedArgs MediaDurationChanged;

            [FieldOffset(8)] public MediaParsedChangedArgs MediaParsedChanged;

            [FieldOffset(8)] public MediaFreedArgs MediaFreed;

            [FieldOffset(8)] public MediaStateChangedArgs MediaStateChanged;

            #endregion media descriptor

            #region media instance

            [FieldOffset(8)] public MediaPlayerBufferingArgs MediaPlayerBuffering;

            [FieldOffset(8)] public MediaPlayerPositionChangedArgs MediaPlayerPositionChanged;

            [FieldOffset(8)] public MediaPlayerTimeChangedArgs MediaPlayerTimeChanged;

            [FieldOffset(8)] public MediaPlayerTitleChangedArgs MediaPlayerTitleChanged;

            [FieldOffset(8)] public MediaPlayerSeekableChangedArgs MediaPlayerSeekableChanged;

            [FieldOffset(8)] public MediaPlayerPausableChangedArgs MediaPlayerPausableChanged;

            [FieldOffset(8)] public MediaPlayerVideoOutChangedArgs MediaPlayerVideoOutChanged;

            #endregion media instance

            #region media list

            [FieldOffset(8)] public MediaListItemAddedArgs MediaListItemAdded;

            [FieldOffset(8)] public MediaListWillAddItemArgs MediaListWillAddItem;

            [FieldOffset(8)] public MediaListItemDeletedArgs MediaListItemDeleted;

            [FieldOffset(8)] public MediaListWillDeleteItemArgs MediaListWillDeleteItem;

            #endregion media list

            #region media list player

            [FieldOffset(8)] public MediaListPlayerNextItemSetArgs MediaListPlayerNextItemSet;

            #endregion media list player

            #region snapshot taken

            [FieldOffset(8)] public MediaPlayerSnapshotTakenArgs MediaPlayerSnapshotTaken;

            #endregion snapshot taken

            #region Length changed

            [FieldOffset(8)] public MediaPlayerLengthChangedArgs MediaPlayerLengthChanged;

            #endregion Length changed

            #region VLM media

            [FieldOffset(8)] public VlmMediaEventArgs VlmMediaEvent;

            #endregion VLM media

            #region Extra MediaPlayer

            [FieldOffset(8)] public MediaPlayerMediaChangedArgs MediaPlayerMediaChanged;

            #endregion Extra MediaPlayer
        }
#else
                [StructLayout(LayoutKind.Explicit)]
        public struct LibVlcEventArgs
        {
            [FieldOffset(0)] public EventTypes Type;

            [FieldOffset(8)] public IntPtr ObjectHandle;

        #region media descriptor

            [FieldOffset(16)] public MediaMetaChangedArgs MediaMetaChanged;

            [FieldOffset(16)] public MediaSubitemAddedArgs MediaSubitemAdded;

            [FieldOffset(16)] public MediaDurationChangedArgs MediaDurationChanged;

            [FieldOffset(16)] public MediaParsedChangedArgs MediaParsedChanged;

            [FieldOffset(16)] public MediaFreedArgs MediaFreed;

            [FieldOffset(16)] public MediaStateChangedArgs MediaStateChanged;

        #endregion media descriptor

        #region media instance

            [FieldOffset(16)] public MediaPlayerBufferingArgs MediaPlayerBuffering;

            [FieldOffset(16)] public MediaPlayerPositionChangedArgs MediaPlayerPositionChanged;

            [FieldOffset(16)] public MediaPlayerTimeChangedArgs MediaPlayerTimeChanged;

            [FieldOffset(16)] public MediaPlayerTitleChangedArgs MediaPlayerTitleChanged;

            [FieldOffset(16)] public MediaPlayerSeekableChangedArgs MediaPlayerSeekableChanged;

            [FieldOffset(16)] public MediaPlayerPausableChangedArgs MediaPlayerPausableChanged;

            [FieldOffset(16)] public MediaPlayerVideoOutChangedArgs MediaPlayerVideoOutChanged;

        #endregion media instance

        #region media list

            [FieldOffset(16)] public MediaListItemAddedArgs MediaListItemAdded;

            [FieldOffset(16)] public MediaListWillAddItemArgs MediaListWillAddItem;

            [FieldOffset(16)] public MediaListItemDeletedArgs MediaListItemDeleted;

            [FieldOffset(16)] public MediaListWillDeleteItemArgs MediaListWillDeleteItem;

        #endregion media list

        #region media list player

            [FieldOffset(16)] public MediaListPlayerNextItemSetArgs MediaListPlayerNextItemSet;

        #endregion media list player

        #region snapshot taken

            [FieldOffset(16)] public MediaPlayerSnapshotTakenArgs MediaPlayerSnapshotTaken;

        #endregion snapshot taken

        #region Length changed

            [FieldOffset(16)] public MediaPlayerLengthChangedArgs MediaPlayerLengthChanged;

        #endregion Length changed

        #region VLM media

            [FieldOffset(16)] public VlmMediaEventArgs VlmMediaEvent;

        #endregion VLM media

        #region Extra MediaPlayer

            [FieldOffset(16)] public MediaPlayerMediaChangedArgs MediaPlayerMediaChanged;

        #endregion Extra MediaPlayer
        }
#endif

        #region media descriptor

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaMetaChangedArgs
        {
            public Media.MetaDataType MetaType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaSubitemAddedArgs
        {
            public IntPtr NewChild;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaDurationChangedArgs
        {
            public long NewDuration;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaParsedChangedArgs
        {
            public int NewStatus;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaFreedArgs
        {
            public IntPtr MediaHandler;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaStateChangedArgs
        {
            public Media.MediaState NewState;
        }

        #endregion media descriptor

        #region media instance

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaPlayerBufferingArgs
        {
            public float NewCache;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaPlayerPositionChangedArgs
        {
            public float NewPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaPlayerTimeChangedArgs
        {
            public long NewTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaPlayerTitleChangedArgs
        {
            public int NewTitle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaPlayerSeekableChangedArgs
        {
            public int NewSeekable;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaPlayerPausableChangedArgs
        {
            public int NewPausable;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaPlayerVideoOutChangedArgs
        {
            public int NewCount;
        }

        #endregion media instance

        #region media list

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaListItemAddedArgs
        {
            public IntPtr ItemHandle;
            public int Index;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaListWillAddItemArgs
        {
            public IntPtr ItemHandle;
            public int Index;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaListItemDeletedArgs
        {
            public IntPtr ItemHandle;
            public int Index;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaListWillDeleteItemArgs
        {
            public IntPtr ItemHandle;
            public int Index;
        }

        #endregion media list

        #region media list player

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaListPlayerNextItemSetArgs
        {
            public IntPtr ItemHandle;
        }

        #endregion media list player

        #region snapshot taken

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaPlayerSnapshotTakenArgs
        {
            public IntPtr pszFilename;
        }

        #endregion snapshot taken

        #region Length changed

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaPlayerLengthChangedArgs
        {
            public long NewLength;
        }

        #endregion Length changed

        #region VLM media

        [StructLayout(LayoutKind.Sequential)]
        public struct VlmMediaEventArgs
        {
            public IntPtr pszMediaName;
            public IntPtr pszInstanceName;
        }

        #endregion VLM media

        #region Extra MediaPlayer

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaPlayerMediaChangedArgs
        {
            public IntPtr NewMediaHandle;
        }

        #endregion Extra MediaPlayer
    }
}