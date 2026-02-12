using Avalonia.Controls;
using Avalonia.Input;
using Elumatec.Tijdregistratie.Models;
using Elumatec.Tijdregistratie.ViewModels;

namespace Elumatec.Tijdregistratie.Views
{
    public partial class InterventieForm : UserControl
    {
        public InterventieForm()
        {
            InitializeComponent();
        }

        private void CallButton_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is Button button &&
                button.DataContext is InterventieCall call &&
                DataContext is InterventieFormViewModel viewModel)
            {
                viewModel.HoveredCall = call;
            }
        }

        private void CallButton_PointerExited(object? sender, PointerEventArgs e)
        {
            if (DataContext is InterventieFormViewModel viewModel)
            {
                viewModel.HoveredCall = null;
            }
        }
    }
}