using B1SLayer;

namespace Integration.Sap.Repositories;

public interface IServiceLayer
{
    SLConnection Access { get; }
}
