using ArcGIS.Desktop.Framework.Contracts;
using UmojoParkingPoC.DockPanes;

namespace UmojoParkingPoC.Buttons
{
    internal class OpenAssetManagerButton : Button
    {
        protected override void OnClick() => AssetManagerDockPaneViewModel.Show();
    }
}
