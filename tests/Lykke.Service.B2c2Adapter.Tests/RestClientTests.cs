using System;
using System.Linq;
using System.Net.Http;
using Lykke.B2c2Client;
using Lykke.B2c2Client.Models.Rest;
using Lykke.B2c2Client.Settings;
using Lykke.Logs;
using Moq;
using Xunit;

namespace Lykke.Service.B2c2Adapter.Tests
{
    public class RestClientTests
    {
        private const string Url = "https://api.uat.b2c2.net";
        private const string Token = "";
        private readonly IB2С2RestClient _restClient = new B2С2RestClient(new B2C2ClientSettings(Url, Token), new Mock<IHttpClientFactory>().Object, LogFactory.Create());

        //[Fact]
        public async void InstrumentsTest()
        {
            var result = await _restClient.InstrumentsAsync();

            Assert.NotEmpty(result);
            Assert.DoesNotContain(result, x => string.IsNullOrEmpty(x.Name));
        }

        //[Fact]
        public async void BalanceTest()
        {
            var result = await _restClient.BalanceAsync();

            Assert.NotEmpty(result);
            Assert.DoesNotContain(result, x => string.IsNullOrEmpty(x.Key));
        }

        //[Fact]
        public async void RequestForQuoteTest()
        {
            var rfqRequest = new RequestForQuoteRequest("BTCUSD", Side.Buy, 1);
            var result = await _restClient.RequestForQuoteAsync(rfqRequest);

            Assert.NotNull(result);
            Assert.Equal(rfqRequest.Instrument + ".SPOT", result.Instrument);
            Assert.Equal(rfqRequest.Quantity, result.Quantity);
            Assert.Equal(rfqRequest.ClientRfqId, result.ClientRfqId);
            Assert.Equal(rfqRequest.Side, result.Side);
            Assert.True(result.Price > 0);
            Assert.NotEmpty(result.Id);
            Assert.NotEqual(default(DateTime), result.ValidUntil);
        }

        //[Fact]
        public async void TradeTest()
        {
            var rfqRequest = new RequestForQuoteRequest("BTCUSD", Side.Buy, 1);
            var rfqResponse = await _restClient.RequestForQuoteAsync(rfqRequest);
            var result = await _restClient.TradeAsync(new TradeRequest(rfqResponse));

            Assert.NotNull(result);
            Assert.NotEmpty(result.TradeId);
            Assert.Equal(result.RfqId, rfqResponse.Id);
            Assert.Equal(result.Instrument, rfqResponse.Instrument);
            Assert.Equal(result.Side, rfqResponse.Side);
            Assert.Equal(result.Price, rfqResponse.Price);
            Assert.Equal(result.Quantity, rfqResponse.Quantity);
            Assert.Null(result.Order);
            Assert.NotEqual(default(DateTime), result.Created);
        }

        //[Fact]
        public async void OrderTest()
        {
            // arrange

            var orderRequest = new OrderRequest();
            orderRequest.ClientOrderId = Guid.NewGuid().ToString();
            orderRequest.Instrument = "BTCUSD.SPOT";
            orderRequest.Side = Side.Buy;
            orderRequest.Quantity = 0.01m;
            orderRequest.OrderType = OrderType.MKT;
            orderRequest.ValidUntil = DateTime.UtcNow.AddSeconds(10);
            orderRequest.AcceptableSlippage = 0;

            // act

            var orderResponse = await _restClient.OrderAsync(orderRequest);

            // assert

            Assert.NotNull(orderResponse);
            Assert.NotEmpty(orderResponse.OrderId);
            Assert.Equal(orderResponse.ClientOrderId, orderRequest.ClientOrderId);
            Assert.Equal(orderResponse.Instrument, orderRequest.Instrument);
            Assert.Equal(orderResponse.Side, orderRequest.Side);
            Assert.NotEqual(0, orderResponse.Price);
            Assert.NotEqual(default(decimal), orderResponse.ExecutedPrice);
            Assert.Equal(orderResponse.Quantity, orderRequest.Quantity);
            Assert.NotEqual(default(DateTime), orderResponse.Created);
            Assert.NotEmpty(orderResponse.Trades);

            var trade = orderResponse.Trades.Single();
            Assert.NotEmpty(trade.TradeId);
            Assert.NotEqual(default(string), trade.TradeId);
            Assert.Equal(trade.Instrument, orderRequest.Instrument);
            Assert.Equal(trade.Side, orderRequest.Side);
            Assert.NotEqual(default(decimal), trade.Price);
            Assert.Equal(trade.Quantity, orderRequest.Quantity);
            Assert.Equal(trade.Order, orderResponse.OrderId);
            Assert.NotEqual(orderResponse.Created, trade.Created);
            Assert.Null(trade.RfqId);
        }

        //[Fact]
        public async void LedgerTest()
        {
            var result = await _restClient.GetLedgerHistoryAsync();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            foreach (var ledgerLog in result)
            {
                Assert.NotEmpty(ledgerLog.TransactionId);
                Assert.NotEmpty(ledgerLog.Reference);
                Assert.NotEmpty(ledgerLog.Currency);
                Assert.NotEqual(0, ledgerLog.Amount);
                Assert.NotEmpty(ledgerLog.Type);
                Assert.NotEqual(default(DateTime), ledgerLog.Created);
            }
        }
    }
}
