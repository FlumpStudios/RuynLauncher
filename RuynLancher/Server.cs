using System.Net.Http;
namespace RuynLancher
{
    public static class Server
    {

        public static string _apiKey = string.Empty;
        private static RuynServer? _server = null;

        public static RuynServer Get()
        {
            if (_server is null)
            {
                var apikey = Security.GetApiKey();
                var h = new HttpClient();
                h.DefaultRequestHeaders.Add("X-Api-Key", apikey);
                _server = new RuynServer("http://plinx-tech.co.uk", h);
            }
            return _server;
        }
    }
}
