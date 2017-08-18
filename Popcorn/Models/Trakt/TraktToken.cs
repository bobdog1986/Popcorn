using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.Models.Trakt
{
    public class TraktToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Created { get; set; }
        public int ExpiresInSeconds { get; set; }
    }
}
