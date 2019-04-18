using System.Collections.Generic;

namespace JsonQuery.Contracts
{
    public class JsonQueryResult
    {
        public List<JsonQueryResultColumn> Columns { get; set; }
        public List<JsonQueryResultRow> Rows { get; set; }
    }
}
