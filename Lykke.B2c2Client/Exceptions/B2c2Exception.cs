using System;
using Lykke.B2c2Client.Models.Rest;

namespace Lykke.B2c2Client.Exceptions
{
    public class B2c2Exception : Exception
    {
        public ErrorResponse ErrorResponse { get; set; }

        public B2c2Exception(ErrorResponse errorResponse)
        {
            ErrorResponse = errorResponse;
        }
    }
}
