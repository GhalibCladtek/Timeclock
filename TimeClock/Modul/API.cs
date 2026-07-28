using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TimeClock.Models;

namespace TimeClock.Modul
{

    public class ApiService
    {
        public List<Submission> GetSubmissions()
        {
            string url = "https://dashboard.cladtek.com/api/pep/submissions";

            using (WebClient client = new WebClient())
            {
                try
                {
                    // 1. Download the JSON string synchronously
                    string jsonResponse = client.DownloadString(url);

                    // 2. Convert the JSON into your C# object
                    ApiResponse result = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

                    // 3. Return the data
                    if (result != null && result.success)
                    {
                        return result.data;
                    }

                    return new List<Submission>();
                }
                catch (Exception ex)
                {
                    return new List<Submission>();
                }
            }
        }
    }
}