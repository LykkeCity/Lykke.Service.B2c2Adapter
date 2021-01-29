using Antares.Sdk.Settings;
using JetBrains.Annotations;

namespace Lykke.Service.B2c2Adapter.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public B2c2AdapterSettings B2c2AdapterService { get; set; }
    }
}
