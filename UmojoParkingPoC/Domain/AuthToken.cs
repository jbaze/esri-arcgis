using System;

namespace UmojoParkingPoC.Domain
{
    public class AuthToken
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; }
    }
}
