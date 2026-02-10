using System;
using System.Collections.Generic;
using System.Linq;
using Elumatec.Tijdregistratie.Models;
using Microsoft.EntityFrameworkCore;

namespace Elumatec.Tijdregistratie.Data
{
    public enum InterventieFilterType
    {
        Bedrijfsnaam,
        Machine,
        Datum
    }

    public static class InterventieRepository
    {
        public static List<Interventie> GetAll(AppDbContext db)
        {
            try
            {
                var interventies = db.Interventies
                    .Include(i => i.Calls)
                    .ToList();

                return interventies
                    .OrderByDescending(i => GetMostRecentCallDate(i))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetAll Interventies] Exception: {ex}");
                return new List<Interventie>();
            }
        }

        public static Interventie? GetById(AppDbContext db, int id)
        {
            try
            {
                return db.Interventies
                    .Include(i => i.Calls)
                    .FirstOrDefault(i => i.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetInterventieById] Exception: {ex}");
                return null;
            }
        }

        public static void Add(AppDbContext db, Interventie interventie)
        {
            try
            {
                db.Interventies.Add(interventie);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Add Interventie] Exception: {ex}");
            }
        }

        public static void Update(AppDbContext db, Interventie interventie)
        {
            try
            {
                db.Interventies.Update(interventie);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Update Interventie] Exception: {ex}");
            }
        }

        public static List<Interventie> GetFiltered(
            AppDbContext db,
            InterventieFilterType filter,
            string? searchText,
            DateTimeOffset? fromDate,
            DateTimeOffset? toDate)
        {
            try
            {
                var query = db.Interventies
                    .Include(i => i.Calls)
                    .AsQueryable();

                switch (filter)
                {
                    case InterventieFilterType.Bedrijfsnaam:
                        if (!string.IsNullOrWhiteSpace(searchText))
                            query = query.Where(i =>
                                EF.Functions.Like(i.BedrijfNaam, $"%{searchText}%"));
                        break;

                    case InterventieFilterType.Machine:
                        if (!string.IsNullOrWhiteSpace(searchText))
                            query = query.Where(i =>
                                EF.Functions.Like(i.Machine, $"%{searchText}%"));
                        break;

                    case InterventieFilterType.Datum:
                        var allForDateFilter = query.ToList();

                        if (fromDate.HasValue || toDate.HasValue)
                        {
                            allForDateFilter = allForDateFilter.Where(i =>
                                i.Calls.Any(call =>
                                    (!fromDate.HasValue || (call.StartCall.HasValue && call.StartCall.Value >= fromDate.Value.DateTime)) &&
                                    (!toDate.HasValue || (call.EindCall.HasValue && call.EindCall.Value <= toDate.Value.DateTime))
                                )).ToList();
                        }

                        return allForDateFilter
                            .OrderBy(i => GetMostRecentCallDate(i))
                            .ToList();
                }

                var results = query.ToList();

                return results
                    .OrderByDescending(i => GetMostRecentCallDate(i))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetFiltered Interventies] Exception: {ex}");
                return new List<Interventie>();
            }
        }

        private static DateTime GetMostRecentCallDate(Interventie interventie)
        {
            var mostRecentCall = interventie.Calls
                .Where(c => c.StartCall.HasValue)
                .OrderByDescending(c => c.StartCall)
                .FirstOrDefault();

            return mostRecentCall?.StartCall ?? DateTime.MinValue;
        }
    }
}