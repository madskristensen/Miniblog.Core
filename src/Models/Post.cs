namespace Miniblog.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The Post class.
    /// </summary>
    public class Post
    {
        /// <summary>
        /// Gets or sets the categories.
        /// </summary>
        /// <value>The categories.</value>
        public IList<string> Categories { get; } = new List<string>();

        /// <summary>
        /// Gets the comments.
        /// </summary>
        /// <value>The comments.</value>
        public IList<Comment> Comments { get; } = new List<Comment>();

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>The content.</value>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the excerpt.
        /// </summary>
        /// <value>The excerpt.</value>
        [Required]
        public string Excerpt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [Required]
        public string ID { get; set; } = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Gets or sets a value indicating whether this instance is published.
        /// </summary>
        /// <value><c>true</c> if this instance is published; otherwise, <c>false</c>.</value>
        public bool IsPublished { get; set; } = true;

        /// <summary>
        /// Gets or sets the last modified.
        /// </summary>
        /// <value>The last modified.</value>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the pub date.
        /// </summary>
        /// <value>The pub date.</value>
        public DateTime PubDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the slug.
        /// </summary>
        /// <value>The slug.</value>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [Required]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Creates the slug.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <returns>System.String.</returns>
        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "The slug should be lower case.")]
        public static string CreateSlug(string title)
        {
            title = title?.ToLowerInvariant().Replace(
                Constants.Space, Constants.Dash, StringComparison.OrdinalIgnoreCase) ?? string.Empty;
            title = RemoveDiacritics(title);
            title = RemoveReservedUrlCharacters(title);

            return title.ToLowerInvariant();
        }

        /// <summary>
        /// Ares the comments open.
        /// </summary>
        /// <param name="commentsCloseAfterDays">The comments close after days.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool AreCommentsOpen(int commentsCloseAfterDays) =>
            this.PubDate.AddDays(commentsCloseAfterDays) >= DateTime.UtcNow;

        /// <summary>
        /// Gets the encoded link.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetEncodedLink() => $"/blog/{System.Net.WebUtility.UrlEncode(this.Slug)}/";

        /// <summary>
        /// Gets the link.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetLink() => $"/blog/{this.Slug}/";

        /// <summary>
        /// Renders the content.
        /// </summary>
        /// <returns>System.String.</returns>
        public string RenderContent()
        {
            var result = this.Content;

            // Set up lazy loading of images/iframes
            if (!string.IsNullOrEmpty(result))
            {
                // Set up lazy loading of images/iframes
                var replacement = " src=\"data:image/gif;base64,R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==\" data-src=\"";
                var pattern = "(<img.*?)(src=[\\\"|'])(?<src>.*?)([\\\"|'].*?[/]?>)";
                result = Regex.Replace(result, pattern, m => m.Groups[1].Value + replacement + m.Groups[4].Value + m.Groups[3].Value);

                // Youtube content embedded using this syntax: [youtube:xyzAbc123]
                var video = "<div class=\"video\"><iframe width=\"560\" height=\"315\" title=\"YouTube embed\" src=\"about:blank\" data-src=\"https://www.youtube-nocookie.com/embed/{0}?modestbranding=1&amp;hd=1&amp;rel=0&amp;theme=light\" allowfullscreen></iframe></div>";
                result = Regex.Replace(
                    result,
                    @"\[youtube:(.*?)\]",
                    m => string.Format(CultureInfo.InvariantCulture, video, m.Groups[1].Value));
            }

            return result;
        }

        /// <summary>
        /// Removes the diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>System.String.</returns>
        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Removes the reserved URL characters.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>System.String.</returns>
        private static string RemoveReservedUrlCharacters(string text)
        {
            var reservedCharacters = new List<string> { "!", "#", "$", "&", "'", "(", ")", "*", ",", "/", ":", ";", "=", "?", "@", "[", "]", "\"", "%", ".", "<", ">", "\\", "^", "_", "'", "{", "}", "|", "~", "`", "+" };

            foreach (var chr in reservedCharacters)
            {
                text = text.Replace(chr, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }
    }
}
