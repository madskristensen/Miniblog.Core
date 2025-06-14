namespace Miniblog.Core.Services;

public interface IUserServices
{
    public bool ValidateUser(string username, string password);
}
