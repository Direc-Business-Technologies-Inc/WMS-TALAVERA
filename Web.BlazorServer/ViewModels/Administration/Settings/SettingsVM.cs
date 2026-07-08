namespace Web.BlazorServer.ViewModels.Administration.Settings
{
    public class SettingsVM
    {
        public Types Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Value {
            get => _value;
            set
            {
                _value = value;
                Dirty = true;
            }
        }
        public string _value { get; set; } = string.Empty;
        public bool Dirty { get; set; }
        public enum Types
        {
            Default,
            String,
            Integer,
            Decimal
        }
    }
}
