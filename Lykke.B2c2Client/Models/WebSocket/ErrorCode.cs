namespace Lykke.B2c2Client.Models.WebSocket
{
    public enum ErrorCode
    {
        None = 0,
        AuthenticationFailure = 3000,
        AuthorizationNotInTheHeaders = 3001,
        EndpointDoesNotExist = 3002,
        InstrumentIsNotAllowed = 3003,
        SubscriptionIsInvalid = 3004,
        UnableToJsoniseYourMessage = 3005,
        AlreadyConnected = 3006,
        AlreadySubscribed = 3007,
        NotSubscribedYet = 3008,
        InvalidFormat = 3009,
        InvalidMessage = 3011,
        NotAbleToQuoteAtTheMoment = 3013,
        UnexpectedError = 3014,
        UsernameChanged = 3015,
        ConnectivityIssues = 3016,
        AuthorizationHeaderIsMalformed = 3017,
        SubscriptionForALevelIsNotValidAnymore = 3018,
        TheGivenInstrumentDoesNotEndWithSpotOrCfd = 3019,
        GenericError = 4000
    }
}
