using System;
using System.IO;
using System.Reflection;

namespace libGenerator
{
    public class EmbeddedResources
    {
        private static readonly Lazy<EmbeddedResources> _callingResources = new Lazy<EmbeddedResources>(() => new EmbeddedResources(Assembly.GetCallingAssembly()));

        private static readonly Lazy<EmbeddedResources> _entryResources = new Lazy<EmbeddedResources>(() => new EmbeddedResources(Assembly.GetEntryAssembly()));

        private static readonly Lazy<EmbeddedResources> _executingResources = new Lazy<EmbeddedResources>(() => new EmbeddedResources(Assembly.GetExecutingAssembly()));

        private readonly Assembly _assembly;

        private readonly string[] _resources;

        public EmbeddedResources(Assembly assembly)
        {
            _assembly = assembly;
            _resources = assembly.GetManifestResourceNames();
        }

        public static EmbeddedResources CallingResources => _callingResources.Value;

        public static EmbeddedResources EntryResources => _entryResources.Value;

        public static EmbeddedResources ExecutingResources => _executingResources.Value;

        public Stream GetStream(string resName) //=> _assembly.GetManifestResourceStream(_resources.Single(s => s.Contains(resName)));
        {
            for(int i=0;i<_resources.Length;i++)
            {
                if (_resources[i].EndsWith(resName))
                    return _assembly.GetManifestResourceStream(_resources[i]);
            }
            return Stream.Null;
        }

    }
}
