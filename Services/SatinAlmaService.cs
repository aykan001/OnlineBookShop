using Npgsql;

namespace OnlineBookShop.Services
{
    public class SatinAlmaService : ISatinAlmaService
    {
        private readonly string _connectionString;

        public SatinAlmaService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ??
                                "Host=localhost;Port=5432;Username=postgres;Password=12345;Database=users";
        }

        public async Task<(bool, string?)> SatinAlAsync(int userId, List<int> kitapIdListesi)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var kitapId in kitapIdListesi)
            {
                var stokCmd = new NpgsqlCommand("SELECT stok FROM kitaplar WHERE id = @kitapId", conn);
                stokCmd.Parameters.AddWithValue("kitapId", kitapId);
                var stokResult = await stokCmd.ExecuteScalarAsync();
                int stok = stokResult != null ? Convert.ToInt32(stokResult) : 0;

                if (stok <= 0)
                    return (false, $"Kitap ID {kitapId} stokta yok.");

                var updateCmd = new NpgsqlCommand("UPDATE kitaplar SET stok = stok - 1 WHERE id = @kitapId", conn);
                updateCmd.Parameters.AddWithValue("kitapId", kitapId);
                await updateCmd.ExecuteNonQueryAsync();

                var deleteCmd = new NpgsqlCommand("DELETE FROM sepet WHERE kitap_id = @kitapId AND kullanici_id = @userId", conn);
                deleteCmd.Parameters.AddWithValue("kitapId", kitapId);
                deleteCmd.Parameters.AddWithValue("userId", userId);
                await deleteCmd.ExecuteNonQueryAsync();

                var siparisCmd = new NpgsqlCommand("INSERT INTO siparisler (kullanici_id, kitap_id) VALUES (@userId, @kitapId)", conn);
                siparisCmd.Parameters.AddWithValue("userId", userId);
                siparisCmd.Parameters.AddWithValue("kitapId", kitapId);
                await siparisCmd.ExecuteNonQueryAsync();
            }

            return (true, null);
        }
    }
}
