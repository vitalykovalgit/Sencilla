namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// The provider configuration AddSencillaRepositoryForEF was given, kept so a context registered later —
/// pinned with [DbContext&lt;T&gt;] on an entity another scan discovers — builds its options from the same
/// provider. See <c>RegisterPinnedEFContext</c>.
/// </summary>
[DisableInjection]
public sealed record EfContextConfigure(Action<DbContextOptionsBuilder> Configure);
