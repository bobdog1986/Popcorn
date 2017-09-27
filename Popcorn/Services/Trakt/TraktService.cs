using System;
using System.Reactive.Linq;
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

            try
            {
                return !await _client.Authentication.CheckIfAccessTokenWasRevokedOrIsNotValidAsync(accessToken);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        public async Task Logout()
        {
            await _client.OAuth.RevokeAuthorizationAsync(await GetAccessToken());
            await BlobCache.UserAccount.Invalidate("trakt");
            await BlobCache.UserAccount.Flush();
        }

        public string GetAuthorizationUrl()
        {
            try
            {
                return _client.OAuth.CreateAuthorizationUrl();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return string.Empty;
            }
        }

        public async Task AuthorizeAsync(string code)
        {
            try
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
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }
    }
}