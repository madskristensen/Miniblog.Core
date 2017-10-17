using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Miniblog.Core
{
    public class Comment
    {
        private static readonly Regex _linkRegex = new Regex("((http://|https://|www\\.)([A-Z0-9.\\-]{1,})\\.[0-9A-Z?;~&%\\(\\)#,=\\-_\\./\\+]{2,}[0-9A-Z?~&%#=\\-_/\\+])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private const string _link = "<a href=\"{0}{1}\" rel=\"nofollow\">{2}</a>";

        [Required]
        public string ID { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Author { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public DateTime PubDate { get; set; } = DateTime.UtcNow;

        public bool IsAdmin { get; set; }

        public string GetGravatar()
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(Email);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                var sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                return $"https://www.gravatar.com/avatar/{sb.ToString().ToLowerInvariant()}?s=60&d=mm" ;
            }
        }

        public string RenderContent()
        {
            return _linkRegex
                .Replace(Content, new MatchEvaluator(Evaluator))
                .Replace(Environment.NewLine, "<br />");
        }

        private static string Evaluator(Match match)
        {
            var info = CultureInfo.InvariantCulture;
            return string.Format(info, _link, !match.Value.Contains("://") ? "http://" : string.Empty, match.Value, ShortenUrl(match.Value, 50));
        }

        private static string ShortenUrl(string url, int max)
        {
            if (url.Length <= max)
            {
                return url;
            }

            // Remove the protocal
            var startIndex = url.IndexOf("://");
            if (startIndex > -1)
            {
                url = url.Substring(startIndex + 3);
            }

            if (url.Length <= max)
            {
                return url;
            }

            // Compress folder structure
            var firstIndex = url.IndexOf("/") + 1;
            var lastIndex = url.LastIndexOf("/");
            if (firstIndex < lastIndex)
            {
                url = url.Remove(firstIndex, lastIndex - firstIndex);
                url = url.Insert(firstIndex, "...");
            }

            if (url.Length <= max)
            {
                return url;
            }

            // Remove URL parameters
            var queryIndex = url.IndexOf("?");
            if (queryIndex > -1)
            {
                url = url.Substring(0, queryIndex);
            }

            if (url.Length <= max)
            {
                return url;
            }

            // Remove URL fragment
            var fragmentIndex = url.IndexOf("#");
            if (fragmentIndex > -1)
            {
                url = url.Substring(0, fragmentIndex);
            }

            if (url.Length <= max)
            {
                return url;
            }

            // Compress page
            firstIndex = url.LastIndexOf("/") + 1;
            lastIndex = url.LastIndexOf(".");
            if (lastIndex - firstIndex > 10)
            {
                var page = url.Substring(firstIndex, lastIndex - firstIndex);
                var length = url.Length - max + 3;
                if (page.Length > length)
                {
                    url = url.Replace(page, string.Format("...{0}", page.Substring(length)));
                }
            }

            return url;
        }
    }
}
