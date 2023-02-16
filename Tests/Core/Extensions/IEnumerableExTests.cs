
namespace Sencilla.Core.Tests;

public class IEnumerableExTests
{
    [Test]
    public void StartWith_EmptyCollections_ShouldBeTrue()
    {
        var c1 = new List<int>();
        var c2 = new List<int>();

        var r = c1.StartWith(c2);
        Assert.IsTrue(r);
    }

    [Test]
    public void StartWith_SecondCollectionsEmpty_ShouldBeTrue()
    {
        var c1 = new List<int>() { 1, 2, 3 };
        var c2 = new List<int>();

        var r = c1.StartWith(c2);
        Assert.IsTrue(r);
    }

    [Test]
    public void StartWith_HappyPath_ShouldBeTrue()
    {
        var c1 = new List<int>() { 1, 2, 3 };
        var c2 = new List<int>() { 1, 2 };

        var r = c1.StartWith(c2);
        Assert.IsTrue(r);
    }

    [Test]
    public void StartWith_NotHappyPath_ShouldBeFalse()
    {
        var c1 = new List<int>() { 1, 2, 3 };
        var c2 = new List<int>() { 1, 2, 4 };

        var r = c1.StartWith(c2);
        Assert.IsFalse(r);
    }

    [Test]
    public void StartWith_FirstCollectionEmpty_ShouldBeFalse()
    {
        var c1 = new List<int>();
        var c2 = new List<int>() { 1, 2, 4 };

        var r = c1.StartWith(c2);
        Assert.IsFalse(r);
    }

    [Test]
    public void StartWith_HappyPathWithClass_ShouldBeTrue()
    {
        var c1 = new List<Type>() { typeof(int), typeof(string) };
        var c2 = new List<Type>() { typeof(int), typeof(string) };

        var r = c1.StartWith(c2);
        Assert.IsTrue(r);
    }

    [Test]
    public void StartWith_NotHappyPathWithClass_ShouldBeFalse()
    {
        var c1 = new List<Type>() { typeof(int), typeof(string) };
        var c2 = new List<Type>() { typeof(string), typeof(int) };

        var r = c1.StartWith(c2);
        Assert.IsFalse(r);
    }

    [Test]
    public void StartWithSelector_HappyPath_ShouldBeTrue()
    {
        var c1 = new List<string>() { "1", "2" };
        var c2 = new List<int>() { 1, 2 };

        var r = c2.StartWith(c1, v => v.ToString());
        Assert.IsTrue(r);
    }

    [Test]
    public void StartWithSelector_NotHappyPath_ShouldBeFalse()
    {
        var c1 = new List<string>() { "1", "2" };
        var c2 = new List<int>() { 2 };

        var r = c2.StartWith(c1, v => v.ToString());
        Assert.IsFalse(r);
    }
}
