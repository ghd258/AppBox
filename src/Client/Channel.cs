using System;
using System.Threading.Tasks;

namespace AppBoxClient
{
    public static class Channel
    {
        public static Task<object> Login(string user, string password, object? external = null)
        {
            throw new NotImplementedException();
        }

        public static Task<bool> Logout()
        {
            throw new NotImplementedException();
        }

        public static Task<object> Invoke(string service, object[] args)
        {
            throw new NotImplementedException();
        }
    }
}