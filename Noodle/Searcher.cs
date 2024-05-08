using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Noodle
{
    public class Searcher
    {
        public bool WholeWord;
        public bool CaseSensitive;
        public bool Contains = true;
        public bool ExactMatch;
        public bool Regex;
        //public bool SoundEx;
        private Regex regex;

        public List<CacheEntry> Find(List<CacheEntry> cache, string searchTerm)
        {
            var results = new List<CacheEntry>();
            foreach (var entry in cache)
            {
                if (FindIt(entry.Value, searchTerm)) 
                { 
                    if(Contains && entry.Value.Length > Shared.MaxCellContentLength)
                    {
                        var haystack = entry.Value.ToString().ToLowerInvariant();
                        var needle = searchTerm.ToLowerInvariant();
                        var foundAt = haystack.IndexOf(needle);

                        var startIndex = haystack.IndexOf(needle);
                        var avail = Shared.MaxCellContentLength - needle.Length;
                        var half = avail / 2;
                        startIndex = startIndex - half > 0 ? startIndex - half : 0;
                        var newval = "";
                        var takeLen = Shared.MaxCellContentLength < (haystack.Length - startIndex) ? Shared.MaxCellContentLength : haystack.Length - startIndex;

                        if (takeLen > 0)
                        {
                            newval = haystack.Substring(startIndex, takeLen);
                        }
                        else
                        {
                            newval = haystack.Substring(startIndex);
                        }
                        //var remainingCount = entry.Value.ToString().Length + 1 - Shared.MaxCellContentLength;
                        //var left = (int)Math.Floor((decimal)remainingCount / 2m);
                        //var right = (int)Math.Floor((decimal)remainingCount / 2m) + searchTerm.Length;

                        //var newval = entry.Value.ToString().Substring(left, right);
                        entry.Value = newval;
                    }
                    results.Add(entry);                    
                }
            }
            return results;
        }

        private bool FindIt(string haystack, string needle)
        {
            if (Regex)
            {
                return new Regex(needle).IsMatch(haystack);
            }

            if(ExactMatch)
            {
                return new Regex(needle).IsMatch(haystack);
            }

            var regex = new Regex(needle);
            if(WholeWord)
            {
                needle = $"\\b{needle}\\b";  //If "Contains" then just don't add the /b (word boundary) anchor
                regex = new Regex(needle);
            }
            if(!CaseSensitive)
            {
                regex = new Regex(needle, RegexOptions.IgnoreCase);
            }
            if (haystack.ToLower().Contains(needle.ToLower())) { return true; }
            return false;
            //return regex.IsMatch(haystack);
        }
    }
}
