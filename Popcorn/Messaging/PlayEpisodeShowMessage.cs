using System;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Models.Bandwidth;
using Popcorn.Models.Download;
using Popcorn.Models.Episode;

namespace Popcorn.Messaging
{
    /// <summary>
    /// Play an episode of a show
    /// </summary>
    public class PlayShowEpisodeMessage : MessageBase
    {
        /// <summary>
        /// Episode
        /// </summary>
        public readonly EpisodeShowJson Episode;

        /// <summary>
        /// The buffer progress
        /// </summary>
        public readonly Progress<double> BufferProgress;

        /// <summary>
        /// The download rate
        /// </summary>
        public readonly Progress<BandwidthRate> BandwidthRate;

        /// <summary>
        /// The playing progress
        /// </summary>
        public readonly IProgress<double> PlayingProgress;

        /// <summary>
        /// The piece availability progress
        /// </summary>
        public readonly Progress<PieceAvailability> PieceAvailability;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="episode">Episode</param>
        /// <param name="bufferProgress">The buffer progress</param>
        /// <param name="bandwidthRate">The bandwidth rate</param>
        /// <param name="playingProgress">The playing progress</param>
        /// <param name="pieceAvailability">The piece availability progress</param>
        public PlayShowEpisodeMessage(EpisodeShowJson episode, Progress<double> bufferProgress, Progress<BandwidthRate> bandwidthRate, IProgress<double> playingProgress, Progress<PieceAvailability> pieceAvailability)
        {
            Episode = episode;
            BufferProgress = bufferProgress;
            BandwidthRate = bandwidthRate;
            PlayingProgress = playingProgress;
            PieceAvailability = pieceAvailability;
        }
    }
}
