using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    public class ChartController : Controller
    {
        // GET: api/values
        [HttpGet("{consumptionFactor}")]
        public async Task<object> Get(int consumptionFactor, string type)
        {
            var isLong = type == "long";

            var start = new DateTime(2016, 4, 9);
            var end = new DateTime(2016, 4, 11);

            if (isLong)
            {
                start = new DateTime(2016, 4, 1);
                end = new DateTime(2016, 5, 1);
            }

            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, StringTable>
                    {
                        {
                            "input1",
                            new StringTable
                            {
                                ColumnNames = new[] {"Col1", "Col2", "Col3", "Col4", "Col5"},
                                Values = MachineLearningParameters(start, end, consumptionFactor, isLong)
                            }
                        }
                    },
                    GlobalParameters = new Dictionary<string, string>()
                };
                const string apiKey =
                    "stoIJ/IUqHrzFHwWyfU7X/1U0W9Fp+TfNxWVc96WxPOWhNz5d3T9gdviUcc/G2jMT6pnwKU0++cXzfqEyUowoA==";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                client.BaseAddress =
                    new Uri(
                        "https://europewest.services.azureml.net/workspaces/1b401c9f2fc742e793848b0586bcc18c/services/6775c4d0e24f4c6ba5415c32f27883e2/execute?api-version=2.0&details=true");

                // WARNING: The 'await' statement below can result in a deadlock if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false) so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)

                var response = await client.PostAsJsonAsync("", scoreRequest).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<RootObject>();
                    Console.WriteLine("Result: {0}", result);

                    return new
                    {
                        cols =
                            new object[]
                            {
                                new {label = "Time", type = "string"},
                                new {label = "Model", type = "number"},
                                new {label = "Simulated", type = "number"},
                                new {label = "Machine learning", type = "number"}
                            },
                        rows = GraphData(start, end, result, consumptionFactor, isLong)
                    };
                }
                Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                // Print the headers - they include the requert ID and the timestamp, which are useful for debugging the failure
                Console.WriteLine(response.Headers.ToString());

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);

                return null;
            }
        }

        private object[] GraphData(DateTime start, DateTime end, RootObject result, double consumptionFactor, bool isLong)
        {
            var simulator = new MeterSimulator();
            var resultRows = result.Results.output1.value.Values;
            var graphRows = new List<object>();
            var i = 0;

            for (var dt = start; dt < end; dt = dt.AddHours(1), i++)
                graphRows.Add(new
                {
                    c = new object[]
                    {
                        new { v = dt.ToString("g") },
                        new { v = simulator.SimulatedConsumption(dt, consumptionFactor, true) },
                        new { v = simulator.SimulatedConsumption(dt, consumptionFactor, false) },
                        new { v = resultRows[i][5] }
                    }
                });

            return graphRows.ToArray();
        }

        private string[][] MachineLearningParameters(DateTime start, DateTime end, int consumptionFactor, bool isLong)
        {
            var parameters = new List<string[]>();

            for (var dt = start; dt < end; dt = dt.AddHours(1))
            {
                parameters.Add(new[]
                {
                    dt.ToString("O"),
                    dt.DayOfYear.ToString(),
                    dt.TimeOfDay.TotalSeconds.ToString(),
                    consumptionFactor.ToString(),
                    "0"
                });
            }

            return parameters.ToArray();
        }
    }
}

public class StringTable
{
    public string[] ColumnNames { get; set; }
    public string[][] Values { get; set; }
}

public class Value
{
    public List<string> ColumnNames { get; set; }
    public List<string> ColumnTypes { get; set; }
    public List<List<string>> Values { get; set; }
}

public class Output1
{
    public string type { get; set; }
    public Value value { get; set; }
}

public class Results
{
    public Output1 output1 { get; set; }
}

public class RootObject
{
    public Results Results { get; set; }
}