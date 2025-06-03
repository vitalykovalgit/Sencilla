
namespace Android.App
{
    /// <summary>
    /// Interface for all services 
    /// for android app
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Create and initilalize service
        /// </summary>
        void Create();

        /// <summary>
        /// Start service
        /// </summary>
        void Start();

        /// <summary>
        /// Stop service
        /// </summary>
        void Stop();

        /// <summary>
        /// Destroy service
        /// </summary>
        void Destroy();
    }
}