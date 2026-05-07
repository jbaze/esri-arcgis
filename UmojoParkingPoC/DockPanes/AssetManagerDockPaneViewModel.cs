using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using UmojoParkingPoC.Domain;
using UmojoParkingPoC.Framework;
using UmojoParkingPoC.Services;

namespace UmojoParkingPoC.DockPanes
{
    internal class AssetManagerDockPaneViewModel : DockPane
    {
        public const string DockPaneId = "UmojoParkingPoC_AssetManagerDockPane";

        public static event EventHandler AuthStateChanged;

        public static void NotifyAuthStateChanged()
        {
            AuthStateChanged?.Invoke(null, EventArgs.Empty);
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId) as AssetManagerDockPaneViewModel;
            pane?.OnAuthStateChanged();
        }

        public static void Show()
        {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }

        public AssetManagerDockPaneViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await LoadAssetsAsync(), () => IsAuthenticated && !IsLoading);
            RefreshAuthState();
        }

        public ObservableCollection<ParkingAsset> Assets { get; } = new ObservableCollection<ParkingAsset>();

        public ICommand RefreshCommand { get; }

        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set
            {
                SetProperty(ref _isAuthenticated, value);
                (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _authStatusText = "Not signed in";
        public string AuthStatusText
        {
            get => _authStatusText;
            private set => SetProperty(ref _authStatusText, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private ParkingAsset _selectedAsset;
        public ParkingAsset SelectedAsset
        {
            get => _selectedAsset;
            set => SetProperty(ref _selectedAsset, value);
        }

        public void SelectAssetById(Guid id)
        {
            var match = Assets.FirstOrDefault(a => a.Id == id);
            if (match != null) SelectedAsset = match;
        }

        public void RefreshAuthState()
        {
            var client = ServiceLocator.ApiClient;
            IsAuthenticated = client?.IsAuthenticated ?? false;
            AuthStatusText = IsAuthenticated
                ? $"Signed in as {client.CurrentUsername}"
                : "Not signed in";
        }

        private async void OnAuthStateChanged()
        {
            RefreshAuthState();
            if (IsAuthenticated)
            {
                await LoadAssetsAsync();
            }
            else
            {
                Assets.Clear();
                SelectedAsset = null;
                StatusMessage = null;
            }
        }

        private async Task LoadAssetsAsync()
        {
            if (!IsAuthenticated) return;

            IsLoading = true;
            StatusMessage = "Loading assets...";
            try
            {
                var result = await ServiceLocator.ApiClient.GetAssetsAsync();
                if (!result.Success)
                {
                    StatusMessage = "Error: " + result.ErrorMessage;
                    return;
                }

                ReplaceAssets(result.Data);
                await MapDisplayService.RenderAssetsAsync(result.Data);
                StatusMessage = $"{result.Data.Count} assets loaded";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ReplaceAssets(IEnumerable<ParkingAsset> source)
        {
            Assets.Clear();
            foreach (var a in source) Assets.Add(a);
            if (SelectedAsset != null && !Assets.Any(a => a.Id == SelectedAsset.Id))
                SelectedAsset = null;
        }
    }
}
