# Source Generator Project Structure

The source generator project has been organized into separate files for better maintainability:

## Project Structure

```
libs/SourceGenerators/
├── Sencilla.Messaging.SourceGenerators.csproj    # Project file
├── README.md                             # Documentation
├── GenerateMessageDispatcherExtensionAttribute.cs  # Attribute definition
├── MessageDispatcherExtensionGenerator.cs          # Main generator logic
└── Entities/
    ├── CommandClassReceiver.cs           # Syntax receiver for finding classes
    ├── CommandClassInfo.cs               # Data class for command information
    └── PropertyInfo.cs                   # Data class for property information
```

## File Descriptions

### `GenerateMessageDispatcherExtensionAttribute.cs`
- Defines the attribute used to mark command classes
- Contains the `MethodName` property for custom method naming

### `MessageDispatcherExtensionGenerator.cs`
- Main source generator class implementing `ISourceGenerator`
- Contains the logic for analyzing code and generating extensions
- Uses the classes from the Entities folder

### `Entities/CommandClassReceiver.cs`
- Implements `ISyntaxContextReceiver`
- Responsible for finding class declarations with attributes
- Required using statements:
  - `Microsoft.CodeAnalysis`
  - `Microsoft.CodeAnalysis.CSharp.Syntax`
  - `System.Collections.Generic`

### `Entities/CommandClassInfo.cs`
- Data class holding information about a command class
- Properties: ClassName, FullTypeName, Namespace, MethodName, Properties
- Required using statements:
  - `System.Collections.Generic`

### `Entities/PropertyInfo.cs`
- Data class holding information about a command property
- Properties: Name, Type, ParameterName
- No additional using statements required

## Key Points

1. **Separation of Concerns**: Each class has a single responsibility
2. **Using Statements**: Each file includes only the necessary using statements
3. **Namespace Consistency**: All classes are in the `Sencilla.Messaging.SourceGenerators` namespace
4. **Internal Visibility**: All helper classes are marked as `internal`

## Build Success

The compilation now succeeds without errors after properly organizing the classes and adding the required using statements to each file.
