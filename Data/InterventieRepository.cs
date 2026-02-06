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
                return db.Interventies
                    .OrderByDescending(i => i.DatumRecentsteCall)
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
                var query = db.Interventies.AsQueryable();

                switch (filter)
                {
                    case InterventieFilterType.Bedrijfsnaam:
                        if (!string.IsNullOrWhiteSpace(searchText))
                            query = query.Where(i =>
                                EF.Functions.Like(i.Bedrijfsnaam, $"%{searchText}%"));
                        break;

                    case InterventieFilterType.Machine:
                        if (!string.IsNullOrWhiteSpace(searchText))
                            query = query.Where(i =>
                                EF.Functions.Like(i.Machine, $"%{searchText}%"));
                        break;

                    case InterventieFilterType.Datum:
                        if (fromDate.HasValue)
                            query = query.Where(i =>
                                i.DatumRecentsteCall >= fromDate.Value.DateTime);

                        if (toDate.HasValue)
                            query = query.Where(i =>
                                i.DatumRecentsteCall <= toDate.Value.DateTime);
                        break;
                }

                if (filter == InterventieFilterType.Datum)
                {
                    query = query.OrderBy(i => i.DatumRecentsteCall);
                }
                else
                {
                    query = query.OrderByDescending(i => i.DatumRecentsteCall);
                }

                return query.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetFiltered Interventies] Exception: {ex}");
                return new List<Interventie>();
            }
        }
    }
}
