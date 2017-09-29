using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Popcorn.Vlc.Interop;
using Popcorn.Vlc.Interop.Core.Events;
using Popcorn.Vlc.Interop.Media;

namespace Popcorn.Vlc
{
    /// <summary>
    ///     The API warpper of LibVlc media.
    /// </summary>
    public class VlcMedia : IVlcObjectWithEvent
    {
        private static LibVlcFunction<MediaAddOption> _addOptionFunction;
        private static LibVlcFunction<MediaAddOptionFlag> _addOptionFlagFunction;
        private static LibVlcFunction<MediaDuplicate> _duplicateFunction;
        private static LibVlcFunction<GetEventManager> _getEventManagerFunction;
        private static LibVlcFunction<GetCodecDescription> _getCodecDescriptionFunction;
        private static LibVlcFunction<GetDuration> _getDurationFunction;
        private static LibVlcFunction<GetMeta> _getMetaFunction;
        private static LibVlcFunction<GetMrl> _getMrlFunction;
        private static LibVlcFunction<GetState> _getStateFunction;
        private static LibVlcFunction<GetStats> _getStatsFunction;
        private static LibVlcFunction<GetTracksInfo> _getTracksInfoFunction;
        private static LibVlcFunction<GetUserData> _getUserDataFunction;
        private static LibVlcFunction<IsParsed> _isParsedFunction;
        private static LibVlcFunction<CreateMediaAsNewNode> _createMediaAsNewNodeFunction;
        private static LibVlcFunction<CreateMediaFromFileDescriptor> _createMediaFromFileDescriptorFunction;
        private static LibVlcFunction<CreateMediaFromLocation> _createMediaFromLocationFunction;
        private static LibVlcFunction<CreateMediaFromPath> _createMediaFromPathFunction;
        private static LibVlcFunction<ParseMedia> _parseMediaFunction;
        private static LibVlcFunction<ParseMediaAsync> _parseMediaAsyncFunction;
        private static LibVlcFunction<ParseMediaWithOptionAsync> _parseMediaWithOptionAsyncFunction;
        private static LibVlcFunction<ReleaseMedia> _releaseMediaFunction;
        private static LibVlcFunction<RetainMedia> _retainMediaFunction;
        private static LibVlcFunction<SaveMeta> _saveMetaFunction;
        private static LibVlcFunction<SetMeta> _setMetaFunction;
        private static LibVlcFunction<SetUserData> _setUserDataFunction;
        private static LibVlcFunction<GetSubitems> _getSubitemsFunction;
        private static LibVlcFunction<GetTracks> _getTracksFunction;
        private readonly LibVlcEventCallBack _onDurationChanged;
        private readonly LibVlcEventCallBack _onFreed;

        private readonly LibVlcEventCallBack _onMetaChanged;
        private readonly LibVlcEventCallBack _onParsedChanged;
        private readonly LibVlcEventCallBack _onStateChanged;
        private readonly LibVlcEventCallBack _onSubItemAdded;

        private bool _disposed;
        private GCHandle _onDurationChangedHandle;
        private GCHandle _onFreedHandle;

        private GCHandle _onMetaChangedHandle;
        private GCHandle _onParsedChangedHandle;
        private GCHandle _onStateChangedHandle;
        private GCHandle _onSubItemAddedHandle;

        private MediaStats _stats;

        static VlcMedia()
        {
            IsLibLoaded = false;
        }

        private VlcMedia(IVlcObject parentVlcObject, IntPtr pointer)
        {
            VlcInstance = parentVlcObject.VlcInstance;
            InstancePointer = pointer;
            EventManager = new VlcEventManager(this, _getEventManagerFunction.Delegate(InstancePointer));

            _onMetaChanged = OnMetaChanged;
            _onSubItemAdded = OnSubItemAdded;
            _onDurationChanged = OnDurationChanged;
            _onParsedChanged = OnParsedChanged;
            _onFreed = OnFreed;
            _onStateChanged = OnStateChanged;

            _onMetaChangedHandle = GCHandle.Alloc(_onMetaChanged);
            _onSubItemAddedHandle = GCHandle.Alloc(_onSubItemAdded);
            _onDurationChangedHandle = GCHandle.Alloc(_onDurationChanged);
            _onParsedChangedHandle = GCHandle.Alloc(_onParsedChanged);
            _onFreedHandle = GCHandle.Alloc(_onFreed);
            _onStateChangedHandle = GCHandle.Alloc(_onStateChanged);

            HandleManager.Add(this);

            EventManager.Attach(EventTypes.MediaMetaChanged, _onMetaChanged, IntPtr.Zero);
            EventManager.Attach(EventTypes.MediaSubItemAdded, _onSubItemAdded, IntPtr.Zero);
            EventManager.Attach(EventTypes.MediaDurationChanged, _onDurationChanged, IntPtr.Zero);
            EventManager.Attach(EventTypes.MediaParsedChanged, _onParsedChanged, IntPtr.Zero);
            EventManager.Attach(EventTypes.MediaFreed, _onFreed, IntPtr.Zero);
            EventManager.Attach(EventTypes.MediaStateChanged, _onStateChanged, IntPtr.Zero);
        }
        
