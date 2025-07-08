using System;
using System.Text;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler;

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
    
    public void AppendLine(string text = "")
    {
        if (string.IsNullOrEmpty(text))
            _stringBuilder.AppendLine();

        var lines = text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            AppendIndentation();
            _stringBuilder.AppendLine(line);
        }
    }

    public void Append(string text = "")
    {
        AppendIndentation();
        _stringBuilder.Append(text);
    }

    public void OpenBlock(string text = "{")
    {
        AppendLine(text);
        IncreaseIndentation();
    }

    public void CloseBlock(string text = "}")
    {
        DecreaseIndentation();
        if (!string.IsNullOrWhiteSpace(text)) Append(text);
    }

    public void IncreaseIndentation() => _indentationLevel++;
    public void DecreaseIndentation() => _indentationLevel--;
    
    public new string ToString() => _stringBuilder.ToString();
}