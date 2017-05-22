using GalaSoft.MvvmLight;
using RestSharp.Deserializers;

namespace Popcorn.Models.Torrent.Movie
{
    public class TorrentJson : ObservableObject
    {
        private string _dateUploaded;

        private int _dateUploadedUnix;

        private string _hash;

        private int _peers;

        private string _quality;

        private int _seeds;

        private string _size;

        private long? _sizeBytes;

        private string _url;

        [DeserializeAs(Name = "url")]
        public string Url
        {
            get => _url;
            set { Set(() => Url, ref _url, value); }
        }

        [DeserializeAs(Name = "hash")]
        public string Hash
        {
            get => _hash;
            set { Set(() => Hash, ref _hash, value); }
        }

        [DeserializeAs(Name = "quality")]
        public string Quality
        {
            get => _quality;
            set { Set(() => Quality, ref _quality, value); }
        }

        [DeserializeAs(Name = "seeds")]
        public int Seeds
        {
            get => _seeds;
            set { Set(() => Seeds, ref _seeds, value); }
        }

        [DeserializeAs(Name = "peers")]
        public int Peers
        {
            get => _peers;
            set { Set(() => Peers, ref _peers, value); }
        }

        [DeserializeAs(Name = "size")]
        public string Size
        {
            get => _size;
            set { Set(() => Size, ref _size, value); }
        }

        [DeserializeAs(Name = "size_bytes")]
        public long? SizeBytes
        {
            get => _sizeBytes;
            set { Set(() => SizeBytes, ref _sizeBytes, value); }
        }

        [DeserializeAs(Name = "date_uploaded")]
        public string DateUploaded
        {
            get => _dateUploaded;
            set { Set(() => DateUploaded, ref _dateUploaded, value); }
        }

        [DeserializeAs(Name = "date_uploaded_unix")]
        public int DateUploadedUnix
        {
            get => _dateUploadedUnix;
            set { Set(() => DateUploadedUnix, ref _dateUploadedUnix, value); }
        }
    }
}