using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Miniblog.Core.Models;

public class Comment
{
    [Required]
    public string Author { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string ID { get; set; } = Guid.NewGuid().ToString();

    public bool IsAdmin { get; set; } = false;

    [Required]
    public DateTime PubDate { get; set; } = DateTime.UtcNow;

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "It is an email address.")]
    [SuppressMessage(
        "Performance",
        "CA1850:Prefer static 'HashData' method over 'ComputeHash'",
        Justification = "We aren't using it for encryption so we don't care.")]
    [SuppressMessage(
        "Security",
        "CA5351:Do Not Use Broken Cryptographic Algorithms",
        Justification = "We aren't using it for encryption so we don't care.")]
    public string GetGravatar()
    {
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(Email.Trim().ToLowerInvariant());
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        // Convert the byte array to hexadecimal string
        StringBuilder sb = new();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            _ = sb.Append(hashBytes[i].ToString("X2", CultureInfo.InvariantCulture));
        }

        return $"https://www.gravatar.com/avatar/{sb.ToString().ToLowerInvariant()}?s=60&d=blank";
    }

    public string RenderContent() => Content;
}
