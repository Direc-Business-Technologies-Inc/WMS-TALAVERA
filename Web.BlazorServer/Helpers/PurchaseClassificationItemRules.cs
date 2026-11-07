using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Helpers;

public static class PurchaseClassificationItemRules
{
    public static bool IsSelected(PurchaseCategoryVM? category, PurchaseSubcategoryVM? subcategory) =>
        category is { Id: > 0 } && subcategory is { Id: > 0 };

    public static bool Matches(ItemsVM? item, PurchaseCategoryVM? category, PurchaseSubcategoryVM? subcategory) =>
        IsSelected(category, subcategory) && item is not null &&
        item.PurchaseCategoryId == category!.Id && item.PurchaseSubcategoryId == subcategory!.Id;

    public static List<AppFilterDescriptor> Filters(PurchaseCategoryVM category, PurchaseSubcategoryVM subcategory) =>
    [
        DataGridFilterUtilities.Equal(nameof(ItemsVM.PurchaseCategoryId), category.Id),
        DataGridFilterUtilities.Equal(nameof(ItemsVM.PurchaseSubcategoryId), subcategory.Id)
    ];

    // Re-read classification at submission so stale lines/item-master changes cannot
    // bypass the picker and scanner checks. Batching avoids the SuiteQL page limit.
    public static async Task<bool> ValidateItemsAsync(IItemsHandler handler, IEnumerable<int> itemIds,
        PurchaseCategoryVM? category, PurchaseSubcategoryVM? subcategory)
    {
        if (!IsSelected(category, subcategory)) return false;

        foreach (var batch in itemIds.Distinct().Chunk(200))
        {
            var filters = Filters(category!, subcategory!);
            filters.Add(DataGridFilterUtilities.In(nameof(ItemsVM.Id), batch));
            var result = await handler.GetItemsDataGridAsync(new DataGridIntent
            {
                Filters = filters,
                Take = batch.Length
            });
            var eligibleIds = result.Data
                .Where(item => Matches(item, category, subcategory))
                .Select(item => item.Id).ToHashSet();
            if (batch.Any(id => !eligibleIds.Contains(id))) return false;
        }

        return true;
    }
}
