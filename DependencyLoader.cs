using System;
using System.IO;
using System.Linq;
using System.Reflection;
#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif

namespace AmberBim
{
    internal static class DependencyLoader
    {
        private static readonly object SyncRoot = new object();
        private static readonly Assembly AddInAssembly = typeof(DependencyLoader).Assembly;
        private static readonly string DependencyDirectory = Path.Combine(
            Path.GetDirectoryName(AddInAssembly.Location), "AmberBIM");
        private static bool registered;

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                if (registered || !Directory.Exists(DependencyDirectory))
                    return;

                AppDomain.CurrentDomain.AssemblyResolve += ResolveDependency;
                registered = true;
            }
        }

        internal static void Shutdown()
        {
            lock (SyncRoot)
            {
                if (!registered)
                    return;

                AppDomain.CurrentDomain.AssemblyResolve -= ResolveDependency;
                registered = false;
            }
        }

        private static Assembly ResolveDependency(object sender, ResolveEventArgs args)
        {
            var requester = args.RequestingAssembly;
            if (requester != null && requester != AddInAssembly &&
                (requester.IsDynamic || string.IsNullOrEmpty(requester.Location) ||
                 !requester.Location.StartsWith(DependencyDirectory + Path.DirectorySeparatorChar,
                     StringComparison.OrdinalIgnoreCase)))
                return null;

            var requested = new AssemblyName(args.Name);
            if (string.IsNullOrEmpty(requested.Name) || requested.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return null;

            var directory = DependencyDirectory;
            if (!string.IsNullOrEmpty(requested.CultureName))
                directory = Path.Combine(directory, System.Globalization.CultureInfo.GetCultureInfo(requested.CultureName).Name);

            var path = Path.Combine(directory, requested.Name + ".dll");
            if (!File.Exists(path))
                return null;

            var candidate = AssemblyName.GetAssemblyName(path);
            if (!AssemblyName.ReferenceMatchesDefinition(requested, candidate) ||
                (requested.Version != null && requested.Version != candidate.Version) ||
                !(requested.GetPublicKeyToken() ?? new byte[0]).SequenceEqual(candidate.GetPublicKeyToken() ?? new byte[0]))
                return null;

#if NETFRAMEWORK
            return Assembly.LoadFrom(path);
#else
            return AssemblyLoadContext.GetLoadContext(requester ?? AddInAssembly).LoadFromAssemblyPath(path);
#endif
        }
    }
}
