namespace EduCollab.Contracts.Responses
{
    public abstract class PagedCollectionResponse
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }
    }
}
