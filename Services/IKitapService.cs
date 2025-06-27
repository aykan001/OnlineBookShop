using System.Collections.Generic;
using OnlineBookShop.Models; // 👈 Bu eksikti, eklemen gerekiyor

namespace OnlineBookShop.Services
{
    public interface IKitapService
    {
        List<Kitap> GetAll();
    }
}

