namespace Web.BlazorServer.Components.Custom.Utilities;


[System.AttributeUsage(System.AttributeTargets.Property)]
public class QuickDataGridIgnore : System.Attribute
{
}


[System.AttributeUsage(System.AttributeTargets.Property)]
public class QuickDataGridTitle : System.Attribute
{
    public string Title;
    public QuickDataGridTitle(string title)
    {
        Title = title;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class QuickDataGridStringFormat : Attribute
{
    public string Format;
    public QuickDataGridStringFormat(string format) { Format = format; }
}
