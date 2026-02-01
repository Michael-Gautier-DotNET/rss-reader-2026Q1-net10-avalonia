using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace gautier.rss.ui
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime
                desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static void SetDisplayMemberPath(ListBox listBox, string propertyPath)
        {
            listBox.ItemTemplate = new FuncDataTemplate<object>(
                (item, scope) =>
                {
                    // Create the TextBlock first, then bind, then return it
                    TextBlock? textBlock = new();
                    textBlock.Bind(TextBlock.TextProperty, new Binding(propertyPath));
                    return textBlock;
                },
                true
            );
        }
    }
}
