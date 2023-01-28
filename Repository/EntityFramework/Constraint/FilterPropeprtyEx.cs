namespace Sencilla.Repository.EntityFramework
{
    public static class FilterPropeprtyEx
    {
        /// <summary>
        /// If no values and type we will treat it as a query 
        /// otherwise it will treat query as name of property in entity
        /// and convert it to expr: name in (val1, val2) 
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static string? ToExpression(this FilterProperty prop)
        {
            if (prop.Type == null)
                return prop.Query;

            if (prop.Values == null || prop.Values.Count == 0)
                return prop.Query;

            var vals = new StringBuilder();
            foreach (var v in prop.Values)
            {
                if (prop.Type == typeof(string))
                {
                    vals.Append($"\"{v}\",");
                }
                else
                {
                    vals.Append($"{v},");
                }
            }

            if (vals.Length > 0)
                vals.Remove(vals.Length - 1, 1);

            return $"{prop.Query} in ({vals})";
        }
    }
}
