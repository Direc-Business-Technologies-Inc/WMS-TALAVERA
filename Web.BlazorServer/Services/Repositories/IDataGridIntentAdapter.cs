using Shared.Entities;

namespace Web.BlazorServer.Services.Repositories;

public interface IDataGridIntentAdapter
{
    protected IDataGridDescriptorOperatorAdapter OperatorAdapter { get; set; }
    DataGridIntent QueryIntent { get; }
    void AdaptToMyFilterQueries();
    void AdaptToMySortQueries();
    void AdaptToPagination();
    void ClearIntent();
    void AddFilter(AppFilterDescriptor filter);
    void AddFilters(List<AppFilterDescriptor> filters);
    void RemoveFilter(AppFilterDescriptor filter);
    void RemoveFilters(List<AppFilterDescriptor> filters);
    void ClearFilters();
}
