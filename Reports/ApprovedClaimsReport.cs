using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CMCS_POE_PART_2.Models;

namespace CMCS_POE_PART_2.Reports
{
    public class ApprovedClaimsReport : IDocument
    {
        private readonly List<Claim> _claims;

        public ApprovedClaimsReport(List<Claim> claims)
        {
            _claims = claims;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);

                page.Header().Row(row =>
                {
                    row.RelativeColumn().Text("CMCS Portal").FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                    row.ConstantColumn(100).Image("wwwroot/images/cmcs-logo.png"); // logo
                });

                page.Content().Stack(stack =>
                {
                    stack.Spacing(10);

                    stack.Item().Text("Invoice Report").FontSize(16).Bold();
                    stack.Item().Text($"Generated On: {DateTime.Now:yyyy-MM-dd}");

                    stack.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60); // InvoiceID
                            columns.RelativeColumn();   // Lecturer
                            columns.RelativeColumn();   // Module
                            columns.RelativeColumn();   // Faculty
                            columns.ConstantColumn(80); // Total
                            columns.ConstantColumn(80); // Date
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("InvoiceID").Bold();
                            header.Cell().Text("Lecturer").Bold();
                            header.Cell().Text("Module").Bold();
                            header.Cell().Text("Faculty").Bold();
                            header.Cell().Text("Total (R)").Bold();
                            header.Cell().Text("Date").Bold();
                        });

                        foreach (var c in _claims)
                        {
                            table.Cell().Text(c.claimID.ToString());
                            table.Cell().Text(c.LecturerName);
                            table.Cell().Text(c.module_name);
                            table.Cell().Text(c.faculty_name);
                            table.Cell().Text($"R {c.TotalAmount:F2}");
                            table.Cell().Text(c.creating_date.ToString("yyyy-MM-dd"));
                        }
                        var subtotals = _claims
            .GroupBy(c => c.LecturerName)
            .Select(g => new { Lecturer = g.Key, Total = g.Sum(c => c.TotalAmount) })
            .ToList();

                        var grandTotal = _claims.Sum(c => c.TotalAmount);

                        stack.Item().Text("Lecturer Subtotals").FontSize(14).Bold().FontColor(Colors.Blue.Medium);

                        stack.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();   // Lecturer
                                columns.ConstantColumn(100); // Total
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Lecturer").Bold();
                                header.Cell().Text("Subtotal (R)").Bold();
                            });

                            foreach (var s in subtotals)
                            {
                                table.Cell().Text(s.Lecturer);
                                table.Cell().Text($"R {s.Total:F2}");
                            }
                        });

                        stack.Item().Text($"Grand Total: R {grandTotal:F2}")
                            .FontSize(14).Bold().FontColor(Colors.Green.Medium);
                    });
                });

                page.Footer().AlignCenter().Text("Payment due within 30 days • Bank: CMCS Finance • Ref: InvoiceID").FontSize(10);
            });
        }

    }
}