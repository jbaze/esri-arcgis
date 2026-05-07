using System.Windows;
using ArcGIS.Desktop.Framework.Controls;

namespace UmojoParkingPoC.DockPanes
{
    public partial class SignInWindow : ProWindow
    {
        private readonly SignInWindowViewModel _vm;

        public SignInWindow()
        {
            InitializeComponent();
            _vm = new SignInWindowViewModel();
            DataContext = _vm;
            _vm.RequestClose += (s, success) =>
            {
                DialogResult = success;
                Close();
            };
            Loaded += (_, __) => UsernameBox.Focus();
        }

        private void OnSignInClick(object sender, RoutedEventArgs e)
        {
            if (_vm.SignInCommand.CanExecute(PasswordBox.Password))
                _vm.SignInCommand.Execute(PasswordBox.Password);
        }
    }
}
