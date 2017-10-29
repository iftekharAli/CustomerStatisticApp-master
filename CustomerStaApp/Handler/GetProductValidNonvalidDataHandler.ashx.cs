using DAL;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace CustomerStaApp.Handler
{
    /// <summary>
    /// Summary description for GetProductValidNonvalidDataHandler
    /// </summary>
    public class GetProductValidNonvalidDataHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            string Recency = context.Request.QueryString["Recency"];
            string Frequency = context.Request.QueryString["Frequency"];
            string Monetary = context.Request.QueryString["Monetary"];
            string DataType = context.Request.QueryString["DataType"];
            string Rating = context.Request.QueryString["Rating"];


            string json = new StreamReader(context.Request.InputStream).ReadToEnd();
            json = json.Replace("[", string.Empty).Replace("]", string.Empty).Replace("\"", string.Empty);
            json = HttpUtility.HtmlDecode(json);
            Db db = new Db();

            List<SqlParameter> list = new List<SqlParameter>
            {
                new SqlParameter
                {
                    ParameterName = "@Recency",
                    Value = Recency
                },
                new SqlParameter
                {
                    ParameterName = "@Frequency",
                    Value = Frequency
                },
                new SqlParameter
                {
                    ParameterName = "@Monetary",
                    Value = Monetary
                }
                ,new SqlParameter
                {
                    ParameterName="@Title",
                    Value = json.Replace("[",string.Empty).Replace("]",string.Empty).Replace("\"",string.Empty)
                },
                new SqlParameter
                {
                    ParameterName="@DataType",
                    Value = DataType
                }, new SqlParameter
                {
                    ParameterName="@Rating",
                    Value = Rating
                }

            };
            DataSet dataSet = null;


            dataSet = db.GetDataSet("spGetProductValidNonvalidData", list);


            string valid = string.Empty;
            string nonvalid = string.Empty;
            string prior = string.Empty;

            valid = DataTableToJsonWithJsonNet(dataSet.Tables[0]); ;
            nonvalid = DataTableToJsonWithJsonNet(dataSet.Tables[1]); ;
            prior = DataTableToJsonWithJsonNet(dataSet.Tables[2]);
            var productListMain = dataSet.Tables[2].AsEnumerable().Select(dataRow => new Product { Name = dataRow.Field<string>("Product Name") }).ToList();
            Dictionary<string, int> dicFrequentpatternOne = new Dictionary<string, int>();

            foreach (DataRow dr in dataSet.Tables[2].Rows)
            {

                string productName = dr["Product Name"].ToString();
                string[] vv = productName.Split(',');
                foreach (var productList in vv)
                {
                    if (dicFrequentpatternOne.ContainsKey(productList))
                    {
                        int value2 = (int)dicFrequentpatternOne[productList];
                        dicFrequentpatternOne[productList] = ++value2;
                    }
                    else
                    {
                        dicFrequentpatternOne.Add(productList, 1);

                    }

                }
            }



            Dictionary<string, int> dicFrequentpattern = new Dictionary<string, int>();
            Dictionary<string, int> copydicFrequentpatternOne = new Dictionary<string, int>(dicFrequentpatternOne);
            Dictionary<string, int> afterMinimum = new Dictionary<string, int>();

            var priorData = PriorLogic(dicFrequentpatternOne, dicFrequentpattern, productListMain, afterMinimum);
          
           List<string> flist = new List<string>();
            int count = priorData.Count();
            foreach (var priorDataKey in priorData.Keys)
            {
                var rawToScan = string.Join(",", priorDataKey.Split(',').Distinct());
                var a = productListMain.Where(o => o.Name.ToString().Contains(rawToScan))
                    .ToList();
                int globalValueToDivide = a.Count;
                var segmentWise= priorDataKey.Split(',').Distinct();
                string[] aa = priorDataKey.Split(',').Distinct().ToArray();
                int renewLength =aa.Length;
                for (int i = 0; i < renewLength; i++)
                {
                    for (int j = i+1; j < renewLength; j++)
                    {
                        
                    }
                }

            }
     
           

            string resjson = "{\"valid\":" + valid + ",\"nonvalid\":" + nonvalid + ",\"prior\":" + prior + "}";

            context.Response.Write(resjson);

        }

        private static Dictionary<string, int> PriorLogic(Dictionary<string, int> dicFrequentpatternOne, Dictionary<string, int> dicFrequentpattern, List<Product> empList,
            Dictionary<string, int> afterMinimum)
        {
            int total = 0;
            DictionaryKeyValueAdd(dicFrequentpatternOne, dicFrequentpattern, empList, total);
            DictionaryKeyValueAdd(dicFrequentpatternOne, dicFrequentpattern, empList, total);
            Dictionary<string, int> copyFrequentPattern = new Dictionary<string, int>(dicFrequentpattern);

            foreach (var rawVal in dicFrequentpattern)
            {
                if (rawVal.Value >= 1)
                {
                    afterMinimum.Add(rawVal.Key, rawVal.Value);
                }
            }
            Dictionary<string, int> copyMinimumPattern = new Dictionary<string, int>(afterMinimum);
            if (afterMinimum.Count > 0)
            {
                afterMinimum.Clear();
                dicFrequentpattern.Clear();
                return PriorLogic(copyMinimumPattern, dicFrequentpattern, empList, afterMinimum);

            }
            else if (afterMinimum.Count == 0)
            {
                Dictionary<string, int> final = new Dictionary<string, int>(dicFrequentpatternOne);
                return final;

            }

            Dictionary<string, int> final1 = new Dictionary<string, int>(dicFrequentpatternOne);
            return final1;
        }

        private static int DictionaryKeyValueAdd(Dictionary<string, int> dicFrequentpatternOne, Dictionary<string, int> hashFrequentpatternTwo, List<Product> proList, int total)
        {
            for (int i = 0; i < dicFrequentpatternOne.Count; i++)
            {

                for (int j = i + 1; j < dicFrequentpatternOne.Count; j++)
                {
                    if (hashFrequentpatternTwo.ContainsKey(dicFrequentpatternOne.Keys.ElementAt(i) + "," +
                                                           dicFrequentpatternOne.Keys.ElementAt(j)))
                    {
                        var a = proList.Where(o => o.Name.ToString().Contains(dicFrequentpatternOne.Keys.ElementAt(i)))
                            .ToList()
                            .Where(o => o.Name.ToString()
                                .Contains(dicFrequentpatternOne.Keys.ElementAt(j))).ToList();
                        if (a.Count > 0)
                        {
                            total = total + 1;
                        }
                        if (total > 0)
                        {
                            int value2 = (int) hashFrequentpatternTwo[dicFrequentpatternOne.Keys.ElementAt(i) + "," +
                                                                      dicFrequentpatternOne.Keys.ElementAt(j)];
                            if (value2 != 0)
                            {
                                value2 += a.Count;
                            }
                            else
                            {
                                value2 = a.Count;
                            }
                            hashFrequentpatternTwo[dicFrequentpatternOne.Keys.ElementAt(i) + "," +
                                                   dicFrequentpatternOne.Keys.ElementAt(j)] = value2;
                        }
                    }
                    else
                    {
                        hashFrequentpatternTwo.Add(
                            dicFrequentpatternOne.Keys.ElementAt(i) + "," + dicFrequentpatternOne.Keys.ElementAt(j), 0);
                    }

                   

                }
            }
            return total;
        }
        public string DataTableToJsonWithJsonNet(DataTable table)
        {
            var jsonString = JsonConvert.SerializeObject(table);
            return jsonString;
        }
        public class Product
        {
            public string Name { get; set; }
        }
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}