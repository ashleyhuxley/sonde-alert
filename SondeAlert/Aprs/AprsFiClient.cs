using ElectricFox.SondeAlert.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Web;

namespace ElectricFox.SondeAlert.Aprs
{
    public sealed class AprsFiClient
    {
        private readonly AprsOptions options;
        private readonly HttpClient _httpClient;

        public AprsFiClient(
            IOptions<AprsOptions> options, 
            HttpClient httpClient)
        {
            this.options = options.Value;
            _httpClient = httpClient;
        }

        public async Task<List<AprsMessage>> GetAprsMessages(string[] callsigns)
        {
            if (callsigns.Length > 10)
            {
                throw new ArgumentException("Maximum of 10 callsigns can be queried at once");
            }

            var uriBuilder = new UriBuilder(options.AprsUrl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["what"] = "msg";
            query["dst"] = string.Join(',', callsigns);
            query["apikey"] = options.AprsApiKey;
            query["format"] = "json";

            uriBuilder.Query = query.ToString();

            HttpResponseMessage response = await _httpClient.GetAsync(uriBuilder.Uri);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            var messageResponse = JsonSerializer.Deserialize<AprsMessageResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return messageResponse?.Messages ?? [];
        }
    }
}
