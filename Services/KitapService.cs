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

        public async Task<(List<Kitap> Kitaplar, int Toplam)> GetAllAsync(int sayfa, int adet, string? tur, string? sirala, string? yon)
        {
            var kitaplar = new List<Kitap>();
            int toplam = 0;
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
        
            var query = "SELECT * FROM kitaplar";
            var countQuery = "SELECT COUNT(*) FROM kitaplar";
            var parameters = new List<NpgsqlParameter>();
        
            if (!string.IsNullOrEmpty(tur))
{
                query += " WHERE LOWER(tur) = LOWER(@tur)";
                countQuery += " WHERE LOWER(tur) = LOWER(@tur)";
                parameters.Add(new NpgsqlParameter("tur", tur));
            }


        
            if (!string.IsNullOrEmpty(sirala))
            {
                string orderColumn = sirala switch
                {
                    "kitapAdi" => "kitap_adi",
                    "stok" => "stok",
                    "ucret" => "ucret",
                    _ => "kitap_adi"
                };
                string direction = (yon ?? "asc").ToLower() == "desc" ? "DESC" : "ASC";
                query += $" ORDER BY {orderColumn} {direction}";
            }
        
            query += " OFFSET @offset LIMIT @limit";
            parameters.Add(new NpgsqlParameter("offset", (sayfa - 1) * adet));
            parameters.Add(new NpgsqlParameter("limit", adet));
        
            // Kitap verilerini al
            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
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
            await reader.CloseAsync();
        
            // Toplam kitap sayısını al
            await using var countCmd = new NpgsqlCommand(countQuery, conn);
            countCmd.Parameters.AddRange(parameters.Where(p => p.ParameterName == "tur").ToArray());
            toplam = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        
            return (kitaplar, toplam);
        }

        public async Task<int> CountAsync(string? tur)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
        
            var query = "SELECT COUNT(*) FROM kitaplar";
            if (!string.IsNullOrEmpty(tur))
                query += " WHERE tur = @tur";
        
            await using var cmd = new NpgsqlCommand(query, conn);
            if (!string.IsNullOrEmpty(tur))
                cmd.Parameters.AddWithValue("tur", tur);
        
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
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

