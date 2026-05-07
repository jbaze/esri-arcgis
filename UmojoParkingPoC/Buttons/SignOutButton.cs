using ArcGIS.Desktop.Framework.Contracts;
using UmojoParkingPoC.DockPanes;
using UmojoParkingPoC.Services;

namespace UmojoParkingPoC.Buttons
{
    internal class SignOutButton : Button
    {
        protected override async void OnClick()
        {
            await ServiceLocator.ApiClient.SignOutAsync();
            AssetManagerDockPaneViewModel.NotifyAuthStateChanged();
        }
    }
}
