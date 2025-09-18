namespace Sencilla.Core.Tests;

public class InjectionTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void InterfaceAutoImplementTests()
    {
        var srv = new ServiceCollection();
        //var con = new ServiceCollectionRegistrator(srv);
        var reg = new AutoDiscoveryRegistrator();

        reg.Register(srv, typeof(EntityBase));
        reg.Register(srv, typeof(EntitySingle));
        reg.Register(srv, typeof(EntityMulti));
        reg.Register(srv, typeof(EntityMultiInherits));

        Assert.Pass();
    }
}

interface ITestBase
{
    void mb();
};

interface ITestInterface1
{
    void m1();
};

interface ITestInterface2
{
    void m2();
};

interface ITestInterface3 : ITestBase, ITestInterface2
{
    void m4();
};

class EntityBase: ITestBase
{
    public void m3() { }
    public void mb() { }
}

class EntitySingle : EntityBase, ITestInterface1
{
    public void m1() { }
}

class EntityMulti : EntityBase, ITestInterface1, ITestInterface2
{
    public void m1() { }
    public void m2() { }
}

class EntityMultiInherits : EntityBase, ITestInterface3, ITestInterface1
{
    public void m1() { }
    public void m2() { }
    public void m4() { }
}