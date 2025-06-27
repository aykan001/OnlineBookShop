using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public interface IBegeniService
    {
        List<Kitap> GetByUserId(int userId);
        Task<bool> AddAsync(int kitapId, int kullaniciId);
        Task<bool> RemoveAsync(int kitapId, int kullaniciId);
    }
}
