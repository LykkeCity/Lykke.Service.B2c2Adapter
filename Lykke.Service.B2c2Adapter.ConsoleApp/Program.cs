using System;
using System.Threading.Tasks;
using Lykke.B2c2Client;
using Lykke.B2c2Client.Models.Rest;
using Lykke.Logs;

namespace Lykke.Service.B2c2Adapter.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var restClient = new B2C2RestClient("1bbe66d9dda462f37e7159c091e86994593b88d5", LogFactory.Create());

            //var restClient = new B2C2RestClient("1bbe66d9dda462f37e7159c091e86994593b88d1", LogFactory.Create());

            Task.Run(async () =>
                {
                    //var balance = await restClient.GetInstruments();
                    //var instruments = await restClient.GetInstruments();

                    var rfq = await restClient.RequestForQuote(new RequestForQuoteRequest
                    {
                        ClientRfqId = Guid.NewGuid().ToString(),
                        Instrument = "BTCUSD.SPOT",
                        Quantity = 1,
                        Side = Side.Buy
                    });



                    var temp = 0;
                })
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        throw t.Exception;
                });
            
            Console.ReadLine();
        }
    }
}
