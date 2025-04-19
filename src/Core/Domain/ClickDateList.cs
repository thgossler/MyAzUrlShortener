namespace Cloud5mins.ShortenerTools.Core.Domain
{
    public class ClickDateList
    {
        public List<ClickDate> Items { get; set; } = new List<ClickDate>();
        public string Url { get; set; } = string.Empty;

        public ClickDateList()
        {
            Url = string.Empty;
        }

        public ClickDateList(List<ClickDate> list)
        {
            Items = list;
            Url = string.Empty;
        }
    }
}