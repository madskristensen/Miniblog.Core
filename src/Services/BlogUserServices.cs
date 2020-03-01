namespace Miniblog.Core.Services
{
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;
    using Microsoft.Extensions.Configuration;

    using System;
    using System.Text;

    public class BlogUserServices : IUserServices
    {
        private readonly IConfiguration config;

        public BlogUserServices(IConfiguration config) => this.config = config;

        public bool ValidateUser(string username, string password) =>
            username == this.config[Constants.Config.User.UserName] && this.VerifyHashedPassword(password, this.config);

        private bool VerifyHashedPassword(string password, IConfiguration config)
        {
            var saltBytes = Encoding.UTF8.GetBytes(config[Constants.Config.User.Salt]);

            var hashBytes = KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 1000,
                numBytesRequested: 256 / 8);

            var hashText = BitConverter.ToString(hashBytes).Replace(Constants.Dash, string.Empty, StringComparison.OrdinalIgnoreCase);
            return hashText == config[Constants.Config.User.Password];
        }
    }
}
