# MessageDispatcher Source Generator

This source generator automatically creates extension methods for `IMessageDispatcher` based on command classes marked with the `[GenerateMessageDispatcherExtension]` attribute.

## Usage

### 1. Mark your command class with the attribute

```csharp
namespace Sencilla.Messaging.SourceGenerator;

[GenerateMessageDispatcherExtension(MethodName = "PrepareSpreadImage")]
public class PrepareSpreadImage
{
    public int ImageId { get; set; }
}
```

### 2. Use the generated extension method

```csharp
using Sencilla.Messaging.Extensions;
using Sencilla.Messaging;

public async Task ProcessImage(IMessageDispatcher dispatcher)
{
    // Instead of manually creating the command:
    // await dispatcher.Send(new PrepareSpreadImage { ImageId = 42 });
    
    // Use the generated extension method:
    await dispatcher.PrepareSpreadImage(imageId: 42);
}
```

## Generated Code

For the example above, the generator creates:

```csharp
public static class MessageDispatcherExtensions
{
    /// <summary>
    /// Sends a PrepareSpreadImage command.
    /// </summary>
    /// <param name="imageId">The ImageId value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task PrepareSpreadImage(this IMessageDispatcher dispatcher, int imageId, CancellationToken cancellationToken = default)
    {
        return dispatcher.Send(new PrepareSpreadImage
        {
            ImageId = imageId,
        }, cancellationToken);
    }
}
```

## Complex Example

```csharp
[GenerateMessageDispatcherExtension(MethodName = "ProcessOrderImage")]
public class ProcessImageCommand
{
    public int OrderId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public double Quality { get; set; }
    public DateTime Deadline { get; set; }
    public bool IsUrgent { get; set; }
}
```

This generates:

```csharp
public static Task ProcessOrderImage(this IMessageDispatcher dispatcher, 
    int orderId, 
    string filePath, 
    int width, 
    int height, 
    double quality, 
    DateTime deadline, 
    bool isUrgent, 
    CancellationToken cancellationToken = default)
{
    return dispatcher.Send(new ProcessImageCommand
    {
        OrderId = orderId,
        FilePath = filePath,
        Width = width,
        Height = height,
        Quality = quality,
        Deadline = deadline,
        IsUrgent = isUrgent,
    }, cancellationToken);
}
```

## Features

- ✅ **Automatic method name generation**: Uses class name by default, removes "Command" suffix
- ✅ **Custom method names**: Use `MethodName` parameter in the attribute
- ✅ **Type-safe parameters**: All properties become method parameters with correct types
- ✅ **Parameter name conversion**: PascalCase properties become camelCase parameters
- ✅ **Documentation generation**: Automatic XML documentation for generated methods
- ✅ **Cancellation token support**: All generated methods include optional CancellationToken
- ✅ **Multiple commands**: Generate extensions for multiple command classes in the same project

## Requirements

- The command class must have public properties with public getters and setters
- The class must be marked with `[GenerateMessageDispatcherExtension]` attribute
- The project must reference the `Sencilla.Messaging.SourceGenerators` project as an Analyzer

## Installation

Add the source generator project reference to your project file:

```xml
<ItemGroup>
  <ProjectReference Include="../../libs/SourceGenerator/Sencilla.Messaging.SourceGenerators.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="true" />
</ItemGroup>
```
