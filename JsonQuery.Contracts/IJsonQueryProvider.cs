using System.Linq;

namespace JsonQuery.Contracts
{
    public interface IJsonQueryProvider
    {
        void AddQueryable(IQueryable queryable);
        void AddQueryable(IQueryable queryable, string name);
        JsonQueryResult ExecuteQuery(JsonQueryRequest request);
    }
}
