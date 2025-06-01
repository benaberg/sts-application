using Newtonsoft.Json;
using System.Net.Http;
using STSApplication.model;

namespace STSApplication.core
{
    internal class SaunaTemperatureClient
    {

        private readonly string host;
        private readonly int port;
        private readonly string path;
        private readonly int retryInterval;

        public SaunaTemperatureClient(string host, int port, string path, int retryInterval) {
            this.host = host;
            this.port = port;
            this.path = path;
            this.retryInterval = retryInterval;
        }

        public TemperatureReading FetchReading()
        {
            TemperatureReading? _reading = null;
            HttpClientHandler handler = new()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };
            HttpClient client = new(handler)
            {
                BaseAddress = new Uri("http://" + host + ":" + port)
            };
            using (client)
            {
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

        private TemperatureReading PerformFetch(HttpClient client)
        {
            System.Diagnostics.Debug.WriteLine("Fetching temperature reading...");
            HttpResponseMessage response = client.GetAsync(path).Result;
            response.EnsureSuccessStatusCode();
            String responseString = response.Content.ReadAsStringAsync().Result;
            System.Diagnostics.Debug.WriteLine("Received response: " + responseString);
            TemperatureReading reading = JsonConvert.DeserializeObject<TemperatureReading>(responseString);
            System.Diagnostics.Debug.WriteLine("Deserialized reading: (temperature) " + reading.Temperature + ", (timestamp) " + reading.Timestamp);
            return reading;
        }

        public async void InitUpdate(int seconds, Action<TemperatureReading> labelAction)
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

        private void UpdateLabel(TemperatureReading reading, Action<TemperatureReading> labelAction)
        {
            System.Diagnostics.Debug.WriteLine("Updating label...");
            labelAction.Invoke(reading);
        }
    }
}
