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
    string? straatNaam,
    string? adresNummer,
    string? postcode,
    string? stad,
    string? land,
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

            Interventie interventie;

            if (existing != null)
            {
                interventie = db.Interventies.First(i => i.Id == existing.Id);

                interventie.Machine = machine;
                interventie.BedrijfNaam = bedrijfsnaam;
                interventie.KlantId = klantId;
                interventie.StraatNaam = straatNaam;
                interventie.AdresNummer = adresNummer;
                interventie.Postcode = postcode;
                interventie.Stad = stad;
                interventie.Land = land;

                // Create call
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

                interventie.TotaleLooptijd += callDurationSeconds;
                interventie.IdRecentsteCall = newCall.Id;

                db.SaveChanges();
            }
            else
            {
                interventie = new Interventie
                {
                    Id = helpers.GetNextPrefixedId("interventies"),
                    Machine = machine,
                    BedrijfNaam = bedrijfsnaam,
                    KlantId = klantId,
                    StraatNaam = straatNaam,
                    AdresNummer = adresNummer,
                    Postcode = postcode,
                    Stad = stad,
                    Land = land,
                    TotaleLooptijd = callDurationSeconds,
                    Afgerond = 0,
                    IdRecentsteCall = 0
                };
                db.Interventies.Add(interventie);
                db.SaveChanges();

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

                interventie.IdRecentsteCall = newCall.Id;
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