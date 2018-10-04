using Newtonsoft.Json;

namespace Lykke.B2c2Client.Models.WebSocket
{
    public class QuantityPrice
    {
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        public QuantityPrice(double quantity, double price)
        {
            Quantity = (decimal)quantity;
            Price = (decimal)price;
        }
    }
}
