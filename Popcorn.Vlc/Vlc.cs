using System;
using System.Runtime.InteropServices;
using System.Text;
using Popcorn.Vlc.Interop;
using Popcorn.Vlc.Interop.Core;
using Popcorn.Vlc.Interop.Core.Events;
using Popcorn.Vlc.Interop.VLM;

namespace Popcorn.Vlc
{
    public partial class Vlc
    {
        #region --- Fields ---

        private bool _disposed;

        #region LibVlcFunctions
        
        private static LibVlcFunction<NewInstance> _newInstanceFunction;
        
        private static LibVlcFunction<ReleaseInstance> _releaseInstanceFunction;
        
        private static LibVlcFunction<RetainInstance> _retainInstanceFunction;
        
        private static LibVlcFunction<AddInterface> _addInterfaceFunction;
        
        private static LibVlcFunction<SetExitHandler> _setExitHandlerFunction;
        
        private static LibVlcFunction<Wait> _waitFunction;
        
        private static LibVlcFunction<SetUserAgent> _setUserAgentFunction;
        
        private static LibVlcFunction<SetAppId> _setAppIdFunction;
        
        private static LibVlcFunction<GetAudioFilterList> _getAudioFilterListFunction;
        
        private static LibVlcFunction<GetVideoFilterList> _getVideoFilterListFunction;

        #endregion LibVlcFunctions

        #endregion --- Fields ---

        #region --- Initialization ---

        static Vlc()
        {
            IsLibLoaded = false;
        }
        
        public Vlc() :
            this(new[]
            {
                "-I", "dummy", "--ignore-config", "--no-video-title", "--file-logging", "--logfile=log.txt",
                "--verbose=2", "--no-sub-autodetect-file"
            })
        {
        }
        
        public Vlc(String[] argv)
        {
            InstancePointer = argv == null
                ? _newInstanceFunction.Delegate(0, IntPtr.Zero)
                : _newInstanceFunction.Delegate(argv.Length, InteropHelper.StringArrayToPtr(argv));

            if (InstancePointer == IntPtr.Zero)
            {
                var ex = VlcError.GetErrorMessage();
                throw new VlcCreateFailException(ex);
            }

            EventManager = new VlcEventManager(this, _getMediaEventManagerFunction.Delegate(InstancePointer));

            _onVlmMediaAdded = OnVlmMediaAdded;
            _onVlmMediaRemoved = OnVlmMediaRemoved;
            _onVlmMediaChanged = OnVlmMediaChanged;
            _onVlmMediaInstanceStarted = OnVlmMediaInstanceStarted;
            _onVlmMediaInstanceStopped = OnVlmMediaInstanceStopped;
            _onVlmMediaInstanceStatusInit = OnVlmMediaInstanceStatusInit;
            _onVlmMediaInstanceStatusOpening = OnVlmMediaInstanceStatusOpening;
            _onVlmMediaInstanceStatusPlaying = OnVlmMediaInstanceStatusPlaying;
            _onVlmMediaInstanceStatusPause = OnVlmMediaInstanceStatusPause;
            _onVlmMediaInstanceStatusEnd = OnVlmMediaInstanceStatusEnd;
            _onVlmMediaInstanceStatusError = OnVlmMediaInstanceStatusError;

            _onVlmMediaAddedHandle = GCHandle.Alloc(_onVlmMediaAdded);
            _onVlmMediaRemovedHandle = GCHandle.Alloc(_onVlmMediaRemoved);
            _onVlmMediaChangedHandle = GCHandle.Alloc(_onVlmMediaChanged);
            _onVlmMediaInstanceStartedHandle = GCHandle.Alloc(_onVlmMediaInstanceStarted);
            _onVlmMediaInstanceStoppedHandle = GCHandle.Alloc(_onVlmMediaInstanceStopped);
            _onVlmMediaInstanceStatusInitHandle = GCHandle.Alloc(_onVlmMediaInstanceStatusInit);
            _onVlmMediaInstanceStatusOpeningHandle = GCHandle.Alloc(_onVlmMediaInstanceStatusOpening);
            _onVlmMediaInstanceStatusPlayingHandle = GCHandle.Alloc(_onVlmMediaInstanceStatusPlaying);
            _onVlmMediaInstanceStatusPauseHandle = GCHandle.Alloc(_onVlmMediaInstanceStatusPause);
            _onVlmMediaInstanceStatusEndHandle = GCHandle.Alloc(_onVlmMediaInstanceStatusEnd);
            _onVlmMediaInstanceStatusErrorHandle = GCHandle.Alloc(_onVlmMediaInstanceStatusError);

            HandleManager.Add(this);

            EventManager.Attach(EventTypes.VlmMediaAdded, _onVlmMediaAdded, IntPtr.Zero);
            EventManager.Attach(EventTypes.VlmMediaRemoved, _onVlmMediaRemoved, IntPtr.Zero);
            EventManager.Attach(EventTypes.VlmMediaChanged, _onVlmMediaChanged, IntPtr.Zero);
            EventManager.Attach(EventTypes.VlmMediaInstanceStarted, _onVlmMediaInstanceStarted, IntPtr.Zero);
            EventManager.Attach(EventTypes.VlmMediaInstanceStopped, _onVlmMediaInstanceStopped, IntPtr.Zero);
            EventManager.Attach(EventTypes.VlmMediaInstanceStatusInit, _onVlmMediaInstanceStatusInit, IntPtr.Zero);
            EventManager.Attach(EventTypes.VlmMediaInstanceStatusOpening, _onVlmMediaInstanceStatusOpening, IntPtr.Zero);
            EventManager.Attach(EventTypes.VlmMediaInstanceStatusPlaying, _onVlmMediaInstanceStatusPlaying, IntPtr.Zero);
            EventManager.Attach(EventTypes.VlmMediaInstanceStatusPause, _onVlmMediaInstanceStatusPause, IntPtr.Zero);
            EventManager.Attach(EventTypes.VlmMediaInstanceStatusEnd, _onVlmMediaInstanceStatusEnd, IntPtr.Zero);
            EventManager.Attach(EventTypes.VlmMediaInstanceStatusError, _onVlmMediaInstanceStatusError, IntPtr.Zero);
        }

