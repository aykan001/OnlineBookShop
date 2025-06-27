// Services/UserService.cs
using Npgsql;
using OnlineBookShop.Models;
using System.ComponentModel.DataAnnotations;

namespace OnlineBookShop.Services
{
    public class UserService : IUserService
    {
        private readonly string _connectionString;

        public UserService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ??
                                "Host=localhost;Port=5432;Username=postgres;Password=12345;Database=users";
        }

        public async Task<bool> RegisterAsync(User user)
        {
            var validationContext = new ValidationContext(user);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(user, validationContext, results, true))
                return false;

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email = @Email", conn);
            checkCmd.Parameters.AddWithValue("Email", user.Email);
            var exists = (long)await checkCmd.ExecuteScalarAsync();
            if (exists > 0) return false;

            using var insertCmd = new NpgsqlCommand("INSERT INTO users (ad, email, sifre) VALUES (@Ad, @Email, @Sifre)", conn);
            insertCmd.Parameters.AddWithValue("Ad", user.Ad);
            insertCmd.Parameters.AddWithValue("Email", user.Email);
            insertCmd.Parameters.AddWithValue("Sifre", user.Sifre);
            await insertCmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<(bool Success, int UserId, string Ad, string Email)> ValidateLoginAsync(string email, string sifre)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand("SELECT id, ad, email FROM users WHERE email = @Email AND sifre = @Sifre", conn);
            cmd.Parameters.AddWithValue("Email", email);
            cmd.Parameters.AddWithValue("Sifre", sifre);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (true, reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
            }

            return (false, 0, "", "");
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand("SELECT id, ad, email FROM users WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Ad = reader.GetString(1),
                    Email = reader.GetString(2)
                };
            }

            return null;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Eğer veritabanı FOREIGN KEY CASCADE ile yapılandırıldıysa, sadece users tablosundan silmek yeterlidir.
            var cmd = new NpgsqlCommand("DELETE FROM users WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdatePasswordAsync(int id, string oldPassword, string newPassword)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE id = @id AND sifre = @sifre", conn);
            checkCmd.Parameters.AddWithValue("id", id);
            checkCmd.Parameters.AddWithValue("sifre", oldPassword);
            var exists = (long)await checkCmd.ExecuteScalarAsync();
            if (exists == 0) return false;

            var updateCmd = new NpgsqlCommand("UPDATE users SET sifre = @yeni WHERE id = @id", conn);
            updateCmd.Parameters.AddWithValue("yeni", newPassword);
            updateCmd.Parameters.AddWithValue("id", id);
            await updateCmd.ExecuteNonQueryAsync();
            return true;
        }
    }
}
