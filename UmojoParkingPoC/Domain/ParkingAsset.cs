using System;
using ArcGIS.Core.Geometry;

namespace UmojoParkingPoC.Domain
{
    public class ParkingAsset
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ParkingAssetType AssetType { get; set; }
        public decimal? HourlyRate { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public AssetStatus Status { get; set; }
        public Geometry Geometry { get; set; }
        public DateTime LastModified { get; set; }
    }
}
