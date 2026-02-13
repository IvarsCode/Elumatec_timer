using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.Models;

class BedrijvenLaden
{
    // csvPath parameter is optional; when null it will look next to the executable
    public static void LoadBedrijvenCsvToDb(AppDbContext db, string? csvPath = null)
    {
        try
        {
            if (string.IsNullOrEmpty(csvPath))
            {
                // published application: the build copies the CSV preserving the
                // Data subdirectory, so first try that location. fall back to the
                // exe directory root for backwards compatibility.
                var exeDir = AppContext.BaseDirectory;
                var candidate1 = Path.Combine(exeDir, "Data", "bedrijvenLijst.csv");
                var candidate2 = Path.Combine(exeDir, "bedrijvenLijst.csv");

                if (File.Exists(candidate1))
                {
                    csvPath = candidate1;
                }
                else if (File.Exists(candidate2))
                {
                    csvPath = candidate2;
                }
                else
                {
                    Console.WriteLine($"CSV file not found in expected locations:");
                    Console.WriteLine($"  {candidate1}");
                    Console.WriteLine($"  {candidate2}");
                    return;
                }
            }

            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"CSV file not found at: {csvPath}");
                return;
            }

            var lines = File.ReadAllLines(csvPath);
            var bedrijven = new List<Bedrijf>();

            int startIndex = 0;
            if (lines.Length > 0 && !int.TryParse(SplitCsvLine(lines[0])[0], out _))
                startIndex = 1;

            for (int i = startIndex; i < lines.Length; i++)
            {
                try
                {
                    var parts = SplitCsvLine(lines[i]);

                    if (parts.Length < 7)
                    {
                        Console.WriteLine($"Skipping line {i + 1}: insufficient columns");
                        continue;
                    }

                    var klantId = int.Parse(parts[0]);
                    var bedrijfNaam = parts[1];

                    if (bedrijfNaam.StartsWith("XXX", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (bedrijfNaam.StartsWith("TEST ", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Check if this klantId already exists
                    if (db.Bedrijven.Any(b => b.klantId == klantId))
                    {
                        Console.WriteLine($"Skipping duplicate klantId: {klantId}");
                        continue;
                    }

                    var b = new Bedrijf
                    {
                        klantId = klantId,
                        BedrijfNaam = parts[1],
                        StraatNaam = string.IsNullOrWhiteSpace(parts[2]) ? null : parts[2],
                        AdresNummer = string.IsNullOrWhiteSpace(parts[3]) ? null : parts[3],
                        Postcode = string.IsNullOrWhiteSpace(parts[4]) ? null : parts[4],
                        Stad = string.IsNullOrWhiteSpace(parts[5]) ? null : parts[5],
                        Land = string.IsNullOrWhiteSpace(parts[6]) ? null : parts[6]
                    };

                    bedrijven.Add(b);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing line {i + 1}: {ex.Message}");
                }
            }

            if (bedrijven.Count > 0)
            {
                db.Bedrijven.AddRange(bedrijven);
                db.SaveChanges();
                Console.WriteLine($"{bedrijven.Count} companies added to the database.");
            }
            else
            {
                Console.WriteLine("No companies to add from CSV.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading companies: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = "";

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
            }
            else current += c;
        }

        result.Add(current.Trim());
        return result.ToArray();
    }
}