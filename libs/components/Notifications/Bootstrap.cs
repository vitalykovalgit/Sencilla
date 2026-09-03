global using System.ComponentModel.DataAnnotations.Schema;
global using System.Reflection;
global using System.Text.Json;

global using Sencilla.Core;
global using Sencilla.Web;
global using Sencilla.Core.Serialization.Json;

// Discovered like every other component: AddSencilla()'s scan force-loads the assembly so
// RepositoryRegistrator sees the Notification entities (repositories + DbContext model) and
// Sencilla.Web's EntityApiControllerFeatureProvider materialises the [CrudApi] controllers for
// NotificationInbox and NotificationRead — nothing for the host to register.
[assembly: AutoDiscovery]
