using Lykke.B2c2Client;
using Xunit;

namespace Lykke.Service.B2c2Adapter.Tests
{
    public class RestClientTests
    {
        [Fact]
        public void Test()
        {
            var restClient = new RestClient("1bbe66d9dda462f37e7159c091e86994593b88d5");

            var result = restClient.GetBalance();
        }
    }
}