        #endregion --- Initialization ---

        #region --- Cleanup ---

        protected void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            HandleManager.Remove(this);

            _releaseInstanceFunction.Delegate(InstancePointer);

            InstancePointer = IntPtr.Zero;

            _onVlmMediaAddedHandle.Free();
            _onVlmMediaRemovedHandle.Free();
            _onVlmMediaChangedHandle.Free();
            _onVlmMediaInstanceStartedHandle.Free();
            _onVlmMediaInstanceStoppedHandle.Free();
            _onVlmMediaInstanceStatusInitHandle.Free();
            _onVlmMediaInstanceStatusOpeningHandle.Free();
            _onVlmMediaInstanceStatusPlayingHandle.Free();
            _onVlmMediaInstanceStatusPauseHandle.Free();
            _onVlmMediaInstanceStatusEndHandle.Free();
            _onVlmMediaInstanceStatusErrorHandle.Free();

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion --- Cleanup ---

        #region --- Properties ---
        
        public static bool IsLibLoaded { get; private set; }
        
        public IntPtr InstancePointer { get; private set; }

        public Vlc VlcInstance
        {
            get { return this; }
        }

        #endregion --- Properties ---

        #region --- Methods ---

        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="NoLibVlcFunctionAttributeException">
        ///     For LibVlcFunction, need LibVlcFunctionAttribute to get Infomation
        ///     of function.
        /// </exception>
        /// <exception cref="FunctionNotFoundException">Can't find function in dll.</exception>
        internal static void LoadLibVlc()
        {
            if (IsLibLoaded) return;

            _newInstanceFunction = new LibVlcFunction<NewInstance>();
            _releaseInstanceFunction = new LibVlcFunction<ReleaseInstance>();
            _retainInstanceFunction = new LibVlcFunction<RetainInstance>();
            _addInterfaceFunction = new LibVlcFunction<AddInterface>();
            _setExitHandlerFunction = new LibVlcFunction<SetExitHandler>();
            _waitFunction = new LibVlcFunction<Wait>();
            _setUserAgentFunction = new LibVlcFunction<SetUserAgent>();
            _setAppIdFunction = new LibVlcFunction<SetAppId>();
            _getAudioFilterListFunction = new LibVlcFunction<GetAudioFilterList>();
            _getVideoFilterListFunction = new LibVlcFunction<GetVideoFilterList>();
            _releaseVlmInstanceFunction = new LibVlcFunction<ReleaseVlmInstance>();
            _newBroadCastInputFunction = new LibVlcFunction<NewBroadCastInput>();
            _newVodInputFunction = new LibVlcFunction<NewVodInput>();
            _delBoroadcastOrOvdFunction = new LibVlcFunction<DelBoroadcastOrOvd>();
            _mediaSwitchFunction = new LibVlcFunction<MediaSwitch>();
            _setMediaOutputFunction = new LibVlcFunction<SetMediaOutput>();
            _setMediaInputFunction = new LibVlcFunction<SetMediaInput>();
            _addMediaInputFunction = new LibVlcFunction<AddMediaInput>();
            _setMediaLoopFunction = new LibVlcFunction<SetMediaLoop>();
            _setVodMuxerFunction = new LibVlcFunction<SetVodMuxer>();
            _editMediaParasFunction = new LibVlcFunction<EditMediaParas>();
            _playNamedBroadcastFunction = new LibVlcFunction<PlayNamedBroadcast>();
            _stopNamedBroadcastFunction = new LibVlcFunction<StopNamedBroadcast>();
            _pauseNamedBroadcastFunction = new LibVlcFunction<PauseNamedBroadcast>();
            _seekInNamedBroadcastFunction = new LibVlcFunction<SeekInNamedBroadcast>();
            _returnJsonMessageFunction = new LibVlcFunction<ReturnJsonMessage>();
            _getMediaPositionFunction = new LibVlcFunction<GetMediaPosition>();
            _getMediaTimeFunction = new LibVlcFunction<GetMediaTime>();
            _getMediaLengthFunction = new LibVlcFunction<GetMediaLength>();
            _getMediaBackRateFunction = new LibVlcFunction<GetMediaBackRate>();
            _getMediaEventManagerFunction = new LibVlcFunction<GetMediaEventManager>();
            IsLibLoaded = true;
        }
        
