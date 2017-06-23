﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Eventus.Events;

namespace Eventus
{
    internal static class ReflectionHelper
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, string>> AggregateEventHandlerCache =
                new ConcurrentDictionary<Type, ConcurrentDictionary<Type, string>>();

        public static Dictionary<Type, string> FindEventHandlerMethodsInAggregate(Type aggregateType)
        {
            if (AggregateEventHandlerCache.ContainsKey(aggregateType) == false)
            {
                var eventHandlers = new ConcurrentDictionary<Type, string>();

                var methods = aggregateType.GetMethodsBySig(typeof(void), true, typeof(IEvent)).ToList();

                if (methods.Any())
                {
                    foreach (var m in methods)
                    {
                        var parameter = m.GetParameters().First();
                        if (eventHandlers.TryAdd(parameter.ParameterType, m.Name) == false)
                        {
                            throw new TargetException($"Multiple methods found handling same event in {aggregateType.Name}");
                        }
                    }
                }

                if (AggregateEventHandlerCache.TryAdd(aggregateType, eventHandlers) == false)
                {
                    throw new TargetException($"Error registering methods for handling events in {aggregateType.Name}");
                }
            }


            return AggregateEventHandlerCache[aggregateType].ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static MethodInfo GetMethod(Type t, string methodName, Type[] paramTypes)
        {
            return t.GetRuntimeMethod(methodName, paramTypes);
        }

        public static IEnumerable<MethodInfo> GetMethodsBySig(this Type type,
            Type returnType,
            bool matchParameterInheritance,
            params Type[] parameterTypes)
        {

            return type.GetRuntimeMethods().Where(m =>
            {
                //ignore properties
                if (m.Name.StartsWith("get_", StringComparison.InvariantCultureIgnoreCase) ||
                    m.Name.StartsWith("set_", StringComparison.InvariantCultureIgnoreCase))
                    return false;

                //does the return type match
                if (m.ReturnType != returnType)
                    return false;

                //does the method have the same number of parameters that are either the same type or assignable from the passed in parameter types 
                //based on the matchParameterInheritance switch
                var parameters = m.GetParameters();

                if ((parameterTypes == null || parameterTypes.Length == 0))
                    return parameters.Length == 0;

                if (parameters.Length != parameterTypes.Length)
                    return false;

                return !parameterTypes.Where((t, i) => (parameters[i].ParameterType == t || matchParameterInheritance && t.IsAssignableFrom(parameters[i].ParameterType)) == false).Any();
            });
        }
    }
}