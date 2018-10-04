using Lykke.HttpClientGenerator;

namespace Lykke.Service.B2c2Adapter.Client
{
    /// <summary>
    /// B2c2Adapter API aggregating interface.
    /// </summary>
    public class B2c2AdapterClient : IB2c2AdapterClient
    {
        // Note: Add similar Api properties for each new service controller

        /// <summary>Inerface to B2c2Adapter Api.</summary>
        public IB2c2AdapterApi Api { get; private set; }

        /// <summary>C-tor</summary>
        public B2c2AdapterClient(IHttpClientGenerator httpClientGenerator)
        {
            Api = httpClientGenerator.Generate<IB2c2AdapterApi>();
        }
    }
}
