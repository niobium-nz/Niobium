namespace Niobium.Channel
{
    public class EditOption(string text, string value, bool isSelected, Action<bool> onSelected)
    {
        public string Text { get; set; } = text;

        public string Value { get; set; } = value;

        public bool IsSelected { get; } = isSelected;

        public Action<bool> OnSelected { get; } = onSelected;
    }
}
