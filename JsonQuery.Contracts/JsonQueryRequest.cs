using System.Collections.Generic;

namespace JsonQuery.Contracts
{
    public class JsonQueryRequest
    {
        public List<JsonQuerySelect> Select { get; set; }
        public JsonQueryFrom From { get; set; }
        public List<JsonQueryWhere> Where { get; set; }
        public List<JsonQueryOrderBy> OrderBy { get; set; }
        public JsonQueryLimit Limit { get; set; }
    }
}
