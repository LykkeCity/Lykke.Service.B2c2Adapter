namespace Lykke.B2c2Client.Models.Rest
{
    public class PaginationResponse<T>
    {
        public T Data { get; set; }
        public string Previous { get; set; }
        public string Next { get; set; }
    }
}
