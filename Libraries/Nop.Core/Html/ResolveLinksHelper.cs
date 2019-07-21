using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Nop.Core.Html
{
    /// <summary>
    /// Represents a ResolveLinks helper
    /// </summary>
    public partial class ResolveLinksHelper
    {
        #region Fields

        private const string LINK = "<a href=\"{0}{1}\" rel=\"nofollow\">{2}</a>";
        private const int MAX_LENGTH = 50;

        /// <summary>
        /// The regular expression used to parse links.
        /// </summary>
        private static readonly Regex _regex = new Regex("((http://|https://|www\\.)([A-Z0-9.\\-]{1,})\\.[0-9A-Z?;~&\\(\\)#,=\\-_\\./\\+]{2,})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        #endregion

        #region Utilities

        /// <summary>
        /// Shortens any absolute URL to a specified maximum length
        /// </summary>
        private static string ShortenUrl(string url, int max)
        {
            if (url.Length <= max)
                return url;

            // Remove the protocol
            var startIndex = url.IndexOf("://", StringComparison.InvariantCultureIgnoreCase);
            if (startIndex > -1)
                url = url.Substring(startIndex + 3);

            if (url.Length <= max)
                return url;

            // Compress folder structure
            var firstIndex = url.IndexOf("/", StringComparison.InvariantCultureIgnoreCase) + 1;
            var lastIndex = url.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase);
            if (firstIndex < lastIndex)
            {
                url = url.Remove(firstIndex, lastIndex - firstIndex);
                url = url.Insert(firstIndex, "...");
            }

            if (url.Length <= max)
                return url;

            // Remove URL parameters
            var queryIndex = url.IndexOf("?", StringComparison.InvariantCultureIgnoreCase);
            if (queryIndex > -1)
                url = url.Substring(0, queryIndex);

            if (url.Length <= max)
                return url;

            // Remove URL fragment
            var fragmentIndex = url.IndexOf("#", StringComparison.InvariantCultureIgnoreCase);
            if (fragmentIndex > -1)
                url = url.Substring(0, fragmentIndex);

            if (url.Length <= max)
                return url;

            // Compress page
            firstIndex = url.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase) + 1;
            lastIndex = url.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);

            if (lastIndex - firstIndex <= 10) 
                return url;

            var page = url.Substring(firstIndex, lastIndex - firstIndex);
            var length = url.Length - max + 3;
            if (page.Length > length)
                url = url.Replace(page, "..." + page.Substring(length));

            return url;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Formats the text
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Formatted text</returns>
        public static string FormatText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var info = CultureInfo.InvariantCulture;
            foreach (Match match in _regex.Matches(text))
            {
                text = text.Replace(match.Value,
                    !match.Value.Contains("://")
                        ? string.Format(info, LINK, "http://", match.Value, ShortenUrl(match.Value, MAX_LENGTH))
                        : string.Format(info, LINK, string.Empty, match.Value, ShortenUrl(match.Value, MAX_LENGTH)));
            }

            return text;
        }

        #endregion
    }
}
