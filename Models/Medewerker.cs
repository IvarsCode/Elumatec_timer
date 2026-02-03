using System.ComponentModel.DataAnnotations;

namespace Elumatec.Tijdregistratie.Models;

public class Medewerker
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Naam { get; set; } = null!;
}