using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lykke.Service.B2c2Adapter.EntityFramework.Models
{
    [Table(Constants.BalancesTable, Schema = Constants.Schema)]
    public class BalanceEntity
    {
        public BalanceEntity()
        {
        }

        public string Asset { get; set; }

        public DateTime Timestamp { get; set; }

        public decimal Balance { get; set; }
    }
}
