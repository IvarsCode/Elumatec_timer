using System;
using System.Linq;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.Data
{
    public static class InterventieFormRepository
    {
        public static void Save(
            AppDbContext db,
            Interventie? existing,
            string bedrijfsnaam,
            string machine,
            DateTimeOffset datumLaatsteCall,
            int aantalCalls,
            int totaleLooptijdInSeconden,
            string interneNotities,
            string externeNotities,
            int contactpersoonId,
            string cpNaam,
            string cpEmail,
            string cpTelefoon)
        {
            // If contactpersoonId is -1, create a new Contactpersoon
            if (contactpersoonId == -1)
            {
                var newCP = new Contactpersoon
                {
                    Naam = cpNaam,
                    Email = cpEmail,
                    TelefoonNummer = cpTelefoon
                };
                db.Contactpersonen.Add(newCP);
                db.SaveChanges();
                contactpersoonId = newCP.Id;
            }

            // Get the recent MedewerkerId from AppState

            var state = db.Set<AppState>().FirstOrDefault(s => s.Key == "RecentMedewerkerId");

            int interneMedewerkerId = 0; // default fallback
            if (state != null && int.TryParse(state.Value, out var medewerkerId))
            {
                interneMedewerkerId = medewerkerId;
            }

            if (existing != null)
            {
                // Update existing
                existing.DatumRecentsteCall = datumLaatsteCall.DateTime;
                existing.AantalCalls = aantalCalls;
                existing.TotaleLooptijd = totaleLooptijdInSeconden;
                existing.InterneNotities = interneNotities;
                existing.ExterneNotities = externeNotities;
                existing.ContactpersoonId = contactpersoonId;

                // Keep existing InterneMedewerkerId if already set
                if (existing.InterneMedewerkerId == 0)
                    existing.InterneMedewerkerId = interneMedewerkerId;

                db.Interventies.Update(existing);
            }
            else
            {
                // Create new Interventie
                var interventie = new Interventie
                {
                    Bedrijfsnaam = bedrijfsnaam,
                    Machine = machine,
                    InterneMedewerkerId = interneMedewerkerId,
                    DatumRecentsteCall = datumLaatsteCall.DateTime,
                    AantalCalls = aantalCalls,
                    TotaleLooptijd = totaleLooptijdInSeconden,
                    InterneNotities = interneNotities,
                    ExterneNotities = externeNotities,
                    ContactpersoonId = contactpersoonId
                };

                db.Interventies.Add(interventie);
            }

            db.SaveChanges();
        }
    }
}
