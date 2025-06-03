namespace Sencilla.Component.I18n;

public class NumericTranslateTextTransform 
{
    private const string pattern = @"{(\w*)}";

    public void Transform(TranslateDefinition translateDefinition)
    {
        var processedText = Regex.Replace(translateDefinition.Text, pattern, new MatchEvaluator((match) =>
        {
            var nextIndex = translateDefinition.Params.Count;
            translateDefinition.Params.Add(nextIndex.ToString(), match.Value);

            return $"{{{nextIndex}}}";
        }));

        translateDefinition.Text = processedText;
    }

    public void TransformBack(TranslateDefinition translateDefinition)
    {
        var processedText = Regex.Replace(translateDefinition.Text, pattern, new MatchEvaluator((match) =>
        {
            var key = match.Value.Substring(1, match.Value.Length - 2);
            return translateDefinition.Params.ContainsKey(key) ? translateDefinition.Params[key] : match.Value;
        }));

        translateDefinition.Text = processedText;
    }
}
