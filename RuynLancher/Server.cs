using System.Net.Http;
namespace RuynLancher
{
    public static class Server
    {
        private static RuynServer? _server = null;

        public static RuynServer Get()
        {
            if (_server is null)
            {
                _server = new RuynServer("http://localhost:5202", new HttpClient());
            }
            return _server;
        }
    }
}
