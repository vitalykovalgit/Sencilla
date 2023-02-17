//namespace Sencilla.Component.Security.Tests;

//public class Tests
//{
//    [SetUp]
//    public void Setup()
//    {
//    }

//    [Test]
//    public void ExpressionParser_ParseSimpleExpression()
//    {
//        var exp = "UserId=2";
//        var res = ConstraintExpressionParser.Parse(exp);
        
//        Assert.NotNull(res);
//        Assert.That(res.Field, Is.EqualTo("UserId"));
//        Assert.That(res.Value, Is.EqualTo(2));
//        Assert.That(res.Operator, Is.EqualTo("="));
//    }

//    [Test]
//    public void ExpressionParser_ParseSimpleExpressionWithSpaces()
//    {
//        var exp = "  UserId =  2  ";
//        var res = ConstraintExpressionParser.Parse(exp);

//        Assert.NotNull(res);
//        Assert.That(res.Field, Is.EqualTo("UserId"));
//        Assert.That(res.Value, Is.EqualTo(2));
//        Assert.That(res.Operator, Is.EqualTo("="));
//    }

//    [Test]
//    public void ExpressionParser_ParseSimpleExpressionWithText()
//    {
//        var exp = "UserId = \"email\"";
//        var res = ConstraintExpressionParser.Parse(exp);

//        Assert.NotNull(res);
//        Assert.That(res.Field, Is.EqualTo("UserId"));
//        Assert.That(res.Value, Is.EqualTo("email"));
//        Assert.That(res.Operator, Is.EqualTo("="));
//    }

//    [Test]
//    public void ExpressionParser_ParseNumberExpression()
//    {
//        var exp = " UserId = -2 ";
//        var res = ConstraintExpressionParser.Parse(exp);

//        Assert.NotNull(res);
//        Assert.That(res.Field, Is.EqualTo("UserId"));
//        Assert.That(res.Value, Is.EqualTo(-2));
//        Assert.That(res.Operator, Is.EqualTo("="));
//    }
//}