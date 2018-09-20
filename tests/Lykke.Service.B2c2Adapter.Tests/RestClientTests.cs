using Lykke.B2c2Client;
using Lykke.Logs;
using Xunit;

namespace Lykke.Service.B2c2Adapter.Tests
{
    public class RestClientTests
    {
        private const string Url = "https://sandboxapi.b2c2.net/";
        private const string Token = "1bbe66d9dda462f37e7159c091e86994593b88d5";

        //[Fact]
        public async void BalanceTest()
        {
            var restClient = new B2c2RestClient(Url, Token, LogFactory.Create());

            var result = await restClient.BalanceAsync();
            
            Assert.NotEmpty(result);
            Assert.DoesNotContain("", result.Keys);
        }
    }
}
