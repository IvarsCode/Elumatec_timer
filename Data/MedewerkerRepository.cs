using System;
using System.Collections.Generic;
using System.Linq;
using Elumatec.Tijdregistratie.Models;
using Microsoft.EntityFrameworkCore;

namespace Elumatec.Tijdregistratie.Data
{
    public static class MedewerkerRepository
    {
        private const string RecentUserKey = "RecentMedewerkerId";

        // 🔹 Get recent user from AppState safely
        public static Medewerker? GetRecentUser(AppDbContext db)
        {
            try
            {
                var state = db.Set<AppState>()
                    .FirstOrDefault(s => s.Key == RecentUserKey);

                if (state == null)
                    return null;

                if (!int.TryParse(state.Value, out var medewerkerId))
                    return null;

                return db.Medewerkers.FirstOrDefault(m => m.Id == medewerkerId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetRecentUser] Exception: {ex}");
                return null;
            }
        }

        // 🔹 Save recent user to AppState
        public static void SaveRecentUser(AppDbContext db, int medewerkerId)
        {
            try
            {
                var state = db.Set<AppState>()
                    .FirstOrDefault(s => s.Key == RecentUserKey);

                if (state == null)
                {
                    state = new AppState
                    {
                        Key = RecentUserKey,
                        Value = medewerkerId.ToString()
                    };
                    db.Add(state);
                }
                else
                {
                    state.Value = medewerkerId.ToString();
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveRecentUser] Exception: {ex}");
            }
        }

        // 🔹 Search method: returns max 4 best matches
        public static List<Medewerker> Search(AppDbContext db, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Medewerker>();

            return db.Medewerkers
                .Where(m => EF.Functions.Like(m.Naam, $"%{searchTerm}%"))
                .OrderBy(m => m.Naam)
                .Take(4) // Only return the first 4 matches
                .ToList();
        }
    }
}
