using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public interface ISiparisService
    {
        List<Kitap> GetByUserId(int userId);
    }
}
