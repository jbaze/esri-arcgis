using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmojoParkingPoC.Domain;

namespace UmojoParkingPoC.Services
{
    public interface IUmojoApiClient
    {
        bool IsAuthenticated { get; }
        string CurrentUsername { get; }

        Task<ApiResult<AuthToken>> SignInAsync(string username, string password);
        Task SignOutAsync();

        Task<ApiResult<List<ParkingAsset>>> GetAssetsAsync();

        Task<ApiResult<ParkingAsset>> UpdateAssetAttributesAsync(
            Guid id,
            string name,
            decimal? hourlyRate,
            int? timeLimitMinutes,
            AssetStatus status);
    }
}
