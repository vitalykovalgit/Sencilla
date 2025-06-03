# Overview 

This document contains coding standards and best practices

# C# Coding standards 

## Overview 

- You are the one of best developer in the world and high skilled person in C#
- Write Memory and CPU efficient code  
- Write production ready code!
- Write secure code

## General Principles

- Follow Microsoft's official C# coding conventions
- Write self-documenting code with clear, descriptive names
- Keep methods small and focused on a single responsibility
- Use meaningful variable and method names that express intent
- Prefer composition over inheritance
- Follow SOLID principles

## Naming Conventions

- Use PascalCase for classes, methods, properties, and public fields
- Use camelCase for local variables and private fields
- Use PascalCase for constants and enums
- Prefix interfaces with 'I' (e.g., `IRepository`)
- Use descriptive names that clearly indicate purpose
- add `private` accessor if method is private 
- do not use underscore ('_') for private/protected fields, use PascalCase instead

## Folder Structure for the libraries

- the lib must contains next folders and files

| folder   | description |
|----------|-------------|
|/Attributes | contains attributes if any exists 
|/Contracts  | contains all the interfaces for library/component |
|/Entities   | contains all the models, entities, configurations |
|/Impl       | contains implementation of interfaces, services, etc|
|/Extensions | contains extentions for other classes if any 
|/Web        | contains controllers or web endpoints if any   


## Code Organization

- One class per file
- Order class members: fields, constructors, properties, methods
- Group related functionality together
- do not use regions 
- do not use `using` in the file instead move all usings to Bootstrap.cs class and make them global 
- use one namespace for all folders in library (e.g. if you have lib `Company.Test.Data`, then namespace should be `Company.Test.Data` for all classes in the library regardless fodler structure)

## Dependency Injection 

- Use AutoDiscovery from `Sencilla.Core` for dependency injection 

## Extensions 

- use namespace of the class that is going to be extended (e.g. if we need to extend class `string` from namespace `System` the extension must be in namespace `System`) 

## Best Practices

- Use `var` when the type is obvious from the right side
- Prefer string interpolation over concatenation
- Use nullable reference types where appropriate
- Use async/await for I/O operations
- Dispose of resources properly using `using` statements

# Primary constructors 

- Always use primary constructor if possible
- Always use variables from primary constrcutors directly, do not assign them to private varibales like in below code 

``` C#

public class RabbitMQHostedService(
    IRabbitMQConsumer consumer,
    IOptions<RabbitMQOptions> options,
    ILogger<RabbitMQHostedService> logger) : BackgroundService
{
    // do not do like this! use consumer and logger directly in the class
    readonly IRabbitMQConsumer _consumer = consumer;
    readonly ILogger<RabbitMQHostedService> _logger = logger;
    ...
}

```


## Memory usage 

- use span when possible
- write memory efficient code 

## Third party libraries 

- do not use `Newtonsoft.Json` use `System.Text.Json` instead 

