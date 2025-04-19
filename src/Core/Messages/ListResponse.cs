using Cloud5mins.ShortenerTools.Core.Domain;

namespace Cloud5mins.ShortenerTools.Core.Messages
{
    public class ListResponse
    {
        public List<ShortUrlEntity> UrlList { get; set; } = new List<ShortUrlEntity>();

        public ListResponse() { }

        public ListResponse(List<ShortUrlEntity> list)
        {
            UrlList = list;
        }
    }
}