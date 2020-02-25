namespace Miniblog.Core.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// The Comment class.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        /// <value>The author.</value>
        [Required]
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>The content.</value>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>The email.</value>
        [Required, EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [Required]
        public string ID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets a value indicating whether this instance is admin.
        /// </summary>
        /// <value><c>true</c> if this instance is admin; otherwise, <c>false</c>.</value>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// Gets or sets the pub date.
        /// </summary>
        /// <value>The pub date.</value>
        [Required]
        public DateTime PubDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the gravatar.
        /// </summary>
        /// <returns>System.String.</returns>
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

        /// <summary>
        /// Renders the content.
        /// </summary>
        /// <returns>The content.</returns>
        public string RenderContent() => this.Content;
    }
}
