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
                if (_firstSet) // this is very bad
                {
                    _firstSet = false;
                    _originalValue = value;
                }
                IsDirty = true;
            }
        }
        public string _value { get; set; } = string.Empty;
        public bool IsDirty { get; set; }
        public enum Types
        {
            Default,
            String,
            Integer,
            Decimal
        }

        public void Reset()
        {
            Value = _originalValue;
            IsDirty = false;
        }

        private bool _firstSet = false;
        private string _originalValue = string.Empty;
    }
}
