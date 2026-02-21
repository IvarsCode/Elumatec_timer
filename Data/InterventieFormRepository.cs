using System;
using System.Linq;
using Elumatec.Tijdregistratie.Models;
using Elumatec.Tijdregistratie.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Elumatec.Tijdregistratie.Data
{
    public static class InterventieFormRepository
    {

        /// Saves or updates an Interventie and creates a new InterventieCall record

        public static void Save(
            AppDbContext db,
            Interventie? existing,
            string bedrijfsnaam,
            string machine,
            int klantId,
            int medewerkerId,
            string contactpersoonNaam,
            string? contactpersoonEmail,
            string? contactpersoonTelefoon,
            string? interneNotities,
            string? externeNotities,
            DateTime? callStartTime,
            DateTime? callEndTime)
        {
            var helpers = new AppStateHelpers(db);

            int callDurationSeconds = 0;
            if (callStartTime.HasValue && callEndTime.HasValue)
            {
                callDurationSeconds = (int)(callEndTime.Value - callStartTime.Value).TotalSeconds;
            }

            if (existing != null)
            {
                // Re-fetch the interventie to ensure it's being tracked
                var interventie = db.Interventies.FirstOrDefault(i => i.Id == existing.Id);

                if (interventie == null)
                {
                    throw new Exception($"Interventie with ID {existing.Id} not found");
                }

                // Update existing interventie
                interventie.Machine = machine;
                interventie.BedrijfNaam = bedrijfsnaam;
                interventie.KlantId = klantId;

                // Create new call record for this session
                var newCall = new InterventieCall
                {
                    Id = helpers.GetNextPrefixedId("interventie_call"),
                    InterventieId = interventie.Id,
                    MedewerkerId = medewerkerId,
                    ContactpersoonNaam = contactpersoonNaam,
                    ContactpersoonEmail = contactpersoonEmail,
                    ContactpersoonTelefoonNummer = contactpersoonTelefoon,
                    InterneNotities = interneNotities,
                    ExterneNotities = externeNotities,
                    StartCall = callStartTime,
                    EindCall = callEndTime
                };

                db.InterventieCalls.Add(newCall);
                db.SaveChanges();

                // Update interventie totals
                interventie.TotaleLooptijd += callDurationSeconds;
                interventie.IdRecentsteCall = newCall.Id;

                db.SaveChanges();
            }
            else
            {
                // Create new interventie
                var newInterventie = new Interventie
                {
                    Id = helpers.GetNextPrefixedId("interventies"),
                    Machine = machine,
                    BedrijfNaam = bedrijfsnaam,
                    KlantId = klantId,
                    TotaleLooptijd = callDurationSeconds,
                    Afgerond = 0,
                    IdRecentsteCall = 0 // Will be updated after call is created
                };

                db.Interventies.Add(newInterventie);
                db.SaveChanges();

                // Create first call for this interventie
                var newCall = new InterventieCall
                {
                    Id = helpers.GetNextPrefixedId("interventie_call"),
                    InterventieId = newInterventie.Id,
                    MedewerkerId = medewerkerId,
                    ContactpersoonNaam = contactpersoonNaam,
                    ContactpersoonEmail = contactpersoonEmail,
                    ContactpersoonTelefoonNummer = contactpersoonTelefoon,
                    InterneNotities = interneNotities,
                    ExterneNotities = externeNotities,
                    StartCall = callStartTime,
                    EindCall = callEndTime
                };

                db.InterventieCalls.Add(newCall);
                db.SaveChanges();

                // Update with the new call ID
                newInterventie.IdRecentsteCall = newCall.Id;
                db.SaveChanges();
            }
        }

        public static void UpdateCall(
    AppDbContext db,
    int callId,
    string contactpersoonNaam,
    string? contactpersoonEmail,
    string? contactpersoonTelefoon,
    string? interneNotities,
    string? externeNotities)
        {
            var call = db.InterventieCalls.FirstOrDefault(c => c.Id == callId);
            if (call == null) throw new Exception($"InterventieCall with ID {callId} not found");

            call.ContactpersoonNaam = contactpersoonNaam;
            call.ContactpersoonEmail = contactpersoonEmail;
            call.ContactpersoonTelefoonNummer = contactpersoonTelefoon;
            call.InterneNotities = interneNotities;
            call.ExterneNotities = externeNotities;

            db.SaveChanges();
        }
    }
}