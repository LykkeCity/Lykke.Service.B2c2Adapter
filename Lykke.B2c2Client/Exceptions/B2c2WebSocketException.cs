using System;
using Lykke.B2c2Client.Models.Rest;

namespace Lykke.B2c2Client.Exceptions
{
    public class B2c2WebSocketException : Exception
    {
        public ErrorResponse ErrorResponse { get; }

        public B2c2WebSocketException(ErrorResponse errorResponse)
        {
            ErrorResponse = errorResponse;
        }

        public B2c2WebSocketException(string message) : base(message)
        {
        }

        //public override string Message
        //{
        //    get
        //    {
        //        if (ErrorResponse != null)
        //            return $"{ErrorResponse.Errors.FirstOrDefault()?.Code} : {ErrorResponse.Errors.FirstOrDefault()?.Message}, guid: {RequestId}";

        //        return $"Message: '{base.Message}', guid: {RequestId}";
        //    }
        //}
    }
}
