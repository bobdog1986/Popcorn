using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.Services.Trakt
{
    public interface ITraktService
    {
        string GetAuthorizationUrl();
        Task AuthorizeAsync(string code);
        Task<bool> IsLoggedIn();
        Task Logout();
    }
}
