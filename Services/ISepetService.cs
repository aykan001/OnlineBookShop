using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public interface ISepetService
    {
        List<Kitap> GetByUserId(int userId);
        Task<bool> AddAsync(Sepet sepet);
        Task<bool> RemoveAsync(int kitapId, int kullaniciId);
    }
}
