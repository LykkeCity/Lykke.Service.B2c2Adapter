using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Lykke.Service.B2c2Adapter.EntityFramework;
using Lykke.Service.B2c2Adapter.EntityFramework.Models;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "";

            var csvPath = @"C:\Temp\trade_history.csv";

            var fileContent = File.ReadAllLines(csvPath);

            var batch = new List<TradeEntity>();

            var page = 100;

            for (var i=1; i < fileContent.Length; i++)
            {
                var line = fileContent[i];

                var trade = Parse(line);

                batch.Add(trade);

                if (i % page == 0 || i == fileContent.Length - 1)
                {
                    using var context = new ReportContext(connectionString);

                    foreach (var tradeEntity in batch)
                    {
                        context.Trades.Add(tradeEntity);
                    }

                    try
                    {
                        await context.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);

                        throw;
                    }

                    batch.Clear();

                    Console.WriteLine($"Saved {i} trades");
                }
            }

            Console.WriteLine("Done!");
        }

        static TradeEntity Parse(string str)
        {
            var result = new TradeEntity();

            var fields = str.Split(',');

            result.TradeId = fields[0];
            result.User = fields[1];
            result.Price = decimal.Parse(fields[2], CultureInfo.InvariantCulture);
            result.Volume = decimal.Parse(fields[3], CultureInfo.InvariantCulture);
            result.Direction = fields[4];
            result.AssetPair = fields[5];
            result.RequestForQuoteId = "";
            result.Created = DateTime.Parse(fields[8], CultureInfo.InvariantCulture);

            return result;
        }
    }
}
