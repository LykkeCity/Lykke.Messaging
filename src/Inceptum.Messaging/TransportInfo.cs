using System;

namespace Inceptum.Messaging
{
    public class TransportInfo
    {
        public TransportInfo(string broker, string login, string password)
        {
            if (string.IsNullOrEmpty((broker ?? "").Trim())) throw new ArgumentException("broker should be not empty string", "broker");
            if (string.IsNullOrEmpty((login ?? "").Trim())) throw new ArgumentException("login should be not empty string", "login");
            if (string.IsNullOrEmpty((password ?? "").Trim())) throw new ArgumentException("password should be not empty string", "password");
            Broker = broker;
            Login = login;
            Password = password;
        }

        public string Broker { get; private set; }
        public string Login { get; private set; }
        public string Password { get; private set; }

        public bool Equals(TransportInfo other)
        {
            return Equals(other.Broker, Broker) && Equals(other.Login, Login) && Equals(other.Password, Password);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (TransportInfo)) return false;
            return Equals((TransportInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = (Broker != null ? Broker.GetHashCode() : 0);
                result = (result*397) ^ (Login != null ? Login.GetHashCode() : 0);
                result = (result*397) ^ (Password != null ? Password.GetHashCode() : 0);
                return result;
            }
        }
    }
}