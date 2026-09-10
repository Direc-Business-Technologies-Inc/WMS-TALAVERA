namespace Mobile.MAUI.Components.Reusables;

public partial class ManualEntryDialog
{
    [Inject] DialogService DialogService { get; set; } = default!;

    [Parameter] public string ItemName { get; set; } = string.Empty;
    [Parameter] public decimal PlannedQty { get; set; }
    [Parameter] public int ShowBad { get; set; } = 0; // if zero then show.
    [Parameter] public int ShowMissing { get; set; } = 0; // if zero then show.
    [Parameter] public decimal GoodQty { get; set; } = 0;
    [Parameter] public decimal BadQty { get; set; } = 0;

    private bool ShowBadIfZero => ShowBad != 1;
    private bool ShowMissingIfZero => ShowMissing != 1;
    private decimal RemainingQty => PlannedQty - (GoodQty + BadQty);
    
    private decimal MissingQty { get; set; } = 0;
    private string? ValidationMessage { get; set; }

    private void OnConfirm()
    {
        if (GoodQty + BadQty + MissingQty > PlannedQty)
        {
            ValidationMessage = $"Total Qty cannot exceed Planned Quantity).";
            return;
        }

        if (GoodQty + BadQty + MissingQty <= 0)
        {
            ValidationMessage = "Enter a Good, Bad, or Missing Quantity greater than 0.";
            return;
        }

        DialogService.Close(new ManualEntryResult
        {
            GoodQty = GoodQty,
            BadQty = BadQty,
            MissingQty = MissingQty
        });
    }

    private void OnCancel()
    {
        DialogService.Close(null);
    }

    public class ManualEntryResult
    {
        public decimal GoodQty { get; set; }
        public decimal BadQty { get; set; }
        public decimal MissingQty { get; set; }
    }
}
