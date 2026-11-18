using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sencilla.Authentication.Server.Tests;

public class AuthenticationConfigTests
{
    [Fact]
    public void SignInEndpoints_WithoutASessionTransport_FailAtStartup()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddSencillaAuthentication(
            new ConfigurationBuilder().Build(), a => a.UseAppAccounts().UseUsers(u => u.EntityFrameworkStore())));

        Assert.Contains("UseJwtToken()", ex.Message);
    }
}
