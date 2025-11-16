namespace Miniblog.Core.Services;

/// <summary>
/// Provides user-related services such as validation.
/// </summary>
public interface IUserServices
{
    /// <summary>
    /// Validates the specified user's credentials.
    /// </summary>
    /// <param name="username">The username to validate.</param>
    /// <param name="password">The password to validate.</param>
    /// <returns><c>true</c> if the credentials are valid; otherwise, <c>false</c>.</returns>
    bool ValidateUser(string username, string password);
}
