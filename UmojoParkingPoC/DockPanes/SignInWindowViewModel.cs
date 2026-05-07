using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using UmojoParkingPoC.Services;

namespace UmojoParkingPoC.DockPanes
{
    internal class SignInWindowViewModel : PropertyChangedBase
    {
        public SignInWindowViewModel()
        {
            SignInCommand = new RelayCommand(() => { _ = SignInAsync(); }, () => !IsBusy);
        }

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password { private get; set; }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand SignInCommand { get; }

        public event EventHandler<bool> RequestClose;

        private async Task SignInAsync()
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
                var result = await ServiceLocator.ApiClient.SignInAsync(Username, Password ?? string.Empty);
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
