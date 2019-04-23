using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WpfAppFace
{
    class StringHelper
    {
        /// <summary>
        /// The ID is captured within () in the field...
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetIdFromText(string text)
        {
            foreach (Match match in Regex.Matches(text, @"\(.*?\)", RegexOptions.IgnoreCase))
            {
                yield return match.Value.Replace("(", "").Replace(")", "");
            }
        }
    }
}
