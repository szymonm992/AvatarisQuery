using System.Collections.Generic;

public class QueryNode
{
    public NodeType Type { get; set; }
    public string Value { get; set; }
    public List<QueryNode> Children { get; } = new();
}