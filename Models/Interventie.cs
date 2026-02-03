using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Elumatec.Tijdregistratie.Models;

public class Interventie
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Bedrijfsnaam { get; set; } = null!;

    [Required]
    public int ContactpersoonId { get; set; }
    public Contactpersoon Contactpersoon { get; set; } = null!;

    [Required]
    public string Machine { get; set; } = null!;

    // changed to a foreign key to Medewerker.Id for consistency
    [Required]
    public int InterneMedewerkerId { get; set; }
    public Medewerker InterneMedewerker { get; set; } = null!;

    public DateTime? DatumRecentsteCall { get; set; }

    public int AantalCalls { get; set; }

    // tijd in seconden
    public int TotaleLooptijd { get; set; }

    public string? InterneNotities { get; set; }
    public string? ExterneNotities { get; set; }
}