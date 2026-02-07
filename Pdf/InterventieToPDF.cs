using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.Pdf
{
    public static class InterventiesPdfExporter
    {
        /// <summary>
        /// Export interventies from database directly using Entity Framework Core
        /// </summary>
        public static void ExportFromDb(AppDbContext db, string pdfPath)
        {
            try
            {
                // Get all interventies from database using EF Core
                var interventies = db.Interventies
                    .AsNoTracking()
                    .OrderByDescending(i => i.DatumRecentsteCall)
                    .ToList();

                GeneratePdf(interventies, pdfPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting interventies to PDF: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Export a single interventie to PDF
        /// </summary>
        public static void ExportSingleInterventie(Interventie interventie, string pdfPath)
        {
            try
            {
                var interventies = new List<Interventie> { interventie };
                GeneratePdf(interventies, pdfPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting interventie to PDF: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Legacy method that opens a direct SQLite connection (kept for backward compatibility)
        /// </summary>
        public static void Export(string dbPath, string pdfPath)
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            string query = @"
                SELECT 
                    Bedrijfsnaam,
                    Machine,
                    DatumRecentsteCall,
                    AantalCalls,
                    TotaleLooptijd,
                    InterneNotities,
                    ExterneNotities
                FROM interventies
                ORDER BY DatumRecentsteCall DESC
            ";

            using var command = new Microsoft.Data.Sqlite.SqliteCommand(query, connection);
            using var reader = command.ExecuteReader();

            var interventies = new List<(string Bedrijf, string Machine, DateTime? Datum, int Calls, int Looptijd, string InterneNotities, string ExterneNotities)>();

            while (reader.Read())
            {
                interventies.Add((
                    reader["Bedrijfsnaam"]?.ToString() ?? "",
                    reader["Machine"]?.ToString() ?? "",
                    reader["DatumRecentsteCall"] as DateTime?,
                    reader["AantalCalls"] as int? ?? 0,
                    reader["TotaleLooptijd"] as int? ?? 0,
                    reader["InterneNotities"]?.ToString() ?? "",
                    reader["ExterneNotities"]?.ToString() ?? ""
                ));
            }

            GeneratePdfLegacy(interventies, pdfPath);
        }

        private static void GeneratePdf(List<Interventie> interventies, string pdfPath)
        {
            // PDF setup
            PdfWriter writer = new PdfWriter(pdfPath);
            PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            // Fonts
            PdfFont headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Title
            document.Add(
                new Paragraph("Interventies Overzicht")
                    .SetFont(headerFont)
                    .SetFontSize(18)
            );

            document.Add(new Paragraph($"Gegenereerd op: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").SetFont(normalFont).SetFontSize(10));
            document.Add(new Paragraph("\n"));

            // Tabel (7 kolommen)
            Table table = new Table(7).UseAllAvailableWidth();

            string[] headers =
            {
                "Bedrijf",
                "Machine",
                "Datum laatste call",
                "Aantal calls",
                "Totale looptijd",
                "Interne notities",
                "Externe notities"
            };

            // Header row
            foreach (var header in headers)
            {
                table.AddHeaderCell(
                    new Cell()
                        .Add(new Paragraph(header).SetFont(headerFont))
                );
            }

            // Data rows
            foreach (var interventie in interventies)
            {
                table.AddCell(new Paragraph(interventie.Bedrijfsnaam ?? "").SetFont(normalFont));
                table.AddCell(new Paragraph(interventie.Machine ?? "").SetFont(normalFont));
                table.AddCell(new Paragraph(interventie.DatumRecentsteCall?.ToString("dd/MM/yyyy HH:mm") ?? "").SetFont(normalFont));
                table.AddCell(new Paragraph((interventie.AantalCalls).ToString()).SetFont(normalFont));
                table.AddCell(new Paragraph(FormatSeconds(interventie.TotaleLooptijd)).SetFont(normalFont));
                table.AddCell(new Paragraph(interventie.InterneNotities ?? "").SetFont(normalFont));
                table.AddCell(new Paragraph(interventie.ExterneNotities ?? "").SetFont(normalFont));
            }

            document.Add(table);
            document.Close();
        }

        private static void GeneratePdfLegacy(List<(string Bedrijf, string Machine, DateTime? Datum, int Calls, int Looptijd, string InterneNotities, string ExterneNotities)> interventies, string pdfPath)
        {
            // PDF setup
            PdfWriter writer = new PdfWriter(pdfPath);
            PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            // Fonts
            PdfFont headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Title
            document.Add(
                new Paragraph("Interventies Overzicht")
                    .SetFont(headerFont)
                    .SetFontSize(18)
            );

            document.Add(new Paragraph($"Gegenereerd op: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").SetFont(normalFont).SetFontSize(10));
            document.Add(new Paragraph("\n"));

            // Tabel (7 kolommen)
            Table table = new Table(7).UseAllAvailableWidth();

            string[] headers =
            {
                "Bedrijf",
                "Machine",
                "Datum laatste call",
                "Aantal calls",
                "Totale looptijd",
                "Interne notities",
                "Externe notities"
            };

            // Header row
            foreach (var header in headers)
            {
                table.AddHeaderCell(
                    new Cell()
                        .Add(new Paragraph(header).SetFont(headerFont))
                );
            }

            // Data rows
            foreach (var interventie in interventies)
            {
                table.AddCell(new Paragraph(interventie.Bedrijf).SetFont(normalFont));
                table.AddCell(new Paragraph(interventie.Machine).SetFont(normalFont));
                table.AddCell(new Paragraph(interventie.Datum?.ToString("dd/MM/yyyy HH:mm") ?? "").SetFont(normalFont));
                table.AddCell(new Paragraph(interventie.Calls.ToString()).SetFont(normalFont));
                table.AddCell(new Paragraph(FormatSeconds(interventie.Looptijd)).SetFont(normalFont));
                table.AddCell(new Paragraph(interventie.InterneNotities).SetFont(normalFont));
                table.AddCell(new Paragraph(interventie.ExterneNotities).SetFont(normalFont));
            }

            document.Add(table);
            document.Close();
        }

        private static string FormatSeconds(int seconds)
        {
            if (seconds <= 0) return "0s";

            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
            else if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m {ts.Seconds}s";
            else
                return $"{ts.Seconds}s";
        }
    }
}
