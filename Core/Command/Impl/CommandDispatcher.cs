
using System.Reflection;

namespace Sencilla.Core
{
    /// <summary>
    /// Dispatch command 
    /// </summary>
    [Implement(typeof(ICommandDispatcher))]
    public class CommandDispatcher: Resolveable, ICommandDispatcher
    {
        public CommandDispatcher(IResolver resolver): base(resolver)
        {
        }

        /// <summary>
        /// Resolve handler for <see cref="ICommandHandler"/> and process command 
        /// </summary>
        /// <inheritdoc/>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task SendAsync(ICommand command)
        {
            var handler = R(typeof(ICommandHandlerBase<>).MakeGenericType(command.GetType()));

            // get method and inject parameters but skip first parameter 
            var method = handler.GetType().GetMethod(nameof(ICommandHandler<ICommand>.HandleAsync));
            var parameters = InjectParameters(method, command);

            await (Task)method.Invoke(handler, parameters.ToArray());
        }

        /// <summary>
        /// Resolve handler <see cref="ICommandHandler<>"/> that can return response
        /// and process command 
        /// </summary>
        /// <inheritdoc/>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        {
            var handler = R(typeof(ICommandHandlerBase<,>).MakeGenericType(command.GetType(), typeof(TResponse)));
            
            var method = handler.GetType().GetMethod(nameof(ICommandHandler<ICommand>.HandleAsync));
            var parameters = InjectParameters(method, command);

            var response = await (Task<TResponse>)method.Invoke(handler, parameters.ToArray());
            //return await handler.HandleAsync((dynamic)command);
            return response;
        }

        protected List<object> InjectParameters(MethodInfo method, ICommand command)
        {
            // inject parameters but skip first parameter 
            var parameters = new List<object>() { command };
            var parameterTypes = method.GetParameters();
            for (var i = 1; i < parameterTypes.Length; i++)
            {
                var service = R(parameterTypes[i].ParameterType);
                parameters.Add(service);
            }
            return parameters;
        }
    }
}
