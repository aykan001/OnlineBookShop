namespace OnlineBookShop.Services
{
    public interface IPuanService
    {
        Task<bool> PuanVerAsync(int kullaniciId, int kitapId, int puan);
        Task<decimal> OrtalamaPuanAsync(int kitapId);
    }
}


