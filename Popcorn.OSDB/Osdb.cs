using System.Globalization;
using CookComputing.XmlRpc;
using Popcorn.OSDB.Backend;

namespace Popcorn.OSDB
{
    public class Osdb
    {
        private IOsdb ProxyInstance { get; set; }

        private IOsdb Proxy => ProxyInstance ?? (ProxyInstance = XmlRpcProxyGen.Create<IOsdb>());

        public IAnonymousClient Login(string userAgent)
        {
            var systemLanguage = GetSystemLanguage();
            return Login(systemLanguage, userAgent);
        }

        private IAnonymousClient Login(string language, string userAgent)
        {
            var client = new AnonymousClient(Proxy);
            client.Login(string.Empty, string.Empty, language, userAgent);
            return client;
        }

        private string GetSystemLanguage()
        {
            var currentCulture = CultureInfo.CurrentUICulture;
            return currentCulture.TwoLetterISOLanguageName.ToLower(CultureInfo.InvariantCulture);
        }
    }
}