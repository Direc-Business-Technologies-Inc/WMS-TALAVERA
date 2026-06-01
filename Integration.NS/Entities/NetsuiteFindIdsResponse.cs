namespace Integration.NS.Entities;

public class NetSuiteFindIdsResponse
{
    public int count { get; set; }

    public List<Item> items { get; set; }

    public class Item
    {
        public string id { get; set; }
    }
}
