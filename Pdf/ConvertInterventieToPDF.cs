using System;
using System.Collections.Generic;
using System.Linq;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.Pdf.ConvertInterventieToPDF
{
    class ServiceBonPdf
    {
        private readonly AppDbContext _db;

        public ServiceBonPdf(AppDbContext db)
        {
            _db = db;
        }

        public string GeneratePdf(int interventieId, string medewerkerNaam)
        {
            string outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = System.IO.Path.Combine(
                outputFolder,
                $"Werkbon_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            );

            try
            {
                var interventie = GetInterventie(interventieId);
                var calls = GetInterventieCalls(interventieId);

                if (interventie == null)
                    throw new Exception($"Interventie with ID {interventieId} not found");

                using var writer = new PdfWriter(filePath);
                using var pdf = new PdfDocument(writer);
                using var doc = new Document(pdf, PageSize.A4);
                doc.SetMargins(30, 30, 30, 30);

                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                // ===== HEADER =====
                Table header = new Table(new float[] { 70, 30 }).UseAllAvailableWidth();
                header.AddCell(new Cell()
                    .Add(new Paragraph("WERKBON - WB-2025-1846").SetFont(boldFont))
                    .SetBorder(Border.NO_BORDER));

                header.AddCell(new Cell()
                    .Add(new Paragraph("Contact:\nservice.nl@voilap.com\n+31 180 315 858").SetFontSize(11))
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetBorder(Border.NO_BORDER));

                doc.Add(header);
                doc.Add(new Paragraph("-------------------------------------------------------------------------------------------------------------------------------------"));

                // ===== ALGEMEEN =====
                doc.Add(new Paragraph("Algemeen").SetFont(boldFont).SetFontSize(20));
                Table algemeen = new Table(new float[] { 50, 50 }).UseAllAvailableWidth();
                AddRow(algemeen, "Servicemonteur:", medewerkerNaam, boldFont, normalFont);
                AddRow(algemeen, "Uitvoerdatum:", DateTime.Now.ToString("dd-MM-yyyy HH:mm"), boldFont, normalFont);
                AddRow(algemeen, "Type werkzaamheden:", "Service / interventie", boldFont, normalFont);
                doc.Add(algemeen);
                doc.Add(new Paragraph("-------------------------------------------------------------------------------------------------------------------------------------"));

                // ===== ADRES =====
                doc.Add(new Paragraph("Uitvoeringsadres").SetFont(boldFont).SetFontSize(20));
                Table adres = new Table(new float[] { 50, 50 }).UseAllAvailableWidth();
                AddRow(adres, "Bedrijf:", interventie.BedrijfNaam ?? "-", boldFont, normalFont);
                AddRow(adres, "Adress", (interventie.StraatNaam ?? "-") + " " + (interventie.AdresNummer ?? ""), boldFont, normalFont);
                AddRow(adres, "Postcode:", interventie.Postcode ?? "-", boldFont, normalFont);
                AddRow(adres, "Stad:", interventie.Stad ?? "-", boldFont, normalFont);
                AddRow(adres, "Land:", interventie.Land ?? "-", boldFont, normalFont);
                doc.Add(adres);

                doc.Add(new Paragraph("-------------------------------------------------------------------------------------------------------------------------------------"));

                // ===== MACHINE =====
                doc.Add(new Paragraph("Machine(s)").SetFont(boldFont).SetFontSize(20));
                Table machineTbl = new Table(new float[] { 50, 50 }).UseAllAvailableWidth();
                AddRow(machineTbl, "Omschrijving:", interventie.Machine ?? "-", boldFont, normalFont);
                AddRow(machineTbl, "Besturing:", "Elumatec", boldFont, normalFont);
                doc.Add(machineTbl);
                doc.Add(new Paragraph("-------------------------------------------------------------------------------------------------------------------------------------"));

                // ===== TIJDEN =====
                doc.Add(new Paragraph("Gewerkte tijd(en)").SetFont(boldFont).SetFontSize(20));
                Table tijden = new Table(new float[] { 25, 25, 10, 25, 15 }).UseAllAvailableWidth();
                tijden.AddHeaderCell(Header("Begintijd", boldFont));
                tijden.AddHeaderCell(Header("Eindtijd", boldFont));
                tijden.AddHeaderCell(Header("Totaal", boldFont));
                tijden.AddHeaderCell(Header("ContactPersoon", boldFont));
                tijden.AddHeaderCell(Header("Omschrijving", boldFont));
                int i = 0;
                foreach (var call in calls)
                {
                    i++;
                    // Safely handle nullable start/end times
                    string startText = call.StartCall.HasValue ? call.StartCall.Value.ToString() : "-";
                    string endText = call.EindCall.HasValue ? call.EindCall.Value.ToString() : "-";

                    string durationText;
                    if (call.StartCall.HasValue && call.EindCall.HasValue)
                    {
                        var duration = call.EindCall.Value - call.StartCall.Value;
                        durationText = duration.ToString(@"hh\:mm\:ss");
                    }
                    else
                    {
                        durationText = "-";
                    }

                    string contactNaam = call.ContactpersoonNaam ?? "-";

                    tijden.AddCell(startText);
                    tijden.AddCell(endText);
                    tijden.AddCell(durationText);
                    tijden.AddCell(contactNaam);
                    tijden.AddCell("Call number: " + i);
                }
                doc.Add(tijden);
                var total = TimeSpan.FromTicks(
                    calls
                        .Where(c => c.StartCall.HasValue && c.EindCall.HasValue)
                        .Sum(c => (c.EindCall!.Value - c.StartCall!.Value).Ticks)
                );
                doc.Add(
                    new Paragraph(
                        "Totaal gewerkte tijd: " +
                        total.ToString(@"hh\:mm\:ss")
                    ).SetFont(boldFont)
                );




                doc.Add(new Paragraph(" "));

                // ===== NOTITIES =====
                doc.Add(new Paragraph("Externe notities").SetFont(boldFont).SetFontSize(20));
                var externeNotities = string.Join("\n", calls.Select(c => c.ExterneNotities ?? "-"));
                doc.Add(new Paragraph(externeNotities).SetFont(normalFont));
                doc.Add(new Paragraph(" "));

                doc.Add(new Paragraph("Contact Personen informatie").SetFont(boldFont).SetFontSize(20));
                List<string> Contactpersonen = new List<string>();
                foreach (var call in calls)
                {
                    var contactNaam = call.ContactpersoonNaam ?? "-";
                    if (!Contactpersonen.Contains(contactNaam) && contactNaam != "-")
                    {
                        Contactpersonen.Add(contactNaam);
                        doc.Add(new Paragraph(contactNaam).SetFont(boldFont).SetFontSize(14));
                        doc.Add(new Paragraph("Telefoonnummer: " + (call.ContactpersoonTelefoonNummer ?? "-")).SetFont(normalFont));
                        doc.Add(new Paragraph("Email: " + (call.ContactpersoonEmail ?? "-")).SetFont(normalFont));
                        doc.Add(new Paragraph(" "));
                    }
                }

                // ===== FOOTER =====
                doc.Add(new Paragraph("\n"));
                doc.Add(new Paragraph(
                    "Voilàp Netherlands B.V. | Hoogeveenenweg 204 | 2913 LV Nieuwerkerk a/d IJssel | www.elumatec.com")
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER));
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating PDF:\n" + ex, ex);
            }

            return filePath;
        }

        // ===== DATABASE METHODS =====
        private Interventie? GetInterventie(int id)
        {
            try
            {
                return InterventieRepository.GetById(_db, id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetInterventie PDF] Exception: {ex}");
                return null;
            }
        }

        private List<InterventieCall> GetInterventieCalls(int interventieId)
        {
            try
            {
                var interventie = InterventieRepository.GetById(_db, interventieId);
                return interventie?.Calls?.ToList() ?? new List<InterventieCall>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetInterventieCalls PDF] Exception: {ex}");
                return new List<InterventieCall>();
            }
        }

        // ===== HELPERS =====
        private static void AddRow(Table table, string label, string value, PdfFont bold, PdfFont normal)
        {
            table.AddCell(new Cell().Add(new Paragraph(label).SetFont(bold)).SetBorder(Border.NO_BORDER));
            table.AddCell(new Cell().Add(new Paragraph(value).SetFont(normal)).SetBorder(Border.NO_BORDER));
        }

        private static Cell Header(string text, PdfFont bold) =>
            new Cell()
                .Add(new Paragraph(text).SetFont(bold))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY);
    }
}
