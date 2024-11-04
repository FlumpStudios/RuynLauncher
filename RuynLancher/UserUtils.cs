using System.Security.Principal;

namespace RuynLancher
{
    public static class UserUtils
    {
        private static string _clientId = string.Empty;
        public static string GetUserId()
        {
            if (!string.IsNullOrEmpty(_clientId))
            {
                return _clientId;
            }

            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            var id = identity?.User?.Value ?? string.Empty;

            if (!string.IsNullOrEmpty(id))
            {
                _clientId = Crypto.GetSha256Hash(id);
                return _clientId;
            }

            return string.Empty;
        }
    }
}
