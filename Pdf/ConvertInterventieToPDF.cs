using System;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.IO.Font;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Layout.Borders;

namespace Elumatec.Tijdregistratie.Pdf.ConvertInterventieToPDF
{
    class ServiceBonPdf
    {
        public string GeneratePdf(
            string bedrijfsnaam,
            string machine,
            string interneNotities,
            string externeNotities,
            string medewerkerNaam,
            TimeSpan totalTime)
        {
            string outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string filePath = System.IO.Path.Combine(
                outputFolder,
                $"Werkbon_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            );

            try
            {
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
                    .Add(new Paragraph("Contact:\nservice.nl@voilap.com\n+31 180 315 858")
                        .SetFontSize(9))
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetBorder(Border.NO_BORDER));

                doc.Add(header);
                // doc.Add(new LineSeparator(new SolidLine()));

                doc.Add(new Paragraph(" "));

                // ===== ALGEMEEN =====
                doc.Add(new Paragraph("Algemeen").SetFont(boldFont));

                Table algemeen = new Table(new float[] { 30, 70 }).UseAllAvailableWidth();
                AddRow(algemeen, "Servicemonteur:", medewerkerNaam, boldFont, normalFont);
                AddRow(algemeen, "Uitvoerdatum:", DateTime.Now.ToString("dd-MM-yyyy HH:mm"), boldFont, normalFont);
                AddRow(algemeen, "Type werkzaamheden:", "Installatie nieuwe machine", boldFont, normalFont);
                doc.Add(algemeen);

                doc.Add(new Paragraph(" "));

                // ===== ADRES =====
                doc.Add(new Paragraph("Uitvoeringsadres").SetFont(boldFont));

                Table adres = new Table(new float[] { 30, 70 }).UseAllAvailableWidth();
                AddRow(adres, "Naam:", bedrijfsnaam, boldFont, normalFont);
                AddRow(adres, "Adres:", "Wim Rötherlaan 16", boldFont, normalFont);
                AddRow(adres, "Postcode / Plaats:", "5051JS GOIRLE", boldFont, normalFont);
                doc.Add(adres);

                doc.Add(new Paragraph(" "));

                // ===== MACHINE =====
                doc.Add(new Paragraph("Machine(s)").SetFont(boldFont));

                Table machineTbl = new Table(new float[] { 30, 70 }).UseAllAvailableWidth();
                AddRow(machineTbl, "Omschrijving:", machine, boldFont, normalFont);
                AddRow(machineTbl, "Besturing:", "Elumatec", boldFont, normalFont);
                doc.Add(machineTbl);

                doc.Add(new Paragraph(" "));

                // ===== TIJDEN =====
                doc.Add(new Paragraph("Gewerkte tijd(en)").SetFont(boldFont));

                Table tijden = new Table(new float[] { 25, 25, 25, 25 }).UseAllAvailableWidth();
                tijden.AddHeaderCell(Header("Begintijd", boldFont));
                tijden.AddHeaderCell(Header("Eindtijd", boldFont));
                tijden.AddHeaderCell(Header("Totaal", boldFont));
                tijden.AddHeaderCell(Header("Omschrijving", boldFont));

                // for each call add a row with the times and description, for demo purposes we add 2 identical rows
                tijden.AddCell("08:30");
                tijden.AddCell("12:15");
                tijden.AddCell(totalTime.ToString(@"hh\:mm"));
                tijden.AddCell("Werkuren servicedienst");

                tijden.AddCell("08:30");
                tijden.AddCell("12:15");
                tijden.AddCell(totalTime.ToString(@"hh\:mm"));
                tijden.AddCell("Werkuren servicedienst");

                doc.Add(tijden);

                doc.Add(new Paragraph(" "));

                // ===== NOTITIES =====
                doc.Add(new Paragraph("Uitgevoerde werkzaamheden").SetFont(boldFont));
                doc.Add(new Paragraph(externeNotities ?? "-").SetFont(normalFont));

                doc.Add(new Paragraph(" "));

                doc.Add(new Paragraph("Interne notities").SetFont(boldFont));
                doc.Add(new Paragraph(interneNotities ?? "-").SetFont(normalFont));

                // ===== FOOTER =====
                doc.Add(new Paragraph("\n"));
                // doc.Add(new LineSeparator(new SolidLine()));
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
