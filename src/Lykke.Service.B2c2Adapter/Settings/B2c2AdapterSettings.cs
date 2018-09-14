using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.B2c2Adapter.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class B2c2AdapterSettings
    {
        public DbSettings Db { get; set; }
    }
}
