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

        private static readonly Dictionary<Guid, (Geometry Geometry, GraphicElement Element)> _byAssetId =
            new Dictionary<Guid, (Geometry, GraphicElement)>();

        public static Task RenderAssetsAsync(IList<ParkingAsset> assets)
        {
            return QueuedTask.Run(() =>
            {
                var layer = EnsureGraphicsLayer();
                if (layer == null) return;

                var existing = layer.GetElementsAsFlattenedList().ToList();
                if (existing.Count > 0)
                    layer.RemoveElements(existing);
                _byAssetId.Clear();

                foreach (var asset in assets)
                {
                    var graphic = BuildGraphic(asset);
                    var element = layer.AddElement(graphic, asset.Id.ToString("N"));
                    _byAssetId[asset.Id] = (asset.Geometry, element);
                }
            });
        }

        public static Task UpdateAssetGraphicAsync(ParkingAsset asset)
        {
            return QueuedTask.Run(() =>
            {
                var layer = EnsureGraphicsLayer();
                if (layer == null) return;

                if (_byAssetId.TryGetValue(asset.Id, out var existing))
                {
                    layer.RemoveElement(existing.Element);
                    _byAssetId.Remove(asset.Id);
                }

                var graphic = BuildGraphic(asset);
                var element = layer.AddElement(graphic, asset.Id.ToString("N"));
                _byAssetId[asset.Id] = (asset.Geometry, element);
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

                foreach (var kvp in _byAssetId)
                {
                    var geom = kvp.Value.Geometry;
                    if (geom == null) continue;
                    var aligned = GeometryEngine.Instance.Project(probe, geom.SpatialReference);
                    if (GeometryEngine.Instance.Intersects(aligned, geom))
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

        private static CIMGraphic BuildGraphic(ParkingAsset asset)
        {
            var symbol = BuildSymbol(asset);
            return asset.Geometry switch
            {
                MapPoint pt => (CIMGraphic)new CIMPointGraphic { Location = pt, Symbol = symbol },
                Polygon poly => new CIMPolygonGraphic { Polygon = poly, Symbol = symbol },
                Polyline line => new CIMLineGraphic { Line = line, Symbol = symbol },
                _ => throw new NotSupportedException($"Unsupported geometry type: {asset.Geometry?.GetType().Name}")
            };
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
