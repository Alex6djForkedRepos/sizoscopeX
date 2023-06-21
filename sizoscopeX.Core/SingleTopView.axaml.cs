using Avalonia.Controls;
using Avalonia.Interactivity;

namespace sizoscopeX.Core;

public partial class SingleTopView : UserControl
{
    private readonly Stack<(object? Content, string? Title)> _stack = new();

    public SingleTopView()
    {
        InitializeComponent();
    }

    public void NavigateTo(object? content)
    {
        _stack.Push((ContentFrame.Content, Title.Text));
        ContentFrame.Content = content;
        Back.IsVisible = true;
    }

    public void Back_Clicked(object? sender, RoutedEventArgs args)
    {
        if (_stack.Any())
        {
            (ContentFrame.Content, Title.Text) = _stack.Pop();
            if (!_stack.Any())
            {
                Back.IsVisible = false;
            }
        }
    }
}
