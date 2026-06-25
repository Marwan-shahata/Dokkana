using Dokkana.BLL.DTOs.Cart;
using Dokkana.Common;

namespace Dokkana.BLL.Managers
{
    public interface ICartManager
    {
        Task<CartDTO> GetCartAsync(string userId);
        Task<GeneralResult<CartDTO>> AddItemAsync(string userId, AddCartItemDTO dto);
        Task<GeneralResult<CartDTO>> UpdateItemAsync(string userId, int productId, UpdateCartItemDTO dto);
        Task<GeneralResult<CartDTO>> RemoveItemAsync(string userId, int productId);
        Task<CartDTO> ClearAsync(string userId);
    }
}