        public static bool IsLibLoaded { get; private set; }
        
        public TimeSpan Duration
        {
            get { return new TimeSpan(_getDurationFunction.Delegate(InstancePointer)*10000); }
        }
        
        public String Mrl
        {
            get { return InteropHelper.PtrToString(_getMrlFunction.Delegate(InstancePointer)); }
        }

        public MediaState State
        {
            get
            {
                if (InstancePointer == IntPtr.Zero)
                    return MediaState.NothingSpecial;

                var state = _getStateFunction.Delegate(InstancePointer);
                if (state == MediaState.Error)
                {
                    Error = VlcError.GetErrorMessage();
                }
                return state;
            }
        }

        public string Error { get; private set; }
        
        public MediaStats Stats
        {
            get
            {
                if (_getStatsFunction.Delegate(InstancePointer, ref _stats))
                {
                    return _stats;
                }
                throw new Exception("无法获取媒体统计信息");
            }
        }
        
        public IntPtr UserData
        {
            get { return _getUserDataFunction.Delegate(InstancePointer); }

            set { _setUserDataFunction.Delegate(InstancePointer, value); }
        }
        
        public bool IsParsed
        {
            get { return _isParsedFunction.Delegate(InstancePointer); }
        }

        public IntPtr Subitems
        {
            get { return _getSubitemsFunction.Delegate(InstancePointer); }
        }
        
        public IntPtr InstancePointer { get; private set; }

        public Vlc VlcInstance { get; private set; }

        public VlcEventManager EventManager { get; private set; }
        
        public void Dispose()
        {
            Dispose(true);
        }
        
        internal static void LoadLibVlc()
        {
            if (!IsLibLoaded)
            {
                _addOptionFunction = new LibVlcFunction<MediaAddOption>();
                _addOptionFlagFunction = new LibVlcFunction<MediaAddOptionFlag>();
                _duplicateFunction = new LibVlcFunction<MediaDuplicate>();
                _getEventManagerFunction = new LibVlcFunction<GetEventManager>();
                _getCodecDescriptionFunction = new LibVlcFunction<GetCodecDescription>();
                _getDurationFunction = new LibVlcFunction<GetDuration>();
                _getMetaFunction = new LibVlcFunction<GetMeta>();
                _getMrlFunction = new LibVlcFunction<GetMrl>();
                _getStateFunction = new LibVlcFunction<GetState>();
                _getStatsFunction = new LibVlcFunction<GetStats>();
                _getTracksInfoFunction = new LibVlcFunction<GetTracksInfo>();
                _getUserDataFunction = new LibVlcFunction<GetUserData>();
                _isParsedFunction = new LibVlcFunction<IsParsed>();
                _createMediaAsNewNodeFunction = new LibVlcFunction<CreateMediaAsNewNode>();
                _createMediaFromFileDescriptorFunction = new LibVlcFunction<CreateMediaFromFileDescriptor>();
                _createMediaFromLocationFunction = new LibVlcFunction<CreateMediaFromLocation>();
                _createMediaFromPathFunction = new LibVlcFunction<CreateMediaFromPath>();
                
                _parseMediaFunction = new LibVlcFunction<ParseMedia>();
                _parseMediaAsyncFunction = new LibVlcFunction<ParseMediaAsync>();
                _parseMediaWithOptionAsyncFunction = new LibVlcFunction<ParseMediaWithOptionAsync>();
                _releaseMediaFunction = new LibVlcFunction<ReleaseMedia>();
                _retainMediaFunction = new LibVlcFunction<RetainMedia>();
                _saveMetaFunction = new LibVlcFunction<SaveMeta>();
                _setMetaFunction = new LibVlcFunction<SetMeta>();
                _setUserDataFunction = new LibVlcFunction<SetUserData>();
                _getSubitemsFunction = new LibVlcFunction<GetSubitems>();
                _getTracksFunction = new LibVlcFunction<GetTracks>();
                IsLibLoaded = true;
            }
        }

