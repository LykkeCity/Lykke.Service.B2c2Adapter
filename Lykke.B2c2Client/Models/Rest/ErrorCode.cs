namespace Lykke.B2c2Client.Models.Rest
{
    public enum ErrorCode
    {
        None = 0,
        GenericUnknownError = 1000,
        InstrumentNotAllowed = 1001,
        TheRfqDoesNotBelongToYou = 1002,
        DifferentInstrument = 1003,
        DifferentSide = 1004,
        DifferentPrice = 1005,
        DifferentQuantity = 1006,
        QuoteIsNotValid = 1007,
        PriceNotValid = 1009,
        QuantityTooBig = 1010,
        NotEnoughBalance = 1011,
        MaxRiskExposureReached = 1012,
        MaxCreditExposureReached = 1013,
        NoBtcAddressAssociated = 1014,
        TooManyDecimals = 1015,
        TradingIsDisabled = 1016,
        IllegalParameter = 1017,
        SettlementIsDisabled = 1018,
        QuantityIsTooSmall = 1019,
        TheFieldValidUntilIsMalformed = 1020,
        OrderHasExpired = 1021,
        CurrencyNotAllowed = 1022,
        OnlySupportFok = 1023,
        FieldRequired = 1101,
        ThisContractIsAlreadyClosed = 1500,
        TheGivenQuantityMustBeSmallerOrEqualToTheContractQuantity = 1501,
        NotHaveEnoughMargin = 1502,
        ContractUpdatesAreOnlyForClosingAContract = 1503,
        OtherError = 1100
    }
}
