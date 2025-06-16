namespace Miniblog.Core.Services;

using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Configuration;

using System;
using System.Text;

public class BlogUserServices(IConfiguration config) : IUserServices
{
    public bool ValidateUser(string username, string password) =>
        username == config[Constants.Config.User.UserName] && VerifyHashedPassword(password, config);

    private static bool VerifyHashedPassword(string password, IConfiguration config)
    {
        var saltBytes = Encoding.UTF8.GetBytes(config[Constants.Config.User.Salt]!);

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
