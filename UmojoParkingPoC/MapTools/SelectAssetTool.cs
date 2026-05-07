using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using UmojoParkingPoC.DockPanes;
using UmojoParkingPoC.Services;

namespace UmojoParkingPoC.MapTools
{
    internal class SelectAssetTool : MapTool
    {
        public SelectAssetTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Point;
            SketchOutputMode = SketchOutputMode.Map;
        }

        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            if (geometry is not MapPoint clickPoint)
                return true;

            var tolerance = ComputeTolerance();
            var assetId = await MapDisplayService.HitTestAsync(clickPoint, tolerance);
            if (assetId == null)
                return true;

            var pane = FrameworkApplication.DockPaneManager.Find(
                AssetManagerDockPaneViewModel.DockPaneId) as AssetManagerDockPaneViewModel;
            pane?.SelectAssetById(assetId.Value);
            return true;
        }

        private static double ComputeTolerance()
        {
            var extent = MapView.Active?.Extent;
            return extent == null ? 0.0005 : extent.Width * 0.005;
        }
    }
}
