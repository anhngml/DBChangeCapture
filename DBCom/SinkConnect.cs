using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBCom
{
    internal class SinkConnect
    {
        private string endpoint;
        private static SinkConnect instance;
        public static SinkConnect GetInstance(string endpoint)
        {
            if (instance == null) {
                instance = new SinkConnect(endpoint);
            }
            else
            {
                instance.endpoint = endpoint;
            }
            return instance;
        }
        private SinkConnect(string endpoint) {
            this.endpoint = endpoint;
        }

        public async Task<string> Push(string jsonData)
        {
            // Tạo một đối tượng HttpClient
            using (HttpClient client = new HttpClient())
            {
                HttpContent content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
                else
                {
                    return response.StatusCode.ToString();
                }
            }
        }
    }
}
