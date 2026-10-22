namespace Application.DataTransferObjects.Transactions.Commons.NS.Request;

public class TransferOrderLineRequestDTO : RequestDTO
{
    /// <summary>
    ///     Optional per-line vendor filter (NetSuite vendor internal id, i.e. the
    ///     value of <c>transactionline.custcol_dbti_vendor</c>). Set when the
    ///     operator scanned the vendor barcode printed on the STR; null returns
    ///     every line on the order.
    /// </summary>
    public int? VendorId { get; set; }
}
