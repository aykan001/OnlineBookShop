using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OnlineBookShop.Models
{
    public class User
    {
        public int Id { get; set; }

        [JsonPropertyName("ad")]
        [Required(ErrorMessage = "Ad boş olamaz")]
        public string Ad { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        [Required(ErrorMessage = "Email boş olamaz")]
        [EmailAddress(ErrorMessage = "Geçersiz email formatı")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("sifre")]
        [Required(ErrorMessage = "Şifre boş olamaz")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı")]
        public string Sifre { get; set; } = string.Empty;

    }
}
