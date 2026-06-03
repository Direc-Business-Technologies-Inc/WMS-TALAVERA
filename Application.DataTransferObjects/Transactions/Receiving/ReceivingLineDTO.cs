using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;
using Domain.Entities.Enums.Transaction.Receiving;
using System.Numerics;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class ReceivingLineDTO
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal WeightReceived { get; set; }
    public decimal WeightTotal { get; set; }
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityOpen { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityBad { get; set; }
}