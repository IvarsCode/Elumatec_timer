using System;
using System.ComponentModel.DataAnnotations;

namespace Elumatec.Tijdregistratie.Models;

public class Machine
{
    [Required]
    public string MachineNaam { get; set; } = null!;
}
