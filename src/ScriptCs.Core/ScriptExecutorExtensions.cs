using System;
using System.Collections.Generic;
using System.Linq;

using ScriptCs.Contracts;

namespace ScriptCs
{
    public static class ScriptExecutorExtensions
    {
        public static void ImportNamespaces(this IScriptExecutor executor, params Type[] types)
        {
            executor.ImportNamespaces(types.AsEnumerable());
        }

        public static void ImportNamespaces(this IScriptExecutor executor, IEnumerable<Type> types)
        {
            executor.ImportNamespaces(types.Select(t => t.Namespace));
        }

        public static void ImportNamespaces(this IScriptExecutor executor, params string[] namespaces)
        {
            executor.ImportNamespaces(namespaces.AsEnumerable());
        }

        public static void ImportNamespace<T>(this IScriptExecutor executor)
        {
            executor.ImportNamespaces(typeof(T));
        }

        public static void RemoveNamespaces(this IScriptExecutor executor, params string[] namespaces)
        {
            executor.RemoveNamespaces(namespaces.AsEnumerable());
        }

        public static void AddReferences(this IScriptExecutor executor, params Type[] types)
        {
            executor.AddReferences(types.AsEnumerable());
        }

        public static void AddReferences(this IScriptExecutor executor, IEnumerable<Type> types)
        {
            executor.AddReferences(types.Select(t => t.Assembly.Location));
        }

        public static void AddReferences(this IScriptExecutor executor, params string[] references)
        {
            executor.AddReferences(references.AsEnumerable());
        }

        public static void AddReference<T>(this IScriptExecutor executor)
        {
            executor.AddReferences(typeof(T));
        }

        public static void RemoveReferences(this IScriptExecutor executor, params string[] references)
        {
            executor.RemoveReferences(references.AsEnumerable());
        }

        public static void AddReferenceAndImportNamespaces(this IScriptExecutor executor, params Type[] types)
        {
            executor.AddReferences(types.AsEnumerable());
            executor.ImportNamespaces(types.AsEnumerable());
        }

        public static void AddReferenceAndImportNamespaces(this IScriptExecutor executor, IEnumerable<Type> types)
        {
            executor.AddReferences(types);
            executor.ImportNamespaces(types);
        }
    }
}
