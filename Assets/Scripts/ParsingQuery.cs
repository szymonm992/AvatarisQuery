[System.Serializable]   
public class ParsingQuery 
{
    public string Query;
    public float Priority;

    public ParsingQuery(string code, float priority)
    {
        this.Query = code;
        this.Priority = priority;
    }
}