        private void OnMetaChanged(ref LibVlcEventArgs arg, IntPtr userData)
        {
            if (MetaChanged != null)
            {
                MetaChanged(this, new ObjectEventArgs<MediaMetaChangedArgs>(arg.MediaMetaChanged));
            }
        }

        public event EventHandler<ObjectEventArgs<MediaMetaChangedArgs>> MetaChanged;

        private void OnSubItemAdded(ref LibVlcEventArgs arg, IntPtr userData)
        {
            if (SubItemAdded != null)
            {
                SubItemAdded(this, new ObjectEventArgs<MediaSubitemAddedArgs>(arg.MediaSubitemAdded));
            }
        }

        public event EventHandler<ObjectEventArgs<MediaSubitemAddedArgs>> SubItemAdded;

        private void OnDurationChanged(ref LibVlcEventArgs arg, IntPtr userData)
        {
            if (DurationChanged != null)
            {
                DurationChanged(this, new ObjectEventArgs<MediaDurationChangedArgs>(arg.MediaDurationChanged));
            }
        }

        public event EventHandler<ObjectEventArgs<MediaDurationChangedArgs>> DurationChanged;

        private void OnParsedChanged(ref LibVlcEventArgs arg, IntPtr userData)
        {
            if (ParsedChanged != null)
            {
                ParsedChanged(this, new ObjectEventArgs<MediaParsedChangedArgs>(arg.MediaParsedChanged));
            }
        }

        public event EventHandler<ObjectEventArgs<MediaParsedChangedArgs>> ParsedChanged;

        private void OnFreed(ref LibVlcEventArgs arg, IntPtr userData)
        {
            if (Freed != null)
            {
                Freed(this, new ObjectEventArgs<MediaFreedArgs>(arg.MediaFreed));
            }
        }

        public event EventHandler<ObjectEventArgs<MediaFreedArgs>> Freed;

        private void OnStateChanged(ref LibVlcEventArgs arg, IntPtr userData)
        {
            var actualState = State;
            StateChanged?.Invoke(this, new ObjectEventArgs<MediaStateChangedArgs>(new MediaStateChangedArgs {NewState = actualState }));
        }

        public event EventHandler<ObjectEventArgs<MediaStateChangedArgs>> StateChanged;
        
        public static VlcMedia CreateAsNewNode(Vlc vlc, String name)
        {
            GCHandle handle = GCHandle.Alloc(Encoding.UTF8.GetBytes(name), GCHandleType.Pinned);
            var madia = new VlcMedia(vlc,
                _createMediaAsNewNodeFunction.Delegate(vlc.InstancePointer, handle.AddrOfPinnedObject()));
            handle.Free();
            return madia;
        }

        public static VlcMedia CreateFormFileDescriptor(Vlc vlc, int fileDescriptor)
        {
            return new VlcMedia(vlc,
                _createMediaFromFileDescriptorFunction.Delegate(vlc.InstancePointer, fileDescriptor));
        }
        
        public static VlcMedia CreateFormLocation(Vlc vlc, String url)
        {
            GCHandle handle = GCHandle.Alloc(Encoding.UTF8.GetBytes(url), GCHandleType.Pinned);
            var media = new VlcMedia(vlc,
                _createMediaFromLocationFunction.Delegate(vlc.InstancePointer, handle.AddrOfPinnedObject()));
            handle.Free();
            return media;
        }

        public static VlcMedia CreateFormPath(Vlc vlc, String path)
        {
            GCHandle handle = GCHandle.Alloc(Encoding.UTF8.GetBytes(path), GCHandleType.Pinned);
            var media = new VlcMedia(vlc,
                _createMediaFromPathFunction.Delegate(vlc.InstancePointer, handle.AddrOfPinnedObject()));
            handle.Free();
            return media;
        }
        
        public void AddOption(params String[] options)
        {
            GCHandle handle = GCHandle.Alloc(Encoding.UTF8.GetBytes(String.Join(" ", options)), GCHandleType.Pinned);
            _addOptionFunction.Delegate(InstancePointer, handle.AddrOfPinnedObject());
            handle.Free();
        }
        