        public void Retain()
        {
            _retainInstanceFunction.Delegate(InstancePointer);
        }
        
        public bool AddInterface(String name)
        {
            var handle = GCHandle.Alloc(Encoding.UTF8.GetBytes(name), GCHandleType.Pinned);
            var result = _addInterfaceFunction.Delegate(InstancePointer, handle.AddrOfPinnedObject()) == 0;
            handle.Free();
            return result;
        }
        
        public void Wait()
        {
            _waitFunction.Delegate(InstancePointer);
        }
        
        public void SetUserAgent(String name, String http)
        {
            var nameHandle = GCHandle.Alloc(Encoding.UTF8.GetBytes(name), GCHandleType.Pinned);
            var httpHandle = GCHandle.Alloc(Encoding.UTF8.GetBytes(http), GCHandleType.Pinned);
            _setUserAgentFunction.Delegate(InstancePointer, nameHandle.AddrOfPinnedObject(),
                httpHandle.AddrOfPinnedObject());
            nameHandle.Free();
            httpHandle.Free();
        }
        
        public void SetAppId(String id, String version, String icon)
        {
            var idHandle = GCHandle.Alloc(Encoding.UTF8.GetBytes(id), GCHandleType.Pinned);
            var versionHandle = GCHandle.Alloc(Encoding.UTF8.GetBytes(version), GCHandleType.Pinned);
            var iconHandle = GCHandle.Alloc(Encoding.UTF8.GetBytes(icon), GCHandleType.Pinned);
            _setAppIdFunction.Delegate(InstancePointer, idHandle.AddrOfPinnedObject(),
                versionHandle.AddrOfPinnedObject(), iconHandle.AddrOfPinnedObject());
            idHandle.Free();
            versionHandle.Free();
            iconHandle.Free();
        }
        
        public ModuleDescriptionList GetAudioFilterList()
        {
            return new ModuleDescriptionList(_getAudioFilterListFunction.Delegate(InstancePointer));
        }
        
        public ModuleDescriptionList GetVideoFilterList()
        {
            return new ModuleDescriptionList(_getVideoFilterListFunction.Delegate(InstancePointer));
        }
        
        public VlcMedia CreateMediaAsNewNode(String name)
        {
            return VlcMedia.CreateAsNewNode(this, name);
        }
        
        public VlcMedia CreateMediaFromFileDescriptor(int fileDescriptor)
        {
            return VlcMedia.CreateFormFileDescriptor(this, fileDescriptor);
        }
        
        public VlcMedia CreateMediaFromLocation(String url)
        {
            return VlcMedia.CreateFormLocation(this, url);
        }
        
        public VlcMedia CreateMediaFromPath(String path)
        {
            return VlcMedia.CreateFormPath(this, path);
        }

        public VlcMediaPlayer CreateMediaPlayer()
        {
            return VlcMediaPlayer.Create(this);
        }

        public void SetExitHandler(ExitHandler handler, IntPtr args)
        {
            _setExitHandlerFunction.Delegate(InstancePointer, handler, args);
        }

        #endregion --- Methods ---
    }
}