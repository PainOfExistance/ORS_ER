using System;
using System.Collections.Generic;
using System.Text;

namespace ORS_ER.connections
{

    public static class ValueRegistry
    {
        public struct RegistryEntry
        {
            public string BlockId { get; set; }
            public string Name { get; set; }
            public dynamic Value { get; set; }
        }

        //todo finish this so it cascades values

        private static readonly List<RegistryEntry> _globalRegistry = new();
        private static readonly Dictionary<string, (string, List<RegistryEntry>)> _localRegistry = new();

        public static void RegisterGlobalValue(string key, RegistryEntry value)
        {
            for (int i = 0; i < _globalRegistry.Count; i++)
            {
                if (_globalRegistry[i].Name == key)
                {
                    _globalRegistry[i] = value;
                    return;
                }
            }
            _globalRegistry.Add(value);
        }

        public static RegistryEntry? GetGlobalValue(string key)
        {
            foreach (var entry in _globalRegistry)
            {
                if (entry.Name == key)
                {
                    return entry;
                }
            }
            return null;
        }

        public static void AddLocalRegistry(string registryId, string parrent)
        {
            if (!_localRegistry.ContainsKey(registryId))
            {
                _localRegistry.Add(registryId, (parrent, new List<RegistryEntry>()));
            }
        }

        public static void RegisterLocalValue(string registryId, string key, RegistryEntry value)
        {
            if (registryId == "")
            {
                RegisterGlobalValue(key, value);
            }
            else if (GetLocalValue(registryId, key) == null)
            {
                _localRegistry[registryId].Item2.Add(value);
            }
            else
            {
                if (_localRegistry.TryGetValue(registryId, out var registry))
                {
                    var entries = registry.Item2;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (entries[i].Name == key)
                        {
                            entries[i] = value;
                            _localRegistry[registryId] = (registry.Item1, entries);
                            return;
                        }
                    }
                }
                else
                {
                    var currentRegistryId = _localRegistry[registryId].Item1;
                    while (currentRegistryId != "")
                    {
                        if (_localRegistry.TryGetValue(currentRegistryId, out var reg))
                        {
                            var entries = reg.Item2;
                            for (int i = 0; i < entries.Count; i++)
                            {
                                if (entries[i].Name == key)
                                {
                                    entries[i] = value;
                                    _localRegistry[registryId] = (reg.Item1, entries);
                                    return;
                                }
                            }
                        }

                        currentRegistryId = _localRegistry[currentRegistryId].Item1;
                    }
                }

                for (int i = 0; i < _globalRegistry.Count; i++)
                {
                    if (_globalRegistry[i].Name == key)
                    {
                        _globalRegistry[i] = value;
                        return;
                    }
                }
            }
        }

        public static RegistryEntry? GetLocalValue(string registryId, string key)
        {
            if (_localRegistry.TryGetValue(registryId, out var registry))
            {
                foreach (var entry in registry.Item2)
                {
                    if (entry.Name == key)
                    {
                        return entry;
                    }
                }
            }

            if (_localRegistry.TryGetValue(registryId, out var currentRegistryId))
            {
                while (currentRegistryId.Item1 != null)
                {
                    if (_localRegistry.TryGetValue(currentRegistryId.Item1, out var reg))
                    {
                        foreach (var entry in reg.Item2)
                        {
                            if (entry.Name == key)
                            {
                                return entry;
                            }
                        }
                    }
                    _localRegistry.TryGetValue(currentRegistryId.Item1, out currentRegistryId);
                }
            }

            foreach (var entry in _globalRegistry)
            {
                if (entry.Name == key)
                {
                    return entry;
                }
            }

            return null;
        }

        public static void ClearAllRegistries()
        {
            _globalRegistry.Clear();
            _localRegistry.Clear();
        }
    }
}
