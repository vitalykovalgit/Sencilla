namespace Sencilla.Core;

/// <summary>
/// TODO: To be rewieved
/// Base repository for all the repos
/// </summary>
public interface IBaseRepository
{
    /// <summary>
    /// Save changes
    /// </summary>
    /// <returns>Count of saved entities</returns>
    Task<int> Save(CancellationToken token = default);
}

