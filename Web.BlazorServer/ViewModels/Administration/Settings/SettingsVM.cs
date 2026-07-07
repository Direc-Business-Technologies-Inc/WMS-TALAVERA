namespace Web.BlazorServer.ViewModels.Administration.Settings
{
    public class SettingsVM
    {
        public Types Type { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string Code { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string Value { get; private set; } = string.Empty;
        public enum Types
        {
            String,
            Integer,
            Decimal
        }
    }
}
