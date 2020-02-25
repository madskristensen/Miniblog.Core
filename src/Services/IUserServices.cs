namespace Miniblog.Core.Services
{
    /// <summary>
    /// The user services interface.
    /// </summary>
    public interface IUserServices
    {
        /// <summary>
        /// Validates the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns><c>true</c> if the user is valid, <c>false</c> otherwise.</returns>
        bool ValidateUser(string username, string password);
    }
}
