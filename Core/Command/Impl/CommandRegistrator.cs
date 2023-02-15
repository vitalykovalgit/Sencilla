namespace Sencilla.Core.Impl
{
    public class CommandRegistrator : ITypeRegistrator
    {
        /// <summary>
        /// Register command hadlers automatically 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="type"></param>
        public void Register(IContainer container, Type type)
        {
            if (type.IsClass && !type.IsAbstract && !type.IsGenericType)
            {
                var cmdHandlerInterfaces = type.GetInterfaces().Where(i => IsInterfaceCommand(i));
                foreach (var cmdInterface in cmdHandlerInterfaces)
                {
                    container.RegisterType(cmdInterface, type);
                }
            }
            
        }

        protected bool IsInterfaceCommand(Type type)
        {
            return  type.IsGenericType
                && (type.GetGenericTypeDefinition() == typeof(ICommandHandlerBase<>)
                ||  type.GetGenericTypeDefinition() == typeof(ICommandHandlerBase<,>));
        }
    }
}
