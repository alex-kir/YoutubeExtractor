using System;
using System.Linq;
using System.Reflection;

namespace Services
{
    class AssemblySmartResolver
    {
        private static Assembly _assembly;
        private static string _assemblyPrefixName;

        public static void Install(Assembly assembly)
        {
            _assembly = assembly;
            _assemblyPrefixName = assembly.FullName.Split(',')[0];
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var resolveName = args.Name.Split(',').FirstOrDefault();

            foreach (var name in _assembly.GetManifestResourceNames())
            {
                if (name.Substring(_assemblyPrefixName.Length).Contains(resolveName) &&
                    name.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                {
                    var stream = _assembly.GetManifestResourceStream(name);
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                    var tmp = Assembly.Load(bytes);
                    return tmp;
                }
            }

            return null;
        }

    }
}
