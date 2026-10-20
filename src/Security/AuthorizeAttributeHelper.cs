using BlackDigital.Domain.Security;
using System.Collections.Concurrent;
using System.Reflection;

namespace BlackDigital.AspNet.Security
{
    /// <summary>
    /// Resolves the <see cref="AuthorizeAttribute"/> requirements that apply to a
    /// REST service action. The attribute is declared on the service
    /// <b>implementation</b> class (and/or on its methods) rather than on the
    /// interface, so this helper bridges an interface method to the matching
    /// implementation method and collects every requirement declared on the way.
    /// </summary>
    /// <remarks>
    /// Requirements declared on the implementing class and on the implementing
    /// method are both returned. Callers are expected to grant access when
    /// <b>any</b> of the returned requirements is satisfied.
    /// </remarks>
    public static class AuthorizeAttributeHelper
    {
        private static readonly ConcurrentDictionary<(Type, MethodInfo), IReadOnlyList<AuthorizeAttribute>> _cache = new();

        /// <summary>
        /// Gets every <see cref="AuthorizeAttribute"/> declared on the given
        /// implementation class and on the implementation of the given interface
        /// method. The result is cached per implementation type/method pair.
        /// </summary>
        /// <param name="implementationType">The concrete service implementation type.</param>
        /// <param name="interfaceMethod">The interface method exposed as a REST action.</param>
        /// <returns>
        /// A read-only list with all declared requirements; empty when the action
        /// is public (no authorization declared).
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        public static IReadOnlyList<AuthorizeAttribute> GetRequirements(Type implementationType, MethodInfo interfaceMethod)
        {
            ArgumentNullException.ThrowIfNull(implementationType);
            ArgumentNullException.ThrowIfNull(interfaceMethod);

            return _cache.GetOrAdd((implementationType, interfaceMethod), static key =>
            {
                var (type, method) = key;
                var requirements = new List<AuthorizeAttribute>();

                // Class-level declarations apply to every action of the service.
                requirements.AddRange(type.GetCustomAttributes<AuthorizeAttribute>(inherit: true));

                // Method-level declarations apply only to the matching action.
                var implementationMethod = FindImplementationMethod(type, method);
                if (implementationMethod != null)
                    requirements.AddRange(implementationMethod.GetCustomAttributes<AuthorizeAttribute>(inherit: true));

                return requirements;
            });
        }

        /// <summary>
        /// Determines whether the given interface method requires authorization
        /// when handled by the given implementation type. Used by the OpenAPI
        /// document builder, which only has access to the service interface.
        /// </summary>
        /// <param name="implementationType">The concrete service implementation type, or null when unknown.</param>
        /// <param name="interfaceMethod">The interface method exposed as a REST action.</param>
        /// <returns><c>true</c> when at least one requirement is declared; otherwise, <c>false</c>.</returns>
        public static bool HasRequirements(Type? implementationType, MethodInfo interfaceMethod)
            => implementationType != null && GetRequirements(implementationType, interfaceMethod).Count > 0;

        /// <summary>
        /// Finds the implementation method that satisfies the given interface
        /// method, using the type's interface map when available and falling back
        /// to a name/parameter-type match.
        /// </summary>
        /// <param name="implementationType">The concrete service implementation type.</param>
        /// <param name="interfaceMethod">The interface method to resolve.</param>
        /// <returns>The implementation method, or null when it cannot be resolved.</returns>
        public static MethodInfo? FindImplementationMethod(Type implementationType, MethodInfo interfaceMethod)
        {
            var interfaceType = interfaceMethod.DeclaringType;
            if (interfaceType is { IsInterface: true } && interfaceType.IsAssignableFrom(implementationType))
            {
                var map = implementationType.GetInterfaceMap(interfaceType);
                for (int i = 0; i < map.InterfaceMethods.Length; i++)
                {
                    if (map.InterfaceMethods[i] == interfaceMethod)
                        return map.TargetMethods[i];
                }
            }

            // Fallback for re-implemented or non-mapped members.
            var interfaceParameters = interfaceMethod.GetParameters();
            return implementationType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(candidate =>
                {
                    if (candidate.Name != interfaceMethod.Name)
                        return false;

                    var candidateParameters = candidate.GetParameters();
                    if (candidateParameters.Length != interfaceParameters.Length)
                        return false;

                    for (int i = 0; i < candidateParameters.Length; i++)
                    {
                        if (candidateParameters[i].ParameterType != interfaceParameters[i].ParameterType)
                            return false;
                    }

                    return true;
                });
        }

        /// <summary>
        /// Finds the concrete implementation type of a service interface among
        /// the given candidate types.
        /// </summary>
        /// <param name="serviceInterface">The service interface declared with <c>[Service]</c>.</param>
        /// <param name="candidateTypes">The types to search (typically the scanned assembly types).</param>
        /// <returns>The first concrete implementation found, or null when none is available.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        public static Type? FindImplementation(Type serviceInterface, IEnumerable<Type> candidateTypes)
        {
            ArgumentNullException.ThrowIfNull(serviceInterface);
            ArgumentNullException.ThrowIfNull(candidateTypes);

            return candidateTypes.FirstOrDefault(type =>
                type is { IsClass: true, IsAbstract: false }
                && serviceInterface.IsAssignableFrom(type));
        }
    }
}
