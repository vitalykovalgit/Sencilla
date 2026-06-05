global using System.Security.Claims;
global using System.Text;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Diagnostics;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Options;
global using Microsoft.IdentityModel.JsonWebTokens;
global using Microsoft.IdentityModel.Tokens;
global using Moq;
global using Sencilla.Core;
global using Sencilla.Component.Users;
global using Sencilla.Repository.EntityFramework;
global using Xunit;

// Disambiguate: both Sencilla.Authentication and Sencilla.Component.Users live in scope.
global using Sencilla.Authentication;
