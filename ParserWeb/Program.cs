using HtmlAgilityPack;
using ParserWeb.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParserWeb
{
    class Program
    {
        static void Main(string[] args)
        {

             ParseFromNis();
           Console.ReadKey();
        }

        private static async void ParseFromNis()
        {

            var url = @"http://www.nis.edu.kz/ru/gz/gzitems/";
            var htmlClient = new HttpClient();
            var html = await htmlClient.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);


            var ProductListItems = htmlDocument.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("article_textpreview")).ToList();

            var context = new ParsedWebDBEntities();
            
            foreach (var item in ProductListItems)
            {
                var number = item.Descendants("a").FirstOrDefault().InnerText.Trim('\r','\n','\t');
                var title = item.Descendants("h2").FirstOrDefault().Descendants("a").FirstOrDefault().InnerText.Trim();
                var content = item.Descendants("div")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Equals("text clearfix"))
                    .FirstOrDefault()
                    .Descendants("a").FirstOrDefault().InnerText.Trim('\r', '\n', '\t');
                var urlTender = "http://www.nis.edu.kz/ru/gz/gzitems/" + item.Descendants("a").FirstOrDefault().GetAttributeValue("href", "").Trim('\r', '\n', '\t');
                var datesInfo = item.Descendants("div").Where(node => node.GetAttributeValue("class", "")
                            .Equals("text clearfix"))
                            .FirstOrDefault()
                            .Descendants("div").Where(node => node.GetAttributeValue("class", "")
                            .Equals("dates pull-right"))
                            .FirstOrDefault()
                            .InnerText.Trim('\r', '\n', '\t');

                var dates = Regex.Matches(datesInfo, @"(\d{2})[.](\d{2})[.](\d{4})");

                DateTime? startDate=null;
                DateTime? endDate=null; 
                if (dates.Count == 2)
                {
                    startDate = DateTime.ParseExact(dates[0].Value.ToString(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
                    endDate = DateTime.ParseExact(dates[1].Value.ToString(),  "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
                }

                var tender = new NisEduTender();
                tender.Number = number;
                tender.Title = title;
                tender.Description = content;
                tender.StartDate = startDate;
                tender.EndDate = endDate;
                tender.Url = urlTender;
                Console.WriteLine(number);
                Console.WriteLine(title);
                Console.WriteLine(content);
                Console.WriteLine(startDate.ToString()+" -> "+endDate);
                Console.WriteLine("-------------------------------------------");
                context.NisEduTenders.Add(tender);
            }
            await context.SaveChangesAsync();

            Console.WriteLine(ProductListItems.Count);
        }
    }
}
