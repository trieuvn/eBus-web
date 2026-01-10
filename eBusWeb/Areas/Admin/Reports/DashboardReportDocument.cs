using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using eBusWeb.Models;
using System.IO;

namespace eBusWeb.Areas.Admin.Reports
{
    public class DashboardReportDocument : IDocument
    {
        private readonly List<Booking> _bookings;
        private readonly List<Models.Route> _routes;

        public DashboardReportDocument(
            List<Booking> bookings,
            List<Models.Route> routes)
        {
            _bookings = bookings;
            _routes = routes;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            var totalRevenue = _bookings.Sum(x => x.TotalAmount);
            var totalBookings = _bookings.Count;
            var activeRoutes = _routes.Count;
            var pendingAmount = _bookings
                .Where(x => x.BookingStatus == 0)
                .Sum(x => x.TotalAmount);

            var logoPath = Path.Combine("wwwroot", "images", "bus.png");

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                // ================= HEADER =================
                page.Header()
                    .Background(Colors.Blue.Lighten5)
                    .Padding(15)
                    .Row(row =>
                    {
                        row.Spacing(15);

                        if (File.Exists(logoPath))
                        {
                            row.ConstantItem(55)
                                .Image(logoPath)
                                .FitArea();
                        }

                        row.RelativeItem().Column(col =>
                        {
                            col.Spacing(2);

                            col.Item().Text("eBus Admin Dashboard Report")
                                .FontSize(20)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);

                            col.Item().Text("System performance & financial overview")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);

                            col.Item().Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken2);
                        });
                    });

                // ================= CONTENT =================
                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(20);

                    // ---- KPI TITLE ----
                    col.Item().Text("Key Performance Indicators")
                        .FontSize(15)
                        .Bold();

                    // ---- KPI CARDS ----
                    col.Item().Row(row =>
                    {
                        row.Spacing(10);

                        KPI(row.RelativeItem(), "Total Revenue", totalRevenue.ToString("C"), Colors.Green.Lighten4);
                        KPI(row.RelativeItem(), "Total Bookings", totalBookings.ToString(), Colors.Blue.Lighten4);
                        KPI(row.RelativeItem(), "Active Routes", activeRoutes.ToString(), Colors.Orange.Lighten4);
                    });

                    // ---- DIVIDER ----
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // ---- PENDING ----
                    col.Item().Text("Pending Payments")
                        .FontSize(14)
                        .Bold();

                    col.Item().Text(pendingAmount.ToString("C"))
                        .FontSize(12)
                        .FontColor(Colors.Red.Darken2);

                    // ---- DIVIDER ----
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // ---- TABLE TITLE ----
                    col.Item().Text("Recent Bookings")
                        .FontSize(14)
                        .Bold();

                    // ================= TABLE =================
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // ID
                            columns.RelativeColumn(3); // Customer
                            columns.RelativeColumn(3); // Amount
                            columns.RelativeColumn(2); // Status
                        });

                        // HEADER
                        table.Header(header =>
                        {
                            header.Cell().HeaderStyle().Text("Booking ID");
                            header.Cell().HeaderStyle().Text("Customer");
                            header.Cell().HeaderStyle().Text("Amount");
                            header.Cell().HeaderStyle().Text("Status");
                        });

                        // ROWS
                        foreach (var b in _bookings
                            .OrderByDescending(x => x.CreatedAt)
                            .Take(5))
                        {
                            table.Cell().BodyStyle().Text($"#{b.Id}");
                            table.Cell().BodyStyle().Text(b.ContactName);
                            table.Cell().BodyStyle().Text(b.TotalAmount.ToString("C"));
                            table.Cell().BodyStyle().Text(
                                b.BookingStatus == 1 ? "Confirmed" :
                                b.BookingStatus == 0 ? "Pending" : "Cancelled");
                        }
                    });
                });

                // ================= FOOTER =================
                page.Footer().PaddingTop(20).Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text("© eBus System")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);

                        row.RelativeItem().AlignRight().Column(sign =>
                        {
                            sign.Spacing(2);

                            sign.Item().Text("Approved by")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);

                            sign.Item().Text("eBus Administration")
                                .Italic()
                                .FontSize(12);

                            sign.Item()
                                .Width(120)
                                .Container()
                                .LineHorizontal(1)
                                .LineColor(Colors.Grey.Darken1);

                            sign.Item().Text(DateTime.Now.Year.ToString())
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken2);
                        });
                    });
                });
            });
        }

        // ================= KPI CARD =================
        private void KPI(IContainer container, string title, string value, string bgColor)
        {
            container
                .Background(bgColor)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(12)
                .Column(col =>
                {
                    col.Item().Text(title)
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken2);

                    col.Item().Text(value)
                        .FontSize(16)
                        .Bold();
                });
        }
    }

    // ================= TABLE STYLES =================
    static class TableStyles
    {
        public static IContainer HeaderStyle(this IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten4)
                .Padding(6)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .DefaultTextStyle(x => x.Bold().FontSize(10));
        }

        public static IContainer BodyStyle(this IContainer container)
        {
            return container
                .Padding(6)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten3)
                .DefaultTextStyle(x => x.FontSize(10));
        }
    }
}
