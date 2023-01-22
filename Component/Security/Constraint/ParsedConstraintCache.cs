using System.Collections.Concurrent;

namespace Sencilla.Component.Security
{
    /// <summary>
    /// Cache for parsed constraints 
    /// </summary>
    public class ParsedConstraintCache
    {
        /// <summary>
        /// 
        /// </summary>
        private static ConcurrentDictionary<string, ParsedConstraint> Cache = new();

        public static ParsedConstraint Get(string constraint)
        {
            // 
            if (Cache.ContainsKey(constraint))
                return Cache[constraint];

            // 
            var parsed = new ParsedConstraint(constraint);
            Cache[constraint] = parsed;
            return parsed;
        }
    }
}
