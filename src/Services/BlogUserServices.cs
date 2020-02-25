namespace Miniblog.Core.Services
{
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;
    using Microsoft.Extensions.Configuration;

    using System;
    using System.Text;

    /// <summary>
    /// The BlogUserServices class. Implements the <see cref="Miniblog.Core.Services.IUserServices" />
    /// </summary>
    /// <seealso cref="Miniblog.Core.Services.IUserServices" />
    public class BlogUserServices : IUserServices
    {
        /// <summary>
        /// The configuration
        /// </summary>
        private readonly IConfiguration config;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlogUserServices" /> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public BlogUserServices(IConfiguration config) => this.config = config;

        /// <summary>
        /// Validates the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns><c>true</c> if the user is valid, <c>false</c> otherwise.</returns>
        public bool ValidateUser(string username, string password) =>
            username == this.config["user:username"] && this.VerifyHashedPassword(password, this.config);

        /// <summary>
        /// Verifies the hashed password.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <param name="config">The configuration.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool VerifyHashedPassword(string password, IConfiguration config)
        {
            var saltBytes = Encoding.UTF8.GetBytes(config["user:salt"]);

            var hashBytes = KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 1000,
                numBytesRequested: 256 / 8);

            var hashText = BitConverter.ToString(hashBytes).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
            return hashText == config["user:password"];
        }
    }
}
