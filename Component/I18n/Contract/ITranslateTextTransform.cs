namespace Sencilla.Component.I18n;

public interface ITranslateTextTransform
{
    void Transform(TranslateDefinition translateDefinition);
    void TransformBack(TranslateDefinition translateDefinition);
}
