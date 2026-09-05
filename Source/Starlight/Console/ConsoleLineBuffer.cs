using System.Text;

namespace Starlight.Console;

internal sealed class ConsoleLineBuffer
{
    private readonly StringBuilder _text = new();

    public string Text => _text.ToString();
    public int CursorIndex { get; private set; }

    public void Insert(char character)
    {
        _text.Insert(CursorIndex, character);
        CursorIndex++;
    }

    public void Backspace()
    {
        if (CursorIndex == 0)
            return;

        _text.Remove(CursorIndex - 1, length: 1);
        CursorIndex--;
    }

    public void Delete()
    {
        if (CursorIndex >= _text.Length)
            return;

        _text.Remove(CursorIndex, length: 1);
    }

    public void MoveLeft()
    {
        if (CursorIndex > 0)
            CursorIndex--;
    }

    public void MoveRight()
    {
        if (CursorIndex < _text.Length)
            CursorIndex++;
    }

    public void MoveHome() => CursorIndex = 0;

    public void MoveEnd() => CursorIndex = _text.Length;

    public void Clear()
    {
        _text.Clear();
        CursorIndex = 0;
    }

    public string TakeLine()
    {
        var line = _text.ToString();
        Clear();
        return line;
    }
}
