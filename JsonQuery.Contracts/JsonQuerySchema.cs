using System.Collections.Generic;

namespace JsonQuery.Contracts
{
    public class JsonQuerySchema
    {
        public string Name { get; set; }
        public List<JsonQuerySchemaProperty> Properties { get; set; }
        // TODO: public List<JsonQuerySchema> Childs { get; set; }
    }
}
