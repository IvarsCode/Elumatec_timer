using Elumatec.Tijdregistratie.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Elumatec.Tijdregistratie.Data
{
    public class AppStateHelpers
    {
        private readonly AppDbContext _db;

        public AppStateHelpers(AppDbContext db)
        {
            _db = db;
        }

        public void EnsureUniekeTellingExists()
        {
            const string key = "UniekeTelling";

            var state = _db.AppState.FirstOrDefault(s => s.Key == key);

            if (state == null)
            {
                var value = new Random().Next(10, 100);

                _db.AppState.Add(new AppState
                {
                    Key = key,
                    Value = value
                });

                _db.SaveChanges();
            }
        }

        public int GetNextPrefixedId(string tableName)
        {
            EnsureUniekeTellingExists();

            var prefix = _db.AppState
                .Where(s => s.Key == "UniekeTelling")
                .Select(s => s.Value)
                .First();

            int nextIncrement = 1;

            try
            {
                var wasOpen = _db.Database.GetDbConnection().State == System.Data.ConnectionState.Open;

                if (!wasOpen)
                    _db.Database.OpenConnection();

                using var command = _db.Database.GetDbConnection().CreateCommand();

                // Parameterized to prevent SQL injection (though table name can't be parameterized)
                // Validate tableName to prevent injection
                if (!IsValidTableName(tableName))
                    throw new ArgumentException("Invalid table name", nameof(tableName));

                command.CommandText = $"SELECT MAX(id) FROM {tableName}";

                var result = command.ExecuteScalar();

                if (result != DBNull.Value && result != null)
                {
                    var maxId = Convert.ToInt32(result);
                    nextIncrement = (maxId % 100000) + 1;
                }

                if (!wasOpen)
                    _db.Database.CloseConnection();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting next ID: {ex.Message}");
                throw;
            }

            return (prefix * 100000) + nextIncrement;
        }

        private bool IsValidTableName(string tableName)
        {
            // Only allow alphanumeric and underscores
            return !string.IsNullOrWhiteSpace(tableName) &&
                   tableName.All(c => char.IsLetterOrDigit(c) || c == '_');
        }
    }
}