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

        public async Task<List<Kitap>> GetAllAsync()
        {
            var kitaplar = new List<Kitap>();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT * FROM kitaplar", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
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

        public async Task<Kitap?> GetByIdAsync(int id)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT * FROM kitaplar WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Kitap
                {
                    Id = reader.GetInt32(0),
                    KitapAdi = reader.GetString(1),
                    Tur = reader.GetString(2),
                    Aciklama = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Stok = reader.GetInt32(4),
                    Ucret = reader.GetDecimal(5)
                };
            }

            return null;
        }
    }
}


