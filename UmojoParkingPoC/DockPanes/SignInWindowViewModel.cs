using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework.Contracts;
using UmojoParkingPoC.Framework;
using UmojoParkingPoC.Services;

namespace UmojoParkingPoC.DockPanes
{
    internal class SignInWindowViewModel : PropertyChangedBase
    {
        public SignInWindowViewModel()
        {
            SignInCommand = new RelayCommand(
                async parameter => await SignInAsync(parameter as string),
                parameter => !IsBusy);
        }

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetProperty(ref _isBusy, value);
                (SignInCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand SignInCommand { get; }

        public event EventHandler<bool> RequestClose;

        private async Task SignInAsync(string password)
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Username is required";
                return;
            }

            IsBusy = true;
            try
            {
                var result = await ServiceLocator.ApiClient.SignInAsync(Username, password ?? string.Empty);
                if (result.Success)
                {
                    AssetManagerDockPaneViewModel.NotifyAuthStateChanged();
                    RequestClose?.Invoke(this, true);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Sign-in failed";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
