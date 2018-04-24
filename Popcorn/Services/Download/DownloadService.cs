using Popcorn.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ltnet;
using NLog;
using System.IO;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Extensions;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Bandwidth;
using Popcorn.Models.Download;
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

        protected DownloadService(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Action to execute when a movie has been buffered
        /// </summary>
        /// <param name="media"><see cref="IMediaFile"/></param>
        /// <param name="reportDownloadProgress">Download progress</param>
        /// <param name="reportDownloadRate">The download rate</param>
        /// <param name="playingProgress">The playing progress</param>
        /// <param name="reportPieceAvailability">Report the piece availability progress</param>
        protected virtual void BroadcastMediaBuffered(T media, Progress<double> reportDownloadProgress,
            Progress<BandwidthRate> reportDownloadRate, IProgress<double> playingProgress,
            Progress<PieceAvailability> reportPieceAvailability)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Download a torrent
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        public Task Download(T media, TorrentType torrentType, MediaType mediaType, string torrentPath,
            int uploadLimit, int downloadLimit, IProgress<double> downloadProgress,
            IProgress<BandwidthRate> bandwidthRate, IProgress<int> nbSeeds, IProgress<int> nbPeers, Action buffered,
            Action cancelled,
            CancellationTokenSource cts)
        {
            return Task.Run(async () =>
            {
                Logger.Info(
                    $"Start downloading : {torrentPath}");
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
            IProgress<double> downloadProgress, IProgress<BandwidthRate> bandwidthRate, IProgress<int> nbSeeds,
            IProgress<int> nbPeers,
            torrent_handle handle,
            session session, Action buffered, Action cancelled, CancellationTokenSource cts)
        {
            handle.set_upload_limit(uploadLimit * 1024);
            handle.set_download_limit(downloadLimit * 1024);
            var alreadyBuffered = false;
            var bandwidth = new Progress<BandwidthRate>();
            var prog = new Progress<double>();
            var playingProgress = new Progress<double>();
            var playingProgression = 0d;
            playingProgress.ProgressChanged += (sender, d) =>
            {
                playingProgression = d;
            };

            IProgress<PieceAvailability> pieceAvailability = new Progress<PieceAvailability>();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (!cts.IsCancellationRequested)
            {
                using (var status = handle.status())
                {
                    var filePath = string.Empty;
                    var progress = status.progress * 100d;
                    if (status.has_metadata)
                    {
                        var downRate = Math.Round(status.download_rate / 1024d, 0);
                        var upRate = Math.Round(status.upload_rate / 1024d, 0);

                        nbSeeds.Report(status.num_seeds);
                        nbPeers.Report(status.num_peers);
                        downloadProgress.Report(progress);
                        var numFiles = handle.torrent_file().num_files();
                        var fileIndex = -1;
                        for (var i = 0; i < numFiles; i++)
                        {
                            var path = handle.torrent_file().file_at(i);
                            if (path.EndsWith(".mp4") || path.EndsWith(".mkv") ||
                                path.EndsWith(".mov") || path.EndsWith(".avi"))
                            {
                                fileIndex = i;
                                filePath = $@"{Directory.GetParent(status.save_path)}\{path}";
                            }
                        }

                        var fileProgress = handle.file_progress(1)[fileIndex];
                        var eta = sw.GetEta(fileProgress, handle.torrent_file().total_size());
                        bandwidthRate.Report(new BandwidthRate
                        {
                            DownloadRate = downRate,
                            UploadRate = upRate,
                            ETA = eta
                        });

                        ((IProgress<double>) prog).Report(progress);
                        ((IProgress<BandwidthRate>) bandwidth).Report(new BandwidthRate
                        {
                            DownloadRate = downRate,
                            UploadRate = upRate,
                            ETA = eta
                        });

                        var numPieces = handle.torrent_file().num_pieces() - 1;
                        double minBuffer;
                        switch (type)
                        {
                            case MediaType.Movie:
                                minBuffer = Constants.MinimumMovieBuffering / 100d;
                                break;
                            case MediaType.Show:
                                minBuffer = Constants.MinimumShowBuffering / 100d;
                                break;
                            default:
                                minBuffer = 0.03d;
                                break;
                        }
                        var cursor = Math.Floor((numPieces - minBuffer * numPieces) * playingProgression);
                        var pieces = handle.piece_priorities()
                            .Select((piece, index) => new {Piece = piece, Index = index})
                            .ToList();

                        var lastPieceAvailableIndex = 0;
                        foreach (var piece in pieces.Where(a => a.Index >= cursor))
                        {
                            if (!handle.have_piece(piece.Index))
                            {
                                handle.set_piece_deadline(piece.Index, 50);
                                foreach (var otherPiece in pieces.Where(a => a.Index != piece.Index))
                                {
                                    handle.reset_piece_deadline(otherPiece.Index);
                                }

                                break;
                            }

                            lastPieceAvailableIndex = piece.Index;
                            handle.reset_piece_deadline(piece.Index);
                        }

                        pieceAvailability.Report(new PieceAvailability(numPieces,
                            pieces.First(a => a.Index >= cursor - minBuffer * numPieces).Index,
                            lastPieceAvailableIndex));
                    }

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
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            alreadyBuffered = true;
                            media.FilePath = filePath;
                            BroadcastMediaBuffered(media, prog, bandwidth, playingProgress,
                                (Progress<PieceAvailability>) pieceAvailability);
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

                    try
                    {
                        await Task.Delay(1000, cts.Token);
                    }
                    catch (Exception ex) when (ex is TaskCanceledException || ex is ObjectDisposedException)
                    {
                        cancelled.Invoke();
                        sw.Stop();
                        try
                        {
                            session.remove_torrent(handle, 1);
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                        break;
                    }
                }
            }
        }
    }
}