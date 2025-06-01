namespace SemanticKernelPlayground.Model;

public class TextSearchResult
{
    public string Content { get; set; }
    public double Score { get; set; }

    public TextSearchResult(string content, double score)
    {
        Content = content;
        Score = score;
    }
}