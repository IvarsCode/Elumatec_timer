using System;
using System.Collections.Generic;
using System.Linq;
using Elumatec.Tijdregistratie.Models;
using Microsoft.EntityFrameworkCore;

namespace Elumatec.Tijdregistratie.Data
{
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
    }
}
