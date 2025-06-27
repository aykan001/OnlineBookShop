using Npgsql;
using OnlineBookShop.Services;

namespace OnlineBookShop.Services
{
    public class PuanService : IPuanService
    {
        private readonly string _connectionString;

        public PuanService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ??
                                "Host=localhost;Port=5432;Username=postgres;Password=12345;Database=users";
        }

        public async Task<bool> PuanVerAsync(int kullaniciId, int kitapId, int puan)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                INSERT INTO puanlar (kullanici_id, kitap_id, puan)
                VALUES (@kullaniciId, @kitapId, @puan)
                ON CONFLICT (kullanici_id, kitap_id)
                DO UPDATE SET puan = EXCLUDED.puan;
            ", conn);

            cmd.Parameters.AddWithValue("kullaniciId", kullaniciId);
            cmd.Parameters.AddWithValue("kitapId", kitapId);
            cmd.Parameters.AddWithValue("puan", puan);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<decimal> OrtalamaPuanAsync(int kitapId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT AVG(puan) FROM puanlar WHERE kitap_id = @kitapId", conn);
            cmd.Parameters.AddWithValue("kitapId", kitapId);

            var result = await cmd.ExecuteScalarAsync();

            return (result is DBNull || result == null) ? 0.0m : Convert.ToDecimal(result);
        }
    }
}
