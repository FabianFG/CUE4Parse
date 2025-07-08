using System.Text;

namespace CUE4Parse;

public class CustomStringBuilder
{
    private readonly StringBuilder _stringBuilder;
    private int _indentationLevel;
    
    private string Indent => new string(' ', _indentationLevel * 4);
    
    public CustomStringBuilder()
    {
        _stringBuilder = new StringBuilder();
        _indentationLevel = 0;
    }
    
    private void AppendIndentation()
    {
        _stringBuilder.Append(Indent);
    }
    
    public void AppendLine(string line)
    {
        AppendIndentation();
        _stringBuilder.AppendLine(line);
    }

    public void OpenBlock(string text = "{")
    {
        AppendLine(text);
        _indentationLevel++;
    }

    public void CloseBlock(string text = "}")
    {
        _indentationLevel--;
        AppendLine(text);
    }
    
    public new string ToString() => _stringBuilder.ToString();
}