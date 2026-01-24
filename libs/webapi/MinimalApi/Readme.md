

[PostApi("api/v1/projects/set-padding")]
class SetPaddingRequest
{
	int padding { get; set; }
	int ProjectId { get; set; }
	int[] ItemIds { get; set; }
}


The Minimal API feature allows you to define API endpoints using simple classes and attributes, reducing boilerplate code and improving readability.

To use Minimal APIs in your project, ensure that your project is targeting .NET 10.0 or higher and that the LangVersion is set to 14.0 or higher in your project file.
Here's an example of how to define a POST API endpoint using Minimal APIs:
```csharp

[PostApi("api/v1/projects/set-padding")]
class SetPaddingRequest
{
	int padding { get; set; }
	int ProjectId { get; set; }
	int[] ItemIds { get; set; }
}
```

In this example, we define a POST API endpoint at "api/v1/projects/set-padding" using the `PostApi` attribute. The `SetPaddingRequest` class contains properties that represent the data expected in the request body.

To handle the request, you would typically create a method that takes an instance of `SetPaddingRequest` as a parameter. The Minimal API framework will automatically bind the incoming request data to this class.

```csharp
app.MapPost("api/v1/projects/set-padding", (SetPaddingRequest request) =>
{
	// Your logic to set padding for the specified project and items
	return Results.Ok();
});
```