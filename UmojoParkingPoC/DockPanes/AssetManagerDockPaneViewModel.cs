using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using UmojoParkingPoC.Domain;
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
            RefreshCommand = new RelayCommand(() => { _ = LoadAssetsAsync(); }, () => IsAuthenticated && !IsLoading);
            SaveCommand = new RelayCommand(() => { _ = SaveAsync(); }, () => IsAuthenticated && !IsLoading && SelectedAsset != null);
            RefreshAuthState();
        }

        public ObservableCollection<ParkingAsset> Assets { get; } = new ObservableCollection<ParkingAsset>();
        public IReadOnlyList<AssetStatus> StatusOptions { get; } = (AssetStatus[])Enum.GetValues(typeof(AssetStatus));

        public ICommand RefreshCommand { get; }
        public ICommand SaveCommand { get; }

        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set => SetProperty(ref _isAuthenticated, value);
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
            set => SetProperty(ref _isLoading, value);
        }

        private ParkingAsset _selectedAsset;
        public ParkingAsset SelectedAsset
        {
            get => _selectedAsset;
            set
            {
                if (SetProperty(ref _selectedAsset, value))
                    LoadEditFieldsFromSelection();
            }
        }

        private string _editName;
        public string EditName
        {
            get => _editName;
            set => SetProperty(ref _editName, value);
        }

        private string _editHourlyRate;
        public string EditHourlyRate
        {
            get => _editHourlyRate;
            set => SetProperty(ref _editHourlyRate, value);
        }

        private string _editTimeLimit;
        public string EditTimeLimit
        {
            get => _editTimeLimit;
            set => SetProperty(ref _editTimeLimit, value);
        }

        private AssetStatus _editStatus;
        public AssetStatus EditStatus
        {
            get => _editStatus;
            set => SetProperty(ref _editStatus, value);
        }

        public string LastModifiedDisplay =>
            SelectedAsset == null
                ? string.Empty
                : SelectedAsset.LastModified.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

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

        private async Task SaveAsync()
        {
            if (SelectedAsset == null) return;

            IsLoading = true;
            StatusMessage = "Syncing...";
            try
            {
                if (!TryParseRate(EditHourlyRate, out var rate))
                {
                    StatusMessage = "Error: Hourly rate must be a number";
                    return;
                }
                if (!TryParseInt(EditTimeLimit, out var timeLimit))
                {
                    StatusMessage = "Error: Time limit must be a whole number";
                    return;
                }

                var result = await ServiceLocator.ApiClient.UpdateAssetAttributesAsync(
                    SelectedAsset.Id, EditName, rate, timeLimit, EditStatus);

                if (!result.Success)
                {
                    StatusMessage = "Error: " + result.ErrorMessage;
                    return;
                }

                ApplyUpdatedAsset(result.Data);
                await MapDisplayService.UpdateAssetGraphicAsync(result.Data);
                StatusMessage = "Saved at " + DateTime.Now.ToString("HH:mm:ss");
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

        private void ApplyUpdatedAsset(ParkingAsset updated)
        {
            for (int i = 0; i < Assets.Count; i++)
            {
                if (Assets[i].Id == updated.Id)
                {
                    Assets[i] = updated;
                    SelectedAsset = updated;
                    return;
                }
            }
        }

        private void LoadEditFieldsFromSelection()
        {
            if (SelectedAsset == null)
            {
                EditName = null;
                EditHourlyRate = null;
                EditTimeLimit = null;
            }
            else
            {
                EditName = SelectedAsset.Name;
                EditHourlyRate = SelectedAsset.HourlyRate?.ToString("0.00", CultureInfo.CurrentCulture);
                EditTimeLimit = SelectedAsset.TimeLimitMinutes?.ToString(CultureInfo.CurrentCulture);
                EditStatus = SelectedAsset.Status;
            }
            NotifyPropertyChanged(nameof(LastModifiedDisplay));
        }

        private void ReplaceAssets(IEnumerable<ParkingAsset> source)
        {
            Assets.Clear();
            foreach (var a in source) Assets.Add(a);
            if (SelectedAsset != null && !Assets.Any(a => a.Id == SelectedAsset.Id))
                SelectedAsset = null;
        }

        private static bool TryParseRate(string input, out decimal? value)
        {
            if (string.IsNullOrWhiteSpace(input)) { value = null; return true; }
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out var d))
            {
                value = d;
                return true;
            }
            value = null;
            return false;
        }

        private static bool TryParseInt(string input, out int? value)
        {
            if (string.IsNullOrWhiteSpace(input)) { value = null; return true; }
            if (int.TryParse(input, NumberStyles.Integer, CultureInfo.CurrentCulture, out var n))
            {
                value = n;
                return true;
            }
            value = null;
            return false;
        }
    }
}
