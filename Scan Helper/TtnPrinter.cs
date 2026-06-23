using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Xps;

namespace ScanHelper
{
    public static class TtnPrinter
    {
        public static void Print(
            List<SealItem> items,
            string printerName,
            int copies)
        {
            if (items.Count == 0)
                return;

            LocalPrintServer server = new();

            var printer =
                server.GetPrintQueues()
                .FirstOrDefault(x =>
                    x.Name == printerName);

            if (printer == null)
            {
                MessageBox.Show(
                    "Принтер не знайдено");

                return;
            }

            var articleGroups =
                items.GroupBy(x => x.Article)
                     .OrderBy(x => x.Key);

            foreach (var articleGroup in articleGroups)
            {
                for (int copy = 0; copy < copies; copy++)
                {
                    FixedDocument document = new();

                    PageContent pageContent = new();

                    FixedPage page = new();

                    page.Width = 378;
                    page.Height = 378;

                    double top = 20;

                    string articleText =
                        articleGroup.Key;

                    double articleFont = 50;

                    if (articleText.Length > 10)
                        articleFont = 46;

                    if (articleText.Length > 13)
                        articleFont = 40;

                    if (articleText.Length > 16)
                        articleFont = 34;

                    if (articleText.Length > 20)
                        articleFont = 28;

                    TextBlock article = new();

                    article.Text = articleText;

                    article.FontSize =
                        articleFont;

                    article.FontWeight =
                        FontWeights.Bold;

                    article.Width = 378;

                    article.TextAlignment =
                        TextAlignment.Center;

                    FixedPage.SetLeft(article, 0);
                    FixedPage.SetTop(article, top);

                    page.Children.Add(article);

                    top += 90;

                    foreach (var item in articleGroup
                                 .OrderBy(x => x.Size))
                    {
                        string size = item.Size;

                        if (size.All(char.IsDigit))
                            size += "р.";

                        TextBlock sizeBlock = new();

                        sizeBlock.Text =
                            $"{size} - {item.Qty} шт.";

                        sizeBlock.FontSize = 42;

                        sizeBlock.FontWeight =
                            FontWeights.Bold;

                        FixedPage.SetLeft(
                            sizeBlock,
                            25);

                        FixedPage.SetTop(
                            sizeBlock,
                            top);

                        page.Children.Add(
                            sizeBlock);

                        top += 65;
                    }

                    ((System.Windows.Markup.IAddChild)
                        pageContent)
                        .AddChild(page);

                    document.Pages.Add(
                        pageContent);

                    XpsDocumentWriter writer =
                        PrintQueue
                        .CreateXpsDocumentWriter(
                            printer);

                    writer.Write(
                        document.DocumentPaginator);
                }
            }
        }
    }
}