namespace Shared.Entities;

public sealed class GridAdmissionBusyException : Exception
{
    public GridAdmissionBusyException() : base("NetSuite is busy. Please retry the refresh.") { }
}