        public void AddOptionFlag(String options, MediaOption flag)
        {
            GCHandle handle = GCHandle.Alloc(Encoding.UTF8.GetBytes(options), GCHandleType.Pinned);
            _addOptionFlagFunction.Delegate(InstancePointer, handle.AddrOfPinnedObject(), flag);
            handle.Free();
        }
        
        public VlcMedia Duplicate()
        {
            return new VlcMedia(this, _duplicateFunction.Delegate(InstancePointer));
        }
        
        public static String GetCodecDescription(TrackType type, int codec)
        {
            return InteropHelper.PtrToString(_getCodecDescriptionFunction.Delegate(type, codec));
        }
        
        public String GetMeta(MetaDataType type)
        {
            return InteropHelper.PtrToString(_getMetaFunction.Delegate(InstancePointer, type));
        }

        public MediaTrackInfo[] GetTrackInfo()
        {
            IntPtr pointer;
            var count = _getTracksInfoFunction.Delegate(InstancePointer, out pointer);
            var result = new MediaTrackInfo[count];
            var temp = pointer;

            for (var i = 0; i < count; i++)
            {
                result[i] = (MediaTrackInfo) Marshal.PtrToStructure(temp, typeof (MediaTrackInfo));
                temp = (IntPtr) ((int) temp + Marshal.SizeOf(typeof (MediaTrackInfo)));
            }

            LibVlcManager.Free(pointer);
            return result;
        }
        
        public void Parse()
        {
            _parseMediaFunction.Delegate(InstancePointer);
        }
        
        public void ParseAsync()
        {
            _parseMediaAsyncFunction.Delegate(InstancePointer);
        }

        public void ParseWithOptionAsync(MediaParseFlag option)
        {
            _parseMediaWithOptionAsyncFunction.Delegate(InstancePointer, option);
        }
        
        public void RetainMedia()
        {
            _retainMediaFunction.Delegate(InstancePointer);
        }
        
        public bool SaveMeta()
        {
            return _saveMetaFunction.Delegate(InstancePointer);
        }
        
        public void SetMeta(MetaDataType type, String data)
        {
            GCHandle handle = GCHandle.Alloc(Encoding.UTF8.GetBytes(data), GCHandleType.Pinned);
            _setMetaFunction.Delegate(InstancePointer, type, handle.AddrOfPinnedObject());
            handle.Free();
        }

        /*
        public void GetSubitems()
        {
            return GetSubitemsFunction.Delegate(InstancePointer);
        }
        */
        
        public MediaTrackList GetTracks()
        {
            var pointer = IntPtr.Zero;
            var count = _getTracksFunction.Delegate(InstancePointer, ref pointer);
            return new MediaTrackList(pointer, count);
        }

        public VlcMediaPlayer CreateMediaPlayer()
        {
            return VlcMediaPlayer.CreateFormMedia(this);
        }

        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            HandleManager.Remove(this);

            EventManager.Detach(EventTypes.MediaMetaChanged, _onMetaChanged, IntPtr.Zero);
            EventManager.Detach(EventTypes.MediaSubItemAdded, _onSubItemAdded, IntPtr.Zero);
            EventManager.Detach(EventTypes.MediaDurationChanged, _onDurationChanged, IntPtr.Zero);
            EventManager.Detach(EventTypes.MediaParsedChanged, _onParsedChanged, IntPtr.Zero);
            EventManager.Detach(EventTypes.MediaFreed, _onFreed, IntPtr.Zero);
            EventManager.Detach(EventTypes.MediaStateChanged, _onStateChanged, IntPtr.Zero);

            EventManager.Dispose();
            _onMetaChangedHandle.Free();
            _onSubItemAddedHandle.Free();
            _onDurationChangedHandle.Free();
            _onParsedChangedHandle.Free();
            _onFreedHandle.Free();
            _onStateChangedHandle.Free();
            _releaseMediaFunction.Delegate(InstancePointer);
            InstancePointer = IntPtr.Zero;

            _disposed = true;
        }
    }

    public class ObjectEventArgs<T> : EventArgs
    {
        public ObjectEventArgs()
        {
        }

        public ObjectEventArgs(T value)
        {
            Value = value;
        }

        public T Value { get; set; }
    }
}