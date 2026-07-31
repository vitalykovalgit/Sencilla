using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Reflection;

namespace Sencilla.Web.Tests;

/// <summary>Stands in for a component's opt-in marker (<c>IEntityTaggable</c>, ...).</summary>
public interface IEntityFeatured
{
}

[CrudApi("api/v1/featured")]
public class FeaturedThing : IEntity<int>, IEntityFeatured
{
    public int Id { get; set; }
}

[CrudApi("api/v1/plain")]
public class PlainThing : IEntity<int>
{
    public int Id { get; set; }
}

/// <summary>
/// Stands in for a component-supplied controller (TagApiController): fits only some entities, and mounts the
/// same literal/parameter route shapes onto the entity's route that the real one does.
/// </summary>
[EntityApi]
public class ConstrainedApiController<TEntity, TKey>(IServiceProvider resolver) : ApiController(resolver)
    where TEntity : class, IEntity<TKey>, IEntityFeatured, new()
{
    [HttpGet, Route("tags")]
    public IActionResult GetTags() => Ok();

    [HttpGet, Route("{id}/tags")]
    public IActionResult GetTags(TKey id) => Ok();
}

/// <summary>
/// The seam that lets a component add endpoints to every entity's API without Sencilla.Web referencing it:
/// an open generic controller marked <see cref="EntityApiAttribute"/> is closed over each <c>[CrudApi]</c>
/// entity it fits, and routed under that entity's route.
/// </summary>
public class EntityApiControllerTests
{
    static ControllerFeature Populate()
    {
        var feature = new ControllerFeature();
        new EntityApiControllerFeatureProvider().PopulateFeature([], feature);
        return feature;
    }

    [Fact]
    public void EveryEntity_GetsEveryGenericControllerItFits()
    {
        var controllers = Populate().Controllers;

        Assert.Contains(typeof(CrudApiController<FeaturedThing, int>).GetTypeInfo(), controllers);
        Assert.Contains(typeof(CrudApiController<PlainThing, int>).GetTypeInfo(), controllers);
        Assert.Contains(typeof(ConstrainedApiController<FeaturedThing, int>).GetTypeInfo(), controllers);
    }

    [Fact]
    public void AControllerWhoseConstraintsDoNotFit_IsSkippedRatherThanFatal()
    {
        // The whole point of the constraint-as-applicability-rule: PlainThing doesn't carry the marker, so it
        // gets no ConstrainedApiController — and the entities that DO fit are still emitted.
        var controllers = Populate().Controllers;

        Assert.DoesNotContain(controllers, c => c.IsGenericType
            && c.GetGenericTypeDefinition() == typeof(ConstrainedApiController<,>)
            && c.GenericTypeArguments[0] == typeof(PlainThing));
    }

    [Fact]
    public void OnlyTwoArgumentGenerics_AreClosed()
    {
        // CrudApiController<TEntity> is a convenience base for hand-written controllers, not an entity API
        // surface; arity is what keeps it (and anything else inheriting the attribute) out.
        Assert.DoesNotContain(Populate().Controllers, c => c.IsGenericType
            && c.GetGenericTypeDefinition() == typeof(CrudApiController<>));
    }

    [Theory]
    [InlineData(typeof(CrudApiController<FeaturedThing, int>))]
    [InlineData(typeof(ConstrainedApiController<FeaturedThing, int>))]
    public void EveryGenericController_IsRoutedUnderItsEntity(Type closed)
    {
        var model = new ControllerModel(closed.GetTypeInfo(), []);

        new EntityApiControllerRouteConvention().Apply(model);

        Assert.Equal(nameof(FeaturedThing), model.ControllerName);
        Assert.Equal("api/v1/featured", Assert.Single(model.Selectors).AttributeRouteModel?.Template);
    }

    [Fact]
    public void AnUnmarkedController_IsLeftAlone()
    {
        var model = new ControllerModel(typeof(ApiController).GetTypeInfo(), []);

        new EntityApiControllerRouteConvention().Apply(model);

        Assert.Empty(model.Selectors);
    }

    /// <summary>
    /// The load-bearing one: splitting an entity's API across two controllers puts them on the SAME route
    /// prefix under the SAME controller name, so MVC must still produce one unambiguous action per template.
    /// A collision here would only surface as an AmbiguousMatchException at request time, in production.
    /// </summary>
    [Fact]
    public void TwoControllersOnOneEntity_ProduceDistinctRoutes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSencillaWeb(services.AddControllers());

        var actions = services.BuildServiceProvider()
            .GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items
            .Where(a => a.AttributeRouteInfo?.Template?.StartsWith("api/v1/featured") == true)
            .ToList();

        var templates = actions.Select(a => $"{a.ActionConstraints?.OfType<HttpMethodActionConstraint>().SingleOrDefault()?.HttpMethods.Single()} {a.AttributeRouteInfo!.Template}").ToList();

        Assert.Contains("GET api/v1/featured/tags", templates);          // the component's controller
        Assert.Contains("GET api/v1/featured/{id}/tags", templates);     // ...
        Assert.Contains("GET api/v1/featured/{id}", templates);          // ...alongside CRUD's
        Assert.Equal(templates.Count, templates.Distinct().Count());
    }
}
