using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Elumatec.Tijdregistratie.Models;

public class Contactpersoon
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Naam { get; set; } = null!;

    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string TelefoonNummer { get; set; } = null!;

    public ICollection<Interventie> Interventies { get; set; } = new List<Interventie>();
}