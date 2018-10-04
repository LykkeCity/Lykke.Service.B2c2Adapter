using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.B2c2Adapter.Client 
{
    /// <summary>
    /// B2c2Adapter client settings.
    /// </summary>
    public class B2c2AdapterServiceClientSettings 
    {
        /// <summary>Service url.</summary>
        [HttpCheck("api/isalive")]
        public string ServiceUrl {get; set;}
    }
}
