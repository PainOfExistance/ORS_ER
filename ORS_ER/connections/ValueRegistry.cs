using System;
using System.Collections.Generic;
using System.Text;

namespace ORS_ER.connections
{
    public class ValueRegistry
    {
        public struct RegistryEntry
        {
            public string BlockId { get; set; }
            public string Name { get; set; }
            public dynamic Value { get; set; }
        }

        private List<RegistryEntry> _globalRegistry = new List<RegistryEntry>();
        private Dictionary<string, List<RegistryEntry>> _localRegistry = new Dictionary<string, List<RegistryEntry>>();

        public void RegisterGlobalValue(string key, RegistryEntry value)
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

        public RegistryEntry? GetGlobalValue(string key)
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

        public void AddLocalRegistry(string registryId)
        {
            if (!_localRegistry.ContainsKey(registryId))
            {
                _localRegistry.Add(registryId, new List<RegistryEntry>());
            }
        }

        public void RegisterLocalValue(string registryId, string key, RegistryEntry value)
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

        public RegistryEntry? GetLocalValue(string registryId, string key)
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

        public void DeleteLocalRegistry(string registryId)
        {
            if (_localRegistry.ContainsKey(registryId))
            {
                _localRegistry.Remove(registryId);
            }
        }
    }
}
