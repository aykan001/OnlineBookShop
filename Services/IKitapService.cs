using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public interface IKitapService
    {
        Task<List<Kitap>> GetAllAsync();
        Task<Kitap?> GetByIdAsync(int id);
    }
}


