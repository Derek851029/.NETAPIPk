using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using PKApp.ConfigOptions;
using System.Collections.Specialized;
using System.Net;
using System.Text.Json;

namespace PKApp.Services
{
    public interface IFirebaseService
    {
        Task<string> GenerateDynamicLink(NameValueCollection dataDynamicLink, string medium, bool suffixOption);
        Task<string> GetDynamicReport();
    }

    public class FirebaseService : IFirebaseService
    {
        private readonly IConfiguration _configuration;
        private readonly FirebaseOptions _options;
        private readonly GoogleCredential _credential;

        public FirebaseService(IConfiguration configuration, IOptions<FirebaseOptions> options)
        {
            _configuration = configuration;
            _options = options.Value;
            _credential = GoogleCredential.FromFile(_options.FirebaseAuthFile);
        }

        public async Task<string> GenerateDynamicLink(NameValueCollection dataDynamicLink, string medium, bool suffixOption)
        {
            string APIKey = _configuration["FirebaseAPIKey"];
            string firebaseDynamicLinksApiUrl = $"https://firebasedynamiclinks.googleapis.com/v1/shortLinks?key={APIKey}";
            string dynamicDomain = "https://app.pkcard.com.tw/link";
            string webLink = "https://www.pkcard.com.tw/downloadapp";

            var androidInfo = new
            {
                androidPackageName = "tw.com.pkcard",
                androidFallbackLink = "https://play.google.com/store/apps/details?id=tw.com.pkcard&hl=zh_TW",
            };

            var iosInfo = new
            {
                iosBundleId = "tw.com.pkcard",
                iosAppStoreId = "1277406216",
            };

            var analyticsInfo = new
            {
                googlePlayAnalytics = new
                {
                    utmSource = "DL",
                    utmMedium = medium,
                }
            };
            var dynamicLinkRequest = new
            {
                dynamicLinkInfo = new
                {
                    domainUriPrefix = dynamicDomain,
                    link = $"{webLink}{BuildQueryString(dataDynamicLink)}",
                    androidInfo = androidInfo,
                    iosInfo = iosInfo,
                    analyticsInfo = analyticsInfo,

                },
                suffix = new
                {
                    option = suffixOption ? "SHORT" : "UNGUESSABLE"
                }
            };

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsJsonAsync(firebaseDynamicLinksApiUrl, dynamicLinkRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(result))
                    {
                        JsonElement root = doc.RootElement;

                        string shortLink = root.GetProperty("shortLink").GetString();

                        Console.WriteLine("Generated Short Link: " + shortLink);
                        return shortLink;
                    }
                }
                else
                {
                    Console.WriteLine("Failed to generate short link. StatusCode: " + response.StatusCode);
                    return "";
                }
            }


        }

        public async Task<string> GetDynamicReport()
        {
            string accessToken = await GetGoogleAPIAccessToken();
            string encodedUrl = WebUtility.UrlEncode("https://app.pkcard.com.tw/link/JgLh");
            string firebaseApiUrl = $"https://firebasedynamiclinks.googleapis.com/v1/{encodedUrl}/linkStats?durationDays=365";
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await httpClient.GetAsync(firebaseApiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(result))
                    {
                        JsonElement root = doc.RootElement;

                        string shortLink = root.GetProperty("linkEventStats").GetString();

                        Console.WriteLine("Generated Short Link: " + shortLink);
                        return shortLink;
                    }
                }
                else
                {
                    throw new Exception("");
                }
            }
            return "";
        }

        public async Task<string> GetGoogleAPIAccessToken()
        {
            List<string> scopes = new List<string>
            {
                "https://www.googleapis.com/auth/firebase"
            };
            GoogleCredential scopedCredential = _credential.CreateScoped(scopes);

            string token = scopedCredential.UnderlyingCredential.GetAccessTokenForRequestAsync().Result;
            return token;
        }

        public string BuildQueryString(NameValueCollection parameters)
        {
            return "?" + string.Join("&", parameters.AllKeys
            .Select(key => $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(parameters[key])}"));
        }
    }
}
