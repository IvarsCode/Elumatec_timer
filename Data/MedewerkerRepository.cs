using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.Data
{
    public static class MedewerkerRepository
    {
        // Path to the SQLite database in the app folder
        private static string DbPath => Path.Combine(AppContext.BaseDirectory, "elumatec.db");

        // Get all users from the database
        public static List<Medewerker> GetAll()
        {
            var medewerkers = new List<Medewerker>();

            // If the database doesn't exist yet, return empty list
            if (!File.Exists(DbPath))
                return medewerkers;

            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Naam FROM medewerkers ORDER BY Naam";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                medewerkers.Add(new Medewerker
                {
                    Id = reader.GetInt32(0),
                    Naam = reader.GetString(1)
                });
            }

            return medewerkers;
        }

        // Optional: Get filtered users by search term (partial match)
        public static List<Medewerker> Search(string searchTerm)
        {
            var medewerkers = new List<Medewerker>();

            if (!File.Exists(DbPath) || string.IsNullOrWhiteSpace(searchTerm))
                return medewerkers;

            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Naam FROM medewerkers WHERE Naam LIKE $search ORDER BY Naam";
            command.Parameters.AddWithValue("$search", $"%{searchTerm}%");

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                medewerkers.Add(new Medewerker
                {
                    Id = reader.GetInt32(0),
                    Naam = reader.GetString(1)
                });
            }

            return medewerkers;
        }
    }
}
