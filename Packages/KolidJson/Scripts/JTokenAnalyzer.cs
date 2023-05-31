using System;
using Newtonsoft.Json.Linq;

namespace KolidSoft.Json.Analyzer
{
    public class NotFoundException : Exception { }

    public static class JTokenAnalyzer
    {
        public static JToken FindFirst(JToken root, string name)
        {
            var token = root.SelectToken(name);
            if (token != null)
                return token;

            foreach (var son in root.Children())
            {
                var ret = FindFirst(son, name);
                if (ret != null)
                    return ret;
            }

            return null;
        }
    }
}
