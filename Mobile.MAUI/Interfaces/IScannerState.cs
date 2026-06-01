namespace Mobile.MAUI.Interfaces;

public interface IScannerState
{
    bool ConflictDialogOpen { get; set; }
    int TimesScanned { get; set; }
}
