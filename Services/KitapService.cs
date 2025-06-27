using Npgsql;
using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public class KitapService : IKitapService
    {
        private readonly string _connectionString;

        public KitapService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ?? 
                                "Host=localhost;Port=5432;Username=postgres;Password=12345;Database=users";
        }

        public List<Kitap> GetAll()
        {
            var kitaplar = new List<Kitap>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM kitaplar", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                kitaplar.Add(new Kitap
                {
                    Id = reader.GetInt32(0),
                    KitapAdi = reader.GetString(1),
                    Tur = reader.GetString(2),
                    Aciklama = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Stok = reader.GetInt32(4),
                    Ucret = reader.GetDecimal(5)
                });
            }

            return kitaplar;
        }
    }
}
