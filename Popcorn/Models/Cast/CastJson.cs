using GalaSoft.MvvmLight;
using RestSharp.Deserializers;

namespace Popcorn.Models.Cast
{
    public class CastJson : ObservableObject
    {
        private string _characterName;
        private string _name;
        private string _smallImage;
        private string _imdbCode;

        [DeserializeAs(Name = "name")]
        public string Name
        {
            get => _name;
            set { Set(() => Name, ref _name, value); }
        }

        [DeserializeAs(Name = "character_name")]
        public string CharacterName
        {
            get => _characterName;
            set { Set(() => CharacterName, ref _characterName, value); }
        }

        [DeserializeAs(Name = "url_small_image")]
        public string SmallImage
        {
            get => _smallImage;
            set { Set(() => SmallImage, ref _smallImage, value); }
        }
        
        [DeserializeAs(Name = "imdb_code")]
        public string ImdbCode
        {
            get => _imdbCode;
            set { Set(() => ImdbCode, ref _imdbCode, value); }
        }
    }
}