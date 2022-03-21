using System;
using System.Collections.Generic;
using AngleSharp;
using AngleSharp.Dom;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Text;

namespace ParserTestTask
{
    class Reports
    {
        [JsonProperty("sellerName")] public string SellerName { get; set; }
        [JsonProperty("sellerInn")] public string SellerInn { get; set; }
        [JsonProperty("buyerName")] public string BuyerName { get; set; }
        [JsonProperty("buyerInn")] public string BuyerInn { get; set; }
        [JsonProperty("woodVolumeBuyer")] public decimal WoodVolumeBuyer { get; set; }
        [JsonProperty("woodVolumeSeller")] public decimal WoodVolumeSeller { get; set; }
        [JsonProperty("dealDate")]
        //[JsonConverter(typeof(UnixTimeToDatetimeConverter))] 
        public DateTime? DealDate { get; set; }
        [JsonProperty("dealNumber")]
        public string DealNumber { get; set; }
        [JsonProperty("__typename")] public string TypeName { get; set; }

    }
  

    class Program
    {
        private static readonly HttpClient client = new HttpClient();


        static async Task Main(string[] args)
        {
            await Parser();
    
        }

        static async Task ReplaceToBD(Reports report)
        {
            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ParserTask;Integrated Security=True;";
           
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                //foreach (Reports report in reports)
               // {
                    try
                    {
                        string sqlExpression = "" +// $"if not exists (select * from WoodTrans where WoodTrans.dealNumber = @dealNumber AND WoodTrans.sellerName = @sellerName AND WoodTrans.sellerInn = @sellerInn " +
                                                    //$" AND WoodTrans.buyerName = @buyerName AND WoodTrans.buyerInn = @buyerInn AND WoodTrans.woodVolumeBuyer=@woodVolumeBuyer" +
                                                     //$" AND WoodTrans.woodVolumeSeller=@woodVolumeSeller AND WoodTrans.dealDate = @dealDate)" +
                    $" INSERT INTO WoodTrans (dealNumber, sellerName, sellerInn, buyerName, buyerInn, woodVolumeBuyer, woodVolumeSeller, dealDate, _typeName) VALUES (@dealNumber, @sellerName, @sellerInn, @buyerName, @buyerInn, @woodVolumeBuyer, @woodVolumeSeller, @dealDate, @_typeName);";

                        SqlCommand command = new SqlCommand(sqlExpression, connection);

                        command.Parameters.AddWithValue("@dealNumber", IsNull(report.DealNumber));
                        command.Parameters.AddWithValue("@sellerName", IsNull(report.SellerName));
                        command.Parameters.AddWithValue("@sellerInn", IsNull(report.SellerInn));
                        command.Parameters.AddWithValue("@buyerName", IsNull(report.BuyerName));
                        command.Parameters.AddWithValue("@buyerInn", IsNull(report.BuyerInn));
                        command.Parameters.AddWithValue("@woodVolumeBuyer", IsNull(report.WoodVolumeBuyer));
                        command.Parameters.AddWithValue("@woodVolumeSeller", IsNull(report.WoodVolumeSeller));
                        command.Parameters.AddWithValue("@dealDate", IsNull(report.DealDate));
                        command.Parameters.AddWithValue("@_typeName", IsNull(report.TypeName));
                   


                        command.ExecuteNonQuery();
                    }
                    // string sqlExpression = "Insert into wood (dealNumber) values ('sdfdssf')";

                    catch { }

               // }
            }
        }

        public static object IsNull(object obj)
        {
            return obj is null ? DBNull.Value : obj;
        }

        static async Task Parser()
        {
            int countInArr = 0;
            int pageNumber = 0;
            List<Reports> listReports = new List<Reports>();


            do
            {
                var handler = new HttpClientHandler();

                handler.AutomaticDecompression = ~DecompressionMethods.None;

                using (var httpClient = new HttpClient(handler))
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://www.lesegais.ru/open-area/graphql"))
                    {
                        request.Headers.Add("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"96\", \"Yandex\";v=\"22\"");
                        request.Headers.Add("Accept", "*/*");
                        request.Headers.Add("sec-ch-ua-mobile", "?0");
                        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                        request.Headers.Add("Origin", "https://www.lesegais.ru");
                        request.Headers.Add("Sec-Fetch-Site", "same-origin");
                        request.Headers.Add("Sec-Fetch-Mode", "cors");
                        request.Headers.Add("Sec-Fetch-Dest", "empty");
                        request.Headers.Add("Referer", "https://www.lesegais.ru/open-area/deal");
                        request.Headers.Add("Accept-Language", "ru,en;q=0.9");

                        var stringContent = "{\"query\":\"query SearchReportWoodDeal($size: Int!, $number: Int!, $filter: Filter, $orders: [Order!]) {\\n  searchReportWoodDeal(filter: $filter, pageable: {number: $number, size: $size}, orders: $orders) {\\n    content {\\n      sellerName\\n      sellerInn\\n      buyerName\\n      buyerInn\\n      woodVolumeBuyer\\n      woodVolumeSeller\\n      dealDate\\n      dealNumber\\n      __typename\\n    }\\n    __typename\\n  }\\n}\\n\",\"variables\":{\"size\":50000,\"number\":@pageNumber,\"filter\":null,\"orders\":[{\"property\":\"dealDate\",\"direction\":\"DESC\"}]},\"operationName\":\"SearchReportWoodDeal\"}";
                        stringContent = stringContent.Replace("@pageNumber", pageNumber.ToString());
                        request.Content = new StringContent(stringContent);
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                        var response = await httpClient.SendAsync(request);

                        var responseString = await response.Content.ReadAsStringAsync();

                        responseString = responseString.Replace("{\"data\":{\"searchReportWoodDeal\":{\"content\":[", "");
                        responseString = responseString.Replace("],\"__typename\":\"PageReportWoodDeal\"}}}", "");

                        Regex reg = new Regex("},{");
                        string[] arr = reg.Split(responseString);


                        foreach (string s in arr)
                        {
                            string? st = s;
                            if (s[0] != '{')
                            {
                                st = s.Insert(0, "{");
                            }
                            if (s[s.Length - 1] != '}') st = st + "}";

                            Reports? restoredPerson = JsonConvert.DeserializeObject<Reports>(st);
                            listReports.Add(restoredPerson);
                            ReplaceToBD(restoredPerson);
                        }
                        countInArr = arr.Count();
                        pageNumber++;
                        Console.WriteLine(pageNumber + "   "+arr.Count());
                    }
                        

                }


            } while (countInArr == 50000);
            //ReplaceToBD(listReports);
        }
            
    }


}



  

