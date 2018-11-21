using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lykke.B2c2Client.Models.Rest;

namespace Lykke.Service.B2c2Adapter.EntityFramework.Models
{
    [Table(Constants.LedgersTable, Schema = Constants.Schema)]
    public class LedgerEntity
    {
        public LedgerEntity()
        {
        }

        public LedgerEntity(LedgerLog ledger)
        {
            Update(ledger);
        }

        [Key]
        public string TransactionId { get; set; }

        public string Reference { get; set; }

        public string Currency { get; set; }

        public decimal Amount { get; set; }

        public string Type { get; set; }

        public DateTime Created { get; set; }

        public void Update(LedgerLog ledger)
        {
            TransactionId = ledger.TransactionId;
            Reference = ledger.Reference;
            Currency = ledger.Currency;
            Amount = ledger.Amount;
            Type = ledger.Type;
            Created = ledger.Created;
        }
    }
}
