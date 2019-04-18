namespace JsonQuery.Contracts
{
    public class JsonQueryWhere
    {
        public string Selector { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public string[] Values { get; set; }

        // TODO: Add support for ORs
    }
}
