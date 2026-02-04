using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Elumatec.Tijdregistratie
{
    public partial class TijdregistratieApp : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try
            {
                File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "startup.log"), $"[{DateTime.Now:O}] OnFrameworkInitializationCompleted start\n");
            }
            catch { }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new TijdregistratieWindow();
                // assign a simple viewmodel so MainWindow.xaml bindings work without additional services
                window.DataContext = new Elumatec.Tijdregistratie.ViewModels.UserSelectionViewModel();
                window.Opened += (_, __) => File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "startup.log"), $"[{DateTime.Now:O}] MainWindow Opened\n");
                window.Closed += (_, __) => File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "startup.log"), $"[{DateTime.Now:O}] MainWindow Closed\n");
                desktop.MainWindow = window;
                try { File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "startup.log"), $"[{DateTime.Now:O}] MainWindow assigned\n"); } catch { }
            }

            base.OnFrameworkInitializationCompleted();
            try { File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "startup.log"), $"[{DateTime.Now:O}] OnFrameworkInitializationCompleted end\n"); } catch { }
        }
    }
}
