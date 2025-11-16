using System.ComponentModel.DataAnnotations;

namespace Miniblog.Core.Models;

/// <summary>
/// ViewModel for user login functionality.
/// </summary>
public class LoginViewModel
{
    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the user should be remembered.
    /// </summary>
    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; } = false;

    /// <summary>
    /// Gets or sets the user's username.
    /// </summary>
    [Required]
    public string UserName { get; set; } = string.Empty;
}
