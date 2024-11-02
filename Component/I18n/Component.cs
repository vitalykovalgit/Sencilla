global using Sencilla.Core;
global using Sencilla.Component.I18n;

global using System.Globalization;
global using System.Text.RegularExpressions;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;

global using Google.Apis.Auth.OAuth2;
global using Google.Cloud.Translate.V3;

global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Localization;
global using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public class I18nComponent : IComponent
{
    public string Type => "Sencilla.Component.I18n";

    public void Init(IContainer container) { }
}
