using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using UmojoParkingPoC.Domain;

namespace UmojoParkingPoC.Services
{
    public class MockUmojoApiClient : IUmojoApiClient
    {
        private const decimal MunicipalRateCap = 20m;
        private static readonly TimeSpan AuthDelay = TimeSpan.FromMilliseconds(1500);
        private static readonly TimeSpan ApiDelay = TimeSpan.FromMilliseconds(1200);

        private AuthToken _currentToken;
        private List<ParkingAsset> _assets;
        private bool _seeded;
        private readonly object _seedLock = new object();

        public bool IsAuthenticated => _currentToken != null;
        public string CurrentUsername => _currentToken?.Username;

        public async Task<ApiResult<AuthToken>> SignInAsync(string username, string password)
        {
            await Task.Delay(AuthDelay);
            _currentToken = new AuthToken
            {
                Token = Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Username = username
            };
            return ApiResult<AuthToken>.Ok(_currentToken);
        }

        public Task SignOutAsync()
        {
            _currentToken = null;
            return Task.CompletedTask;
        }

        public async Task<ApiResult<List<ParkingAsset>>> GetAssetsAsync()
        {
            if (!IsAuthenticated)
                return ApiResult<List<ParkingAsset>>.Fail("Not authenticated");

            await EnsureSeededAsync();
            await Task.Delay(ApiDelay);
            return ApiResult<List<ParkingAsset>>.Ok(_assets.ToList());
        }

        public async Task<ApiResult<ParkingAsset>> UpdateAssetAttributesAsync(
            Guid id,
            string name,
            decimal? hourlyRate,
            int? timeLimitMinutes,
            AssetStatus status)
        {
            if (!IsAuthenticated)
                return ApiResult<ParkingAsset>.Fail("Not authenticated");

            await Task.Delay(ApiDelay);

            if (hourlyRate.HasValue && hourlyRate.Value > MunicipalRateCap)
                return ApiResult<ParkingAsset>.Fail($"Hourly rate exceeds municipal cap of ${MunicipalRateCap:0.00}");

            await EnsureSeededAsync();
            var asset = _assets.FirstOrDefault(a => a.Id == id);
            if (asset == null)
                return ApiResult<ParkingAsset>.Fail($"Asset {id} not found");

            asset.Name = name;
            asset.HourlyRate = hourlyRate;
            asset.TimeLimitMinutes = timeLimitMinutes;
            asset.Status = status;
            asset.LastModified = DateTime.UtcNow;

            return ApiResult<ParkingAsset>.Ok(asset);
        }

        private Task EnsureSeededAsync()
        {
            if (_seeded) return Task.CompletedTask;
            return QueuedTask.Run(() =>
            {
                lock (_seedLock)
                {
                    if (_seeded) return;
                    _assets = SeedAssets();
                    _seeded = true;
                }
            });
        }

        private static List<ParkingAsset> SeedAssets()
        {
            var sr = SpatialReferences.WGS84;
            var now = DateTime.UtcNow;

            // Bounding box around downtown Chicago: lon -87.65, lat 41.88
            const double cx = -87.65;
            const double cy = 41.88;
            const double zoneSize = 0.004;
            const double zoneStep = 0.010;

            var zoneNames = new[]
            {
                "Downtown Zone A",
                "Loop Zone B",
                "Riverwalk Zone C",
                "Theatre Zone D",
                "Permit Zone E"
            };

            var zones = new List<ParkingAsset>();
            for (int i = 0; i < zoneNames.Length; i++)
            {
                double offsetX = (i - 2) * zoneStep;
                double minX = cx + offsetX - zoneSize;
                double maxX = cx + offsetX + zoneSize;
                double minY = cy - zoneSize;
                double maxY = cy + zoneSize;

                var polygon = PolygonBuilderEx.CreatePolygon(
                    new[]
                    {
                        new Coordinate2D(minX, minY),
                        new Coordinate2D(maxX, minY),
                        new Coordinate2D(maxX, maxY),
                        new Coordinate2D(minX, maxY),
                        new Coordinate2D(minX, minY)
                    },
                    sr);

                zones.Add(new ParkingAsset
                {
                    Id = Guid.NewGuid(),
                    Name = zoneNames[i],
                    AssetType = ParkingAssetType.Zone,
                    HourlyRate = 2.5m + i * 0.5m,
                    TimeLimitMinutes = (i % 3) switch { 0 => 60, 1 => 120, _ => 240 },
                    Status = AssetStatus.Active,
                    Geometry = polygon,
                    LastModified = now
                });
            }

            var meters = new List<ParkingAsset>();
            for (int i = 0; i < 5; i++)
            {
                double mx = cx + (i - 2) * 0.003;
                double my = cy + 0.006;
                var point = MapPointBuilderEx.CreateMapPoint(mx, my, sr);

                meters.Add(new ParkingAsset
                {
                    Id = Guid.NewGuid(),
                    Name = $"Meter {(i + 1):000}",
                    AssetType = ParkingAssetType.Meter,
                    HourlyRate = 1.5m + i * 0.75m,
                    TimeLimitMinutes = (i % 2 == 0) ? 60 : 120,
                    Status = i == 3 ? AssetStatus.Maintenance : AssetStatus.Active,
                    Geometry = point,
                    LastModified = now
                });
            }

            var all = new List<ParkingAsset>();
            all.AddRange(zones);
            all.AddRange(meters);
            return all;
        }
    }
}
