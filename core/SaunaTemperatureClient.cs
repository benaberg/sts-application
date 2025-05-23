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
            HttpClientHandler Handler = new()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };
            HttpClient Client = new(Handler)
            {
                BaseAddress = new Uri("http://" + ApplicationResource.STS_API_Address + ":" + ApplicationResource.STS_API_Port)
            };
            using (Client)
            {
                int RetryInterval = int.Parse(ApplicationResource.STS_HTTP_RetryInterval);
                do
                {
                    try
                    {
                        _reading = PerformFetch(Client);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Request failed. Reason: " + e.Message);
                        System.Diagnostics.Debug.WriteLine("Retrying in " + RetryInterval + " seconds...");
                    }
                }
                while (_reading == null);
            }
            return _reading;
        }

        private static TemperatureReading PerformFetch(HttpClient Client)
        {
            System.Diagnostics.Debug.WriteLine("Fetching temperature reading...");
            HttpResponseMessage Response = Client.GetAsync(ApplicationResource.STS_API_Context).Result;
            Response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<TemperatureReading>(Response.Content.ReadAsStringAsync().Result);
        }

        public static async void InitUpdate(int Seconds, Action<LabelContent> LabelAction)
        {
            System.Diagnostics.Debug.WriteLine("Initializing update...");
            TemperatureReading _reading = FetchReading();
            if (Seconds <= 0 || _reading == null)
            {
                return;
            }

            // Inital update of label
            UpdateLabel(_reading, LabelAction);

            var PeriodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(Seconds));
            while (await PeriodicTimer.WaitForNextTickAsync())
            {
                _reading = FetchReading();
                if (_reading != null)
                {
                    UpdateLabel(_reading, LabelAction);
                }
            }
        }

        private static void UpdateLabel(TemperatureReading Reading, Action<LabelContent> LabelAction)
        {
            System.Diagnostics.Debug.WriteLine("Updating label...");
            LabelContent Content = new(Reading.Temperature);
            LabelAction.Invoke(Content);
        }
    }
}
