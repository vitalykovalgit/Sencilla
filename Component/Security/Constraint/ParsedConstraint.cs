using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;

using Sencilla.Core;

namespace Sencilla.Component.Security
{
    /// <summary>
    /// 
    /// </summary>
    public class ParsedConstraint
    {
        private List<string> Params = new();

        public ParsedConstraint(string constraint) 
        {
            Constraint = Parse(constraint);
        }

        /// <summary>
        /// Parsed constraint
        /// </summary>
        public string Constraint { get; }

        /// <summary>
        /// return array of values from system variables
        /// </summary>
        /// <param name="sysVars"></param>
        /// <returns></returns>
        public object[] Vars(ISystemVariable sysVars) 
        {
            // for debug purpose 
            var vars = Params.Select(p => sysVars.Get(p));
            return vars.ToArray();
        }

        /// <summary>
        /// Parse constraint. 
        /// Replace {varname} to @idx (like @0, @1)
        /// </summary>
        /// <param name="constraint"> contraint to parse </param>
        /// <returns></returns>
        private string Parse(string constraint)
        {
            // replace all placeholders 
            var idx = 0;
            var parsed = Regex.Replace(constraint, "{[\\w\\s\\d.]*}", match =>
            {
                // add vars 
                var val = match.Value.Trim(' ', '{', '}');
                var var = new Regex("[\\s\\w\\d]*").Replace(val, m =>
                {
                    Params.Add(m.Value.ToLower());
                    return $"@{idx++}";
                }, 1);

                return var;
            });
            return parsed;
        }
    }
}
