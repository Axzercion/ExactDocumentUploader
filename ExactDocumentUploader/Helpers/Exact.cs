using DotNetOpenAuth.OAuth2;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ExactDocumentUploader.Helpers
{
    public static class Exact
    {
        /// <summary>
        /// Create the web server client used for OAuth2 authentication.
        /// </summary>
        /// <returns>Exact Online web server client</returns>
        public static WebServerClient CreateClient()
        {
            AuthorizationServerDescription description = GetAuthServerDescription();
            WebServerClient client = new WebServerClient(description, ConfigurationManager.AppSettings["exactClientId"], ConfigurationManager.AppSettings["exactClientSecret"]);
            return client;
        }

        private static AuthorizationServerDescription GetAuthServerDescription()
        {
            string url = ConfigurationManager.AppSettings["exactOnlineUrl"];
            return new AuthorizationServerDescription
            {
                AuthorizationEndpoint = new Uri(string.Format("{0}/api/oauth2/auth", url)),
                TokenEndpoint = new Uri(string.Format("{0}/api/oauth2/token", url))
            };
        }
    }
}