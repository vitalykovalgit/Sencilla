
namespace Microsoft.Extensions.DependencyInjection
{
    public static class Bootstrap
    {
        /// <summary>
        /// This method is used in startup class just to reference 
        /// this assambly with all it's component 
        /// Sencilla will register everything automatically
        /// </summary>
        public static IServiceCollection AddUsers(this IServiceCollection builder)
        {
            // Do nothing here
            return builder;
        }
    }
}
