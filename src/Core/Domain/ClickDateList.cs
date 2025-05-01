namespace AzUrlShortener.Core.Domain
{
    public class ClickDateList
    {
        public List<ClickDate> Items { get; set; }
        public string Url { get; set; }

        public ClickDateList()
        {
            Items = new List<ClickDate>();
            Url = string.Empty;
        }

        public ClickDateList(List<ClickDate> list)
        {
            Items = list;
            Url = string.Empty;
        }
    }
}