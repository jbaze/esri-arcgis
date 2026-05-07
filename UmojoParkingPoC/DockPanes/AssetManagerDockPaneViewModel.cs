using System;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
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
            pane?.RefreshAuthState();
        }

        public static void Show()
        {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }

        public AssetManagerDockPaneViewModel()
        {
            RefreshAuthState();
        }

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

        public void RefreshAuthState()
        {
            var client = ServiceLocator.ApiClient;
            IsAuthenticated = client?.IsAuthenticated ?? false;
            AuthStatusText = IsAuthenticated
                ? $"Signed in as {client.CurrentUsername}"
                : "Not signed in";
        }
    }

    internal class AssetManagerDockPane_ShowButton : Button
    {
        protected override void OnClick() => AssetManagerDockPaneViewModel.Show();
    }
}
