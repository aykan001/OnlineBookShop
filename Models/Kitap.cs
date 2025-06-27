using System.ComponentModel.DataAnnotations;

namespace OnlineBookShop.Models
{
    public class Kitap
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kitap adı boş olamaz")]
        public string KitapAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tür boş olamaz")]
        public string Tur { get; set; } = string.Empty;

        public string Aciklama { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Stok sıfırdan küçük olamaz")]
        public int Stok { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Ücret pozitif olmalıdır")]
        public decimal Ucret { get; set; }
    }
}
