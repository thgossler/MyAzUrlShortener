namespace Cloud5mins.ShortenerTools.Core.Domain
{
    public class ClickStatsEntityList
    {
        public List<ClickStatsEntity> ClickStatsList { get; set; } = new List<ClickStatsEntity>();

        public ClickStatsEntityList() { }
        public ClickStatsEntityList(List<ClickStatsEntity> list)
        {
            ClickStatsList = list;
        }
    }
}