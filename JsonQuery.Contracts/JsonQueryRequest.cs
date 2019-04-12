using System;
using System.Collections.Generic;

namespace JsonQuery.Contracts
{
    public class JsonQueryRequest
    {
        public List<JsonQuerySelect> Select { get; set; }
        public string From { get; set; }
    }

    public class JsonQuerySelect
    {
        public string Selector { get; set; }
        public string Name { get; set; }
    }

    public class JsonQueryResult
    {
        public List<string> Columns { get; set; }
        public List<JsonQueryResultRow> Rows { get; set; }
    }

    public class JsonQueryResultRow
    {
        public object[] Data { get; set; }
    }

    public class JsonQuerySchema
    {
        public string Name { get; set; }
        public List<JsonQuerySchemaProperty> Properties { get; set; }
        public List<JsonQuerySchema> Childs { get; set; }
    }

    public class JsonQuerySchemaProperty
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
    }
}
