using System.Threading.Tasks;
using OnlineBookShop.Models; // 💥 BU SATIR GEREKLİ

namespace OnlineBookShop.Services
{
    public interface IUserService
    {
        Task<bool> RegisterAsync(User user);
        Task<(bool Success, int UserId, string Ad, string Email)> ValidateLoginAsync(string email, string sifre);
        Task<User?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdatePasswordAsync(int id, string oldPassword, string newPassword);
    }
}

