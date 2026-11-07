using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Sencilla.Web.Tests;

/// <summary>
/// EF's AddRange/UpdateRange write every row a body's navigations reach, and no event judges those rows, so nothing
/// below an entity binds from a CRUD body. Read through the options <c>AddSencillaWeb</c> gives MVC, as a body is.
/// </summary>
public class RequestBodyNavigationsTests
{
    public class Owner : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = "";
    }

    public class Project : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public Guid OwnerId { get; set; }
        public List<string> Tags { get; set; } = [];

        public Owner? Owner { get; set; }
        public List<Owner>? Members { get; set; }
        public Owner[]? Reviewers { get; set; }
    }

    static JsonSerializerOptions MvcBodyOptions()
    {
        var services = new ServiceCollection();
        services.AddSencillaWeb(services.AddControllers());
        return services.BuildServiceProvider().GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
    }

    [Fact]
    public void ABody_BindsItsOwnColumns_AndNoRowBelowIt()
    {
        var (id, owner) = (Guid.NewGuid(), Guid.NewGuid());
        var body = $$"""
            { "id": "{{id}}", "name": "mine", "ownerId": "{{owner}}", "tags": ["a"],
              "owner": { "id": "{{Guid.NewGuid()}}", "email": "attacker@example.com" },
              "members": [{ "id": "{{Guid.NewGuid()}}" }],
              "reviewers": [{ "id": "{{Guid.NewGuid()}}" }] }
            """;

        var project = JsonSerializer.Deserialize<Project>(body, MvcBodyOptions())!;

        Assert.Equal((id, "mine", owner, "a"), (project.Id, project.Name, project.OwnerId, Assert.Single(project.Tags)));
        Assert.Null(project.Owner);
        Assert.Null(project.Members);
        Assert.Null(project.Reviewers);
    }

    /// <summary>`?with=` reads go on working: only the setter is gone.</summary>
    [Fact]
    public void AResponse_StillCarriesItsNavigations()
    {
        var json = JsonSerializer.Serialize(new Project { Owner = new Owner { Email = "o@example.com" }, Members = [new Owner()] }, MvcBodyOptions());

        Assert.Contains("\"owner\":{", json);
        Assert.Contains("\"members\":[", json);
    }
}
