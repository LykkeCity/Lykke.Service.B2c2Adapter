namespace Lykke.Service.B2c2Adapter.Settings
{
    public sealed class InstrumentLevels
    {
        public string Instrument { get; set; }

        public decimal[] Levels { get; set; } = new decimal[0];
    }
}
