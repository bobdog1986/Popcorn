using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using NLog;
using Popcorn.Models.Trakt;
using TraktApiSharp;
using TraktApiSharp.Authentication;

namespace Popcorn.Services.Trakt
{
    public class TraktService : ITraktService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private readonly TraktClient _client;

        private TraktAuthorization _traktAuthorization;

        public TraktService()
        {
            _client = new TraktClient(Utils.Constants.TraktClientApiKey, Utils.Constants.TraktSecretKey);
        }

        private async Task<string> GetAccessToken()
        {
            var accessToken = string.Empty;
            try
            {
                var token = await BlobCache.UserAccount.GetObject<TraktToken>("trakt");
                accessToken = token.AccessToken;
            }
            catch (Exception)
            {
                
            }

            return accessToken;
        }

        public async Task<bool> IsLoggedIn()
        {
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken)) return false;

            return !await _client.Authentication.CheckIfAccessTokenWasRevokedOrIsNotValidAsync(accessToken);
        }

        public string GetAuthorizationUrl()
        {
            return _client.OAuth.CreateAuthorizationUrl();
        }

        public async Task AuthorizeAsync(string code)
        {
            _traktAuthorization = await _client.OAuth.GetAuthorizationAsync(code);
            await BlobCache.UserAccount.InsertObject("trakt", new TraktToken
            {
                AccessToken = _traktAuthorization.AccessToken,
                Created = _traktAuthorization.Created,
                ExpiresInSeconds = _traktAuthorization.ExpiresInSeconds,
                RefreshToken = _traktAuthorization.RefreshToken
            });
            await BlobCache.UserAccount.Flush();
        }
    }
}
