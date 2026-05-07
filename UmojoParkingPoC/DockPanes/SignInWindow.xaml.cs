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
            _vm.Password = PasswordBox.Password;
            if (_vm.SignInCommand.CanExecute(null))
                _vm.SignInCommand.Execute(null);
        }
    }
}
