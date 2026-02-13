using System.Linq;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.Data
{
    public static class DataSeeder
    {
        /// <summary>
        /// Ensures that a minimal set of medewerkers and machines exist in the database.
        /// This method is idempotent and safe to call after migrations.
        /// </summary>
        public static void Seed(AppDbContext db)
        {
            // seed medewerkers
            var medewerkersToEnsure = new[] { "Marcel", "Aad", "Bas" };
            var existing = db.Medewerkers.Select(m => m.Naam).ToHashSet();
            foreach (var name in medewerkersToEnsure)
            {
                if (!existing.Contains(name))
                {
                    db.Medewerkers.Add(new Medewerker { Naam = name });
                }
            }

            // seed machines
            var machinesToEnsure = new[] { "MachineA", "MachineB", "MachineC" };
            var existingMachines = db.Machines.Select(m => m.MachineNaam).ToHashSet();
            foreach (var name in machinesToEnsure)
            {
                if (!existingMachines.Contains(name))
                {
                    db.Machines.Add(new Machine { MachineNaam = name });
                }
            }

            // only save if we added something
            if (db.ChangeTracker.HasChanges())
            {
                db.SaveChanges();
            }
        }
    }
}