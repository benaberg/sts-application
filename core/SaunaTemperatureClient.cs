using Newtonsoft.Json;
using System.Net.Http;
using STSApplication.model;

namespace STSApplication.core
{
    internal static class SaunaTemperatureClient
    {
        public static TemperatureReading FetchReading()
        {
            TemperatureReading? _reading = null;
            HttpClientHandler handler = new()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };
            HttpClient client = new(handler)
            {
                BaseAddress = new Uri("http://" + ApplicationResource.STS_API_Address + ":" + ApplicationResource.STS_API_Port)
            };
            using (client)
            {
                int retryInterval = int.Parse(ApplicationResource.STS_HTTP_RetryInterval);
                do
                {
                    try
                    {
                        _reading = PerformFetch(client);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Request failed. Reason: " + e.Message);
                        System.Diagnostics.Debug.WriteLine("Retrying in " + retryInterval + " seconds...");
                    }
                }
                while (_reading == null);
            }
            return _reading;
        }

        private static TemperatureReading PerformFetch(HttpClient client)
        {
            System.Diagnostics.Debug.WriteLine("Fetching temperature reading...");
            HttpResponseMessage response = client.GetAsync(ApplicationResource.STS_API_Context).Result;
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<TemperatureReading>(response.Content.ReadAsStringAsync().Result);
        }

        public static async void InitUpdate(int seconds, Action<LabelContent> labelAction)
        {
            System.Diagnostics.Debug.WriteLine("Initializing update...");
            TemperatureReading _reading = FetchReading();
            if (seconds <= 0 || _reading == null)
            {
                return;
            }

            // Inital update of label
            UpdateLabel(_reading, labelAction);

            var PeriodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
            while (await PeriodicTimer.WaitForNextTickAsync())
            {
                _reading = FetchReading();
                if (_reading != null)
                {
                    UpdateLabel(_reading, labelAction);
                }
            }
        }

        private static void UpdateLabel(TemperatureReading reading, Action<LabelContent> labelAction)
        {
            System.Diagnostics.Debug.WriteLine("Updating label...");
            LabelContent Content = new(reading.Temperature);
            labelAction.Invoke(Content);
        }
    }
}
