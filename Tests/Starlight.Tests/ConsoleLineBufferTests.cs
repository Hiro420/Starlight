using Starlight.Console;
using Xunit;

namespace Starlight.Tests;

public sealed class ConsoleLineBufferTests
{
    [Fact]
    public void EditingInTheMiddle_PreservesTheRestOfTheLine()
    {
        var buffer = new ConsoleLineBuffer();

        foreach (var character in "gve 1001")
        {
            buffer.Insert(character);
        }

        buffer.MoveHome();
        buffer.MoveRight();
        buffer.Insert('i');

        Assert.Equal("give 1001", buffer.Text);
        Assert.Equal(expected: 2, buffer.CursorIndex);
    }

    [Fact]
    public void BackspaceAndDelete_EditAroundTheCursor()
    {
        var buffer = new ConsoleLineBuffer();

        foreach (var character in "givee")
        {
            buffer.Insert(character);
        }

        buffer.MoveLeft();
        buffer.Backspace();
        buffer.Delete();

        Assert.Equal("giv", buffer.Text);
        Assert.Equal(expected: 3, buffer.CursorIndex);
    }

    [Fact]
    public void TakeLine_ReturnsTheCurrentTextAndResetsTheBuffer()
    {
        var buffer = new ConsoleLineBuffer();

        foreach (var character in "give 1001 201")
        {
            buffer.Insert(character);
        }

        var line = buffer.TakeLine();

        Assert.Equal("give 1001 201", line);
        Assert.Equal(string.Empty, buffer.Text);
        Assert.Equal(expected: 0, buffer.CursorIndex);
    }
}
