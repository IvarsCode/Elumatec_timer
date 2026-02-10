// using Avalonia;
// using Avalonia.Controls;
// using Avalonia.Markup.Xaml;
// using Avalonia.Controls.ApplicationLifetimes;

// namespace Elumatec.Tijdregistratie
// {
//     public partial class TijdregistratieApp : Application
//     {
//         public override void Initialize()
//         {
//             AvaloniaXamlLoader.Load(this);
//         }

//         public override void OnFrameworkInitializationCompleted()
//         {
//             if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
//             {
//                 // Open a placeholder window so the app builds
//                 desktop.MainWindow = new Window
//                 {
//                     Width = 800,
//                     Height = 600,
//                     Title = "Tijdregistratie"
//                 };
//             }

//             base.OnFrameworkInitializationCompleted();
//         }
//     }
// }
