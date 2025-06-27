namespace OnlineBookShop.Models
{
    public class Sepet
    {
        public int Id { get; set; }
        public int KitapId { get; set; }
        public int KullaniciId { get; set; }  // ✅ EKLENDİ
    }
}
