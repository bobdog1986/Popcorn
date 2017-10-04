using Popcorn.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using lt;
using NLog;
using System.IO;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Bandwidth;
using Popcorn.Models.Media;
using Popcorn.Services.Cache;
using Popcorn.Utils.Exceptions;

namespace Popcorn.Services.Download
{
    /// <summary>
    /// Generic download service for torrent download
    /// </summary>
    /// <typeparam name="T"><see cref="IMediaFile"/></typeparam>
    public class DownloadService<T> : IDownloadService<T> where T : IMediaFile
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private readonly ICacheService _cacheService;

        public DownloadService(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Action to execute when a movie has been buffered
        /// </summary>
        /// <param name="media"><see cref="IMediaFile"/></param>
        /// <param name="reportDownloadProgress">Download progress</param>
        /// <param name="reportDownloadRate">The download rate</param>
        protected virtual void BroadcastMediaBuffered(T media, Progress<double> reportDownloadProgress,
            Progress<BandwidthRate> reportDownloadRate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Download a torrent
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        public async Task Download(T media, TorrentType torrentType, MediaType mediaType, string torrentPath,
            int uploadLimit, int downloadLimit, IProgress<double> downloadProgress,
            IProgress<BandwidthRate> bandwidthRate, IProgress<int> nbSeeds, IProgress<int> nbPeers, Action buffered,
            Action cancelled,
            CancellationTokenSource cts)
        {
            Logger.Info(
                $"Start downloading : {torrentPath}");
            await Task.Run(async () =>
            {
                using (var session = new session())
                {
                    downloadProgress.Report(0d);
                    bandwidthRate.Report(new BandwidthRate
                    {
                        DownloadRate = 0d,
                        UploadRate = 0d
                    });
                    nbSeeds.Report(0);
                    nbPeers.Report(0);
                    string savePath = string.Empty;
                    switch (mediaType)
                    {
                        case MediaType.Movie:
                            savePath = _cacheService.MovieDownloads;
                            break;
                        case MediaType.Show:
                            savePath = _cacheService.ShowDownloads;
                            break;
                        case MediaType.Unkown:
                            savePath = _cacheService.DropFilesDownloads;
                            break;
                    }

                    if (torrentType == TorrentType.File)
                    {
                        using (var addParams = new add_torrent_params
                        {
                            save_path = savePath,
                            ti = new torrent_info(torrentPath)
                        })
                        using (var handle = session.add_torrent(addParams))
                        {
                            await HandleDownload(media, mediaType, uploadLimit, downloadLimit, downloadProgress,
                                bandwidthRate, nbSeeds, nbPeers, handle, session, buffered, cancelled, cts);
                        }
                    }
                    else
                    {
                        var magnet = new magnet_uri();
                        using (var error = new error_code())
                        {
                            var addParams = new add_torrent_params
                            {
                                save_path = savePath,
                            };
                            magnet.parse_magnet_uri(torrentPath, addParams, error);
                            using (var handle = session.add_torrent(addParams))
                            {
                                await HandleDownload(media, mediaType, uploadLimit, downloadLimit, downloadProgress,
                                    bandwidthRate, nbSeeds, nbPeers, handle, session, buffered, cancelled, cts);
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Download media
        /// </summary>
        /// <param name="media">Media file <see cref="IMediaFile"/></param>
        /// <param name="type">Media type <see cref="MediaType"/></param>
        /// <param name="uploadLimit">Upload limit</param>
        /// <param name="downloadLimit">Download limit</param>
        /// <param name="downloadProgress">Download progress</param>
        /// <param name="bandwidthRate">Download rate</param>
        /// <param name="nbSeeds">Number of seeders</param>
        /// <param name="nbPeers">Number of peers</param>
        /// <param name="handle"><see cref="torrent_handle"/></param>
        /// <param name="session"><see cref="session"/></param>
        /// <param name="buffered">Action to execute when media has been buffered</param>
        /// <param name="cancelled">Action to execute when media download has been cancelled</param>
        /// <param name="cts"><see cref="CancellationTokenSource"/></param>
        /// <returns><see cref="Task"/></returns>
        private async Task HandleDownload(T media, MediaType type, int uploadLimit, int downloadLimit,
            IProgress<double> downloadProgress,
            IProgress<BandwidthRate> bandwidthRate, IProgress<int> nbSeeds, IProgress<int> nbPeers,
            torrent_handle handle,
            session session, Action buffered, Action cancelled, CancellationTokenSource cts)
        {
            handle.set_upload_limit(uploadLimit * 1024);
            handle.set_download_limit(downloadLimit * 1024);
            handle.set_sequential_download(true);
            var alreadyBuffered = false;
            var bandwidth = new Progress<BandwidthRate>();
            var prog = new Progress<double>();
            while (!cts.IsCancellationRequested)
            {
                using (var status = handle.status())
                {
                    var progress = status.progress * 100d;
                    var downRate = Math.Round(status.download_rate / 1024d, 0);
                    var upRate = Math.Round(status.upload_rate / 1024d, 0);

                    nbSeeds.Report(status.num_seeds);
                    nbPeers.Report(status.num_peers);
                    downloadProgress.Report(progress);
                    bandwidthRate.Report(new BandwidthRate
                    {
                        DownloadRate = downRate,
                        UploadRate = upRate
                    });

                    ((IProgress<double>) prog).Report(progress);
                    ((IProgress<BandwidthRate>) bandwidth).Report(new BandwidthRate
                    {
                        DownloadRate = downRate,
                        UploadRate = upRate
                    });

                    handle.flush_cache();
                    if (handle.need_save_resume_data())
                        handle.save_resume_data(1);

                    double minimumBuffering;
                    switch (type)
                    {
                        case MediaType.Show:
                            minimumBuffering = Constants.MinimumShowBuffering;
                            break;
                        default:
                            minimumBuffering = Constants.MinimumMovieBuffering;
                            break;
                    }

                    if (progress >= minimumBuffering && !alreadyBuffered)
                    {
                        buffered.Invoke();
                        var filePath =
                            Directory
                                .GetFiles(status.save_path, "*.*",
                                    SearchOption.AllDirectories)
                                .FirstOrDefault(s => s.Contains(handle.torrent_file().name()) &&
                                                     (s.EndsWith(".mp4") || s.EndsWith(".mkv") ||
                                                      s.EndsWith(".mov") || s.EndsWith(".avi")));
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            alreadyBuffered = true;
                            media.FilePath = filePath;
                            BroadcastMediaBuffered(media, prog, bandwidth);
                        }

                        if (!alreadyBuffered)
                        {
                            session.remove_torrent(handle, 0);
                            if (type == MediaType.Unkown)
                            {
                                Messenger.Default.Send(
                                    new UnhandledExceptionMessage(
                                        new PopcornException(
                                            LocalizationProviderHelper.GetLocalizedValue<string>(
                                                "NoMediaInDroppedTorrent"))));
                            }
                            else
                            {
                                Messenger.Default.Send(
                                    new UnhandledExceptionMessage(
                                        new PopcornException(
                                            LocalizationProviderHelper.GetLocalizedValue<string>("NoMediaInTorrent"))));
                            }

                            break;
                        }
                    }

                    if (status.is_finished)
                    {
                        session.remove_torrent(handle, 0);
                        ((IProgress<BandwidthRate>)bandwidth).Report(new BandwidthRate
                        {
                            DownloadRate = 0d,
                            UploadRate = 0d
                        });
                        break;
                    }

                    try
                    {
                        await Task.Delay(1000, cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        cancelled.Invoke();
                        break;
                    }
                }
            }
        }
    }
}