using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using UmojoParkingPoC.DockPanes;

namespace UmojoParkingPoC.Buttons
{
    internal class SignInButton : Button
    {
        protected override void OnClick()
        {
            var window = new SignInWindow
            {
                Owner = FrameworkApplication.Current.MainWindow
            };
            window.ShowDialog();
            AssetManagerDockPaneViewModel.NotifyAuthStateChanged();
        }
    }
}
