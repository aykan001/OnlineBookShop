using Npgsql;
using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public class SiparisService : ISiparisService
    {
        private readonly string _connectionString;

        public SiparisService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ??
                                "Host=localhost;Port=5432;Username=postgres;Password=12345;Database=users";
        }

        public List<Kitap> GetByUserId(int kullaniciId)
        {
            var siparisler = new List<Kitap>();

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = @"
              SELECT k.id, k.kitap_adi, k.tur, k.aciklama, k.stok, k.ucret
              FROM siparisler s
              INNER JOIN kitaplar k ON s.kitap_id = k.id
              WHERE s.kullanici_id = @userId";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", kullaniciId);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                siparisler.Add(new Kitap
                {
                    Id = reader.GetInt32(0),
                    KitapAdi = reader.GetString(1),
                    Tur = reader.GetString(2),
                    Aciklama = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Stok = reader.GetInt32(4),
                    Ucret = reader.GetDecimal(5)
                });
            }

            return siparisler;
        }

        public bool Add(int kullaniciId, int kitapId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("INSERT INTO siparisler (kullanici_id, kitap_id) VALUES (@userId, @kitapId)", conn);
            cmd.Parameters.AddWithValue("userId", kullaniciId);
            cmd.Parameters.AddWithValue("kitapId", kitapId);

            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
