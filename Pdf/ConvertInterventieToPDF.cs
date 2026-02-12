using System;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.IO.Font;

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
            // Safe output folder (works with OneDrive)
            string outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string filePath = Path.Combine(
                outputFolder,
                $"ServiceBon_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            );

            try
            {
                using (PdfWriter writer = new PdfWriter(filePath))
                using (PdfDocument pdfDoc = new PdfDocument(writer))
                using (Document document = new Document(pdfDoc))
                {
                    // Fonts (explicit encoding = stable)
                    PdfFont boldFont = PdfFontFactory.CreateFont(
                        StandardFonts.HELVETICA_BOLD,
                        PdfEncodings.WINANSI,
                        PdfFontFactory.EmbeddingStrategy.PREFER_NOT_EMBEDDED
                    );

                    PdfFont normalFont = PdfFontFactory.CreateFont(
                        StandardFonts.HELVETICA,
                        PdfEncodings.WINANSI,
                        PdfFontFactory.EmbeddingStrategy.PREFER_NOT_EMBEDDED
                    );

                    // Title
                    document.Add(
                        new Paragraph("SERVICEBON")
                            .SetFont(boldFont)
                            .SetFontSize(20)
                            .SetTextAlignment(TextAlignment.CENTER)
                    );

                    document.Add(new Paragraph(" "));

                    // Customer info
                    document.Add(new Paragraph("Klantgegevens").SetFont(boldFont));
                    document.Add(new Paragraph($"Bedrijf: {bedrijfsnaam}").SetFont(normalFont));
                    document.Add(new Paragraph($"Machine: {machine}").SetFont(normalFont));
                    document.Add(new Paragraph(" "));

                    // Service info
                    document.Add(new Paragraph("Servicegegevens").SetFont(boldFont));
                    document.Add(new Paragraph($"Datum: {DateTime.Now:dd-MM-yyyy}").SetFont(normalFont));
                    document.Add(new Paragraph($"Monteur: {medewerkerNaam}").SetFont(normalFont));
                    document.Add(new Paragraph($"Tijd: {totalTime:hh\\:mm\\:ss}").SetFont(normalFont));
                    document.Add(new Paragraph(" "));

                    // Internal notes
                    if (!string.IsNullOrWhiteSpace(interneNotities))
                    {
                        document.Add(new Paragraph("Interne notities").SetFont(boldFont));
                        document.Add(new Paragraph(interneNotities).SetFont(normalFont));
                        document.Add(new Paragraph(" "));
                    }

                    // External notes
                    if (!string.IsNullOrWhiteSpace(externeNotities))
                    {
                        document.Add(new Paragraph("Externe notities").SetFont(boldFont));
                        document.Add(new Paragraph(externeNotities).SetFont(normalFont));
                        document.Add(new Paragraph(" "));
                    }

                    // Signatures
                    document.Add(new Paragraph("Handtekening klant:").SetFont(normalFont));
                    document.Add(new Paragraph("______________________________"));
                    document.Add(new Paragraph(" "));

                    document.Add(new Paragraph("Handtekening monteur:").SetFont(normalFont));
                    document.Add(new Paragraph("______________________________"));
                }
            }
            catch (Exception ex)
            {
                // Full exception for debugging
                throw new Exception("Error creating PDF:\n" + ex, ex);
            }

            // Final verification
            if (!File.Exists(filePath))
                throw new Exception("PDF file was not created.");

            if (new FileInfo(filePath).Length == 0)
                throw new Exception("PDF file is empty.");

            return filePath;
        }
    }
}
