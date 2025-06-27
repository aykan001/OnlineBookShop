using Npgsql;
using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public class BegeniService : IBegeniService
    {
        private readonly string _connectionString;

        public BegeniService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ??
                                "Host=localhost;Port=5432;Username=postgres;Password=12345;Database=users";
        }

        public List<Kitap> GetByUserId(int userId)
        {
            var kitaplar = new List<Kitap>();

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = @"
                SELECT k.id, k.kitap_adi, k.tur, k.aciklama, k.stok, k.ucret
                FROM begeniler b
                INNER JOIN kitaplar k ON b.kitap_id = k.id
                WHERE b.kullanici_id = @userId";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);
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

        public async Task<bool> AddAsync(int kitapId, int kullaniciId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var check = new NpgsqlCommand("SELECT COUNT(*) FROM begeniler WHERE kitap_id = @kitapId AND kullanici_id = @kullaniciId", conn);
            check.Parameters.AddWithValue("kitapId", kitapId);
            check.Parameters.AddWithValue("kullaniciId", kullaniciId);
            var exists = (long)await check.ExecuteScalarAsync();

            if (exists > 0)
                return false;

            var insert = new NpgsqlCommand("INSERT INTO begeniler (kitap_id, kullanici_id) VALUES (@kitapId, @kullaniciId)", conn);
            insert.Parameters.AddWithValue("kitapId", kitapId);
            insert.Parameters.AddWithValue("kullaniciId", kullaniciId);
            await insert.ExecuteNonQueryAsync();

            return true;
        }

        public async Task<bool> RemoveAsync(int kitapId, int kullaniciId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("DELETE FROM begeniler WHERE kitap_id = @kitapId AND kullanici_id = @kullaniciId", conn);
            cmd.Parameters.AddWithValue("kitapId", kitapId);
            cmd.Parameters.AddWithValue("kullaniciId", kullaniciId);
            int affected = await cmd.ExecuteNonQueryAsync();

            return affected > 0;
        }
    }
}
