namespace Miniblog.Core.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;

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

        [SuppressMessage(
            "Security",
            "CA5351:Do Not Use Broken Cryptographic Algorithms",
            Justification = "We aren't using it for encryption so we don't care.")]
        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "It is an email address.")]
        public string GetGravatar()
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(this.Email.Trim().ToLowerInvariant());
            var hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            var sb = new StringBuilder();
            for (var i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2", CultureInfo.InvariantCulture));
            }

            return $"https://www.gravatar.com/avatar/{sb.ToString().ToLowerInvariant()}?s=60&d=blank";
        }

        public string RenderContent() => this.Content;
    }
}
