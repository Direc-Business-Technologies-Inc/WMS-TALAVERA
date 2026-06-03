namespace Application.DataTransferObjects.Others.NS;

public class ItemBarcodesPerUoMDTO
{
    public int MaterialInternalId { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialName { get; set; }
    public string MaterialBarcode { get; set; }

    public string UoMName { get; set; }
    public int UoMRate { get; set; }
}
