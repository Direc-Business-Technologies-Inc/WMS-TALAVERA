namespace Application.DataTransferObjects.Others.NS;

public class NetsuiteTransactionApiDTO
{
    public List<Link> links { get; set; }

    public int count { get; set; }

    public bool hasMore { get; set; }

    public List<TransactionItem> items { get; set; }

    public int offset { get; set; }

    public int totalResults { get; set; }
}

public class NetSuiteResponse<T>
{
    public List<Link> links { get; set; }

    public int count { get; set; }

    public bool hasMore { get; set; }

    public List<T> items { get; set; }

    public int offset { get; set; }

    public int totalResults { get; set; }
    public int UnsyncedItemCount { get; set; }
}

public class Link
{

    public string rel { get; set; }

    public string href { get; set; }
}
public class Count
{
    List<Link> links { get; set; }
    public string unsynceditemcount { get; set; }
}

public class Brand
{
    public string brandid { get; set; }
    public string brandname { get; set; }
}
public class Category
{
    public string categoryid { get; set; }
    public string categoryname
    {
        get; set;
    }
}
public class SubCategory
{
    public string subcategoryid { get; set; }
    public string subcategoryname { get; set; }
}
public class Customer
{
    public int customerid { get; set; }
    public string customername { get; set; }
    public string customerentityid { get; set; }
}
public class OutOfSyncItems
{

    public string count { get; set; }
}
public class TransactionItem
{
    public List<Link> links { get; set; }

    //Sales or Transfer Order Line
    public string id { get; set; }

    public string orderdate { get; set; }

    public string ordernumber { get; set; }

    public string transactiontype { get; set; }
    public string totalquantity { get; set; }

    public string amount { get; set; }
    public string volume { get; set; }
    public string customerid { get; set; }
    public string customername { get; set; }

    //Items
    public string itemid { get; set; }
    public string displayname { get; set; }
    public string stockunit { get; set; }


    //Brand
    public string brandid { get; set; }
    public string brandname { get; set; }

    //ItemLine
    public string itemcode { get; set; }
    public string memo { get; set; }
    public string unit { get; set; }
    public string rate { get; set; }
    public string quantity { get; set; }
    public string totalprice { get; set; }
    public string category { get; set; }
    public string subcategory { get; set; }
    public string brand { get; set; }

    //Region
    public string region { get; set; }
    public string deliverydate { get; set; }
    //cluster

    public string cluster { get; set; }
}

public class OrderRelatedRecords
{
    public string orderid { get; set; }
    public string invoiceno { get; set; }
    public string drno { get; set; }
    public string fulfillmentid { get; set; }

}
