using JetBrains.Annotations;

namespace Lykke.Service.B2c2Adapter.Client
{
    /// <summary>
    /// B2c2Adapter client interface.
    /// </summary>
    [PublicAPI]
    public interface IB2c2AdapterClient
    {
        // Make your app's controller interfaces visible by adding corresponding properties here.
        // NO actual methods should be placed here (these go to controller interfaces, for example - IB2c2AdapterApi).
        // ONLY properties for accessing controller interfaces are allowed.

        /// <summary>Application Api interface</summary>
        IB2c2AdapterApi Api { get; }
    }
}
