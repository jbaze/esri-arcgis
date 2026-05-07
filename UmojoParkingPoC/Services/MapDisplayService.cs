using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using UmojoParkingPoC.Domain;

namespace UmojoParkingPoC.Services
{
    public static class MapDisplayService
    {
        public const string LayerName = "Umojo Parking (PoC)";

        private static readonly Dictionary<Guid, GraphicElement> _elementsByAssetId =
            new Dictionary<Guid, GraphicElement>();

        public static Task RenderAssetsAsync(IList<ParkingAsset> assets)
        {
            return QueuedTask.Run(() =>
            {
                var layer = EnsureGraphicsLayer();
                if (layer == null) return;

                var existing = layer.GetElementsAsFlattenedList().ToList();
                if (existing.Count > 0)
                    layer.RemoveElements(existing);
                _elementsByAssetId.Clear();

                foreach (var asset in assets)
                {
                    var symbol = BuildSymbol(asset);
                    var element = layer.AddElement(asset.Geometry, symbol);
                    element.SetName(asset.Id.ToString("N"));
                    _elementsByAssetId[asset.Id] = element;
                }
            });
        }

        public static Task UpdateAssetGraphicAsync(ParkingAsset asset)
        {
            return QueuedTask.Run(() =>
            {
                var layer = EnsureGraphicsLayer();
                if (layer == null) return;

                if (_elementsByAssetId.TryGetValue(asset.Id, out var existing))
                {
                    layer.RemoveElement(existing);
                    _elementsByAssetId.Remove(asset.Id);
                }

                var symbol = BuildSymbol(asset);
                var element = layer.AddElement(asset.Geometry, symbol);
                element.SetName(asset.Id.ToString("N"));
                _elementsByAssetId[asset.Id] = element;
            });
        }

        public static Task<Guid?> HitTestAsync(MapPoint clickPoint, double mapUnitsTolerance)
        {
            return QueuedTask.Run<Guid?>(() =>
            {
                if (clickPoint == null) return null;

                Geometry probe = clickPoint;
                if (mapUnitsTolerance > 0)
                    probe = GeometryEngine.Instance.Buffer(clickPoint, mapUnitsTolerance);

                foreach (var kvp in _elementsByAssetId)
                {
                    var elementGeom = kvp.Value.GetGraphic()?.Geometry;
                    if (elementGeom == null) continue;
                    var aligned = GeometryEngine.Instance.Project(probe, elementGeom.SpatialReference);
                    if (GeometryEngine.Instance.Intersects(aligned, elementGeom))
                        return kvp.Key;
                }
                return null;
            });
        }

        private static GraphicsLayer EnsureGraphicsLayer()
        {
            var map = MapView.Active?.Map;
            if (map == null) return null;

            var existing = map.GetLayersAsFlattenedList()
                              .OfType<GraphicsLayer>()
                              .FirstOrDefault(l => string.Equals(l.Name, LayerName, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing;

            var glParams = new GraphicsLayerCreationParams { Name = LayerName };
            return LayerFactory.Instance.CreateLayer<GraphicsLayer>(glParams, map);
        }

        private static CIMSymbolReference BuildSymbol(ParkingAsset asset)
        {
            switch (asset.AssetType)
            {
                case ParkingAssetType.Zone:
                    var fill = SymbolFactory.Instance.ConstructPolygonSymbol(
                        ColorFactory.Instance.CreateRGBColor(70, 130, 200, 40),
                        SimpleFillStyle.Solid,
                        SymbolFactory.Instance.ConstructStroke(
                            ColorFactory.Instance.CreateRGBColor(40, 90, 160), 1.5,
                            SimpleLineStyle.Solid));
                    return fill.MakeSymbolReference();

                case ParkingAssetType.Meter:
                default:
                    var pt = SymbolFactory.Instance.ConstructPointSymbol(
                        ColorFactory.Instance.CreateRGBColor(220, 100, 30), 8,
                        SimpleMarkerStyle.Circle);
                    return pt.MakeSymbolReference();
            }
        }
    }
}
