using BankCoreApi.Controllers.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BankCoreApi.Helpers;

public static class PdfHelper
{
    public static byte[] EkstreOlustur(HesapOzetResponse ozet, string donem)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Column(col =>
                {
                    col.Item().Text("BankaCuzdan").FontSize(22).Bold().FontColor(Colors.Teal.Darken2);
                    col.Item().Text("Hesap Ekstresi").FontSize(14).SemiBold();
                    col.Item().PaddingTop(4).Text($"Dönem: {donem}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Teal.Medium);
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Item().Background(Colors.Grey.Lighten3).Padding(12).Column(info =>
                    {
                        info.Item().Text($"Hesap Sahibi: {ozet.HesapSahibiAd}").SemiBold();
                        info.Item().Text($"Hesap No: {ozet.HesapNo}");
                        info.Item().Text($"Bakiye: {ozet.Bakiye:N2} TL");
                    });

                    col.Item().PaddingTop(16).Text("Hesap Hareketleri").FontSize(12).SemiBold();

                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Teal.Darken2).Padding(6)
                                .Text("Tarih").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(Colors.Teal.Darken2).Padding(6)
                                .Text("Açıklama").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(Colors.Teal.Darken2).Padding(6)
                                .Text("Karşı Hesap").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(Colors.Teal.Darken2).Padding(6)
                                .AlignRight().Text("Tutar").FontColor(Colors.White).SemiBold();
                        });

                        foreach (var islem in ozet.SonIslemler)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(islem.Tarih.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(islem.Aciklama);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(islem.KarsiHesapAdSoyad ?? "-");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .AlignRight().Text($"{islem.Miktar:N2} TL");
                        }
                    });

                    if (ozet.SonIslemler.Count == 0)
                    {
                        col.Item().PaddingTop(12).Text("Bu dönemde işlem bulunamadı.").Italic();
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("BankaCuzdan — Güvenli Bankacılık  |  Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public static byte[] DekontOlustur(DekontResponse dekont)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));

                page.Header().Column(col =>
                {
                    col.Item().Text("BankaCuzdan").FontSize(22).Bold().FontColor(Colors.Teal.Darken2);
                    col.Item().Text("Transfer Dekontu").FontSize(14).SemiBold();
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Teal.Medium);
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().Background(Colors.Grey.Lighten3).Padding(16).Column(box =>
                    {
                        box.Item().Text("İşlem Bilgileri").FontSize(12).SemiBold().FontColor(Colors.Teal.Darken2);
                        box.Item().PaddingTop(10).Row(r =>
                        {
                            r.RelativeItem().Text("Referans No:");
                            r.RelativeItem().AlignRight().Text(dekont.IslemGrupId.ToString()).SemiBold();
                        });
                        box.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("Tarih:");
                            r.RelativeItem().AlignRight()
                                .Text(dekont.Tarih.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));
                        });
                        box.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("Tutar:");
                            r.RelativeItem().AlignRight().Text($"{dekont.Tutar:N2} TL").Bold().FontSize(13);
                        });
                        box.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("Transfer Ücreti ve BSMV:");
                            r.RelativeItem().AlignRight().Text($"{dekont.BsmvKesintisi:N2} TL");
                        });
                    });

                    col.Item().PaddingTop(20).Text("Gönderen").SemiBold().FontColor(Colors.Teal.Darken2);
                    col.Item().PaddingTop(4).Text(dekont.GonderenAd);
                    col.Item().Text($"Hesap No: {dekont.GonderenHesapNo}").FontSize(10);

                    col.Item().PaddingTop(16).Text("Alıcı").SemiBold().FontColor(Colors.Teal.Darken2);
                    col.Item().PaddingTop(4).Text(dekont.AliciAd);
                    col.Item().Text($"Hesap No: {dekont.AliciHesapNo}").FontSize(10);

                    col.Item().PaddingTop(24).Border(1).BorderColor(Colors.Teal.Lighten2).Padding(12)
                        .Text("Bu belge BankaCuzdan sistemi tarafından elektronik olarak üretilmiştir.")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().Text("BankaCuzdan — Güvenli Bankacılık")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
            });
        });

        return document.GeneratePdf();
    }
}
