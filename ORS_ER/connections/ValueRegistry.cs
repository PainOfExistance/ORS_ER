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

        private static readonly List<RegistryEntry> _globalRegistry = new();
        private static readonly Dictionary<string, List<RegistryEntry>> _localRegistry = new();

        public static List<RegistryEntry> GetGlobalRegistry()
        {
            return _globalRegistry;
        }
        public static List<RegistryEntry> GetLocalRegistry(string key)
        {
            return _localRegistry[key];
        }
        public static void ClearGlobalRegistry()
        {
            _globalRegistry.Clear();
        }
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

        public static void AddLocalRegistry(string registryId)
        {
            if (!_localRegistry.ContainsKey(registryId))
            {
                _localRegistry.Add(registryId, new List<RegistryEntry>());
            }
        }

        public static void RegisterLocalValue(string registryId, string key, RegistryEntry value)
        {
            if (_localRegistry.TryGetValue(registryId, out var registry))
            {
                for (int i = 0; i < registry.Count; i++)
                {
                    if (registry[i].Name == key)
                    {
                        registry[i] = value;
                        return;
                    }
                }
                registry.Add(value);
            }
        }

        public static RegistryEntry? GetLocalValue(string registryId, string key)
        {
            foreach (var entry in _globalRegistry)
            {
                if (entry.Name == key)
                {
                    return null;
                }
            }

            if (_localRegistry.TryGetValue(registryId, out var registry))
            {
                foreach (var entry in registry)
                {
                    if (entry.Name == key)
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        public static void DeleteLocalRegistry(string registryId)
        {
            if (_localRegistry.ContainsKey(registryId))
            {
                _localRegistry.Remove(registryId);
            }
        }
    }
}
