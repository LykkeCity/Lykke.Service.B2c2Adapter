using Lykke.B2c2Client.Models.WebSocket;

namespace Lykke.B2c2Client.Exceptions
{
    public class B2c2WebSocketAlreadySubscribedException : B2c2WebSocketException
    {
        public B2c2WebSocketAlreadySubscribedException(string message, ErrorResponse errorResponse) : base(message, errorResponse)
        {
        }
    }
}
