using System;

namespace Lykke.B2c2Client.Settings
{
    public class B2C2ClientSettings
    {
        public string Url { get; }
        public string AuthorizationToken { get; }

        public B2C2ClientSettings(string url, string authorizationToken)
        {
            Url = string.IsNullOrWhiteSpace(url) ? throw new ArgumentOutOfRangeException(nameof(url)) : url;
            AuthorizationToken = string.IsNullOrWhiteSpace(authorizationToken) ? throw new ArgumentOutOfRangeException(nameof(authorizationToken)) : authorizationToken;
        }
    }
}
