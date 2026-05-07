using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using UmojoParkingPoC.Services;

namespace UmojoParkingPoC
{
    internal class Module : ArcGIS.Desktop.Framework.Contracts.Module
    {
        private static Module _this;

        public static Module Current =>
            _this ??= (Module)FrameworkApplication.FindModule("UmojoParkingPoC_Module");

        protected override Task<bool> OnStartupAsync()
        {
            ServiceLocator.ApiClient ??= new MockUmojoApiClient();
            return Task.FromResult(true);
        }

        protected override bool CanUnload() => true;
    }
}
