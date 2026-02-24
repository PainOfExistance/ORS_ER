using System;
using System.Collections.Generic;
using System.Text;

namespace ORS_ER.connections
{

    public readonly record struct RegistryId(string Value)
    {
        public static readonly RegistryId Global = new("");

        public bool IsGlobal => string.IsNullOrEmpty(Value);

        public override string ToString() => Value;

        public static implicit operator RegistryId(string value) => new(value ?? "");

        public static implicit operator string(RegistryId id) => id.Value;
    }

    public readonly record struct RegistryKey
    {
        public string Value { get; }

        public RegistryKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Registry key cannot be null or whitespace.", nameof(value));
            }
            Value = value;
        }

        public override string ToString() => Value;
    }

    public static class ValueRegistry
    {
        public struct RegistryEntry
        {
            public RegistryId ScopeId { get; set; }
            public RegistryId BlockId { get; set; }
            public RegistryKey Key { get; set; }

            public string Name
            {
                get => Key.Value;
                set => Key = new RegistryKey(value);
            }

            public string BlockIdString
            {
                get => BlockId.Value;
                set => BlockId = new RegistryId(value);
            }

            public dynamic Value { get; set; }
        }

        private static readonly List<RegistryEntry> _globalRegistry = new();
        private static readonly Dictionary<string, (string, List<RegistryEntry>)> _localRegistry = new();

        public static void RegisterGlobalValue(RegistryKey key, RegistryEntry value)
        {
            for (int i = 0; i < _globalRegistry.Count; i++)
            {
                if (_globalRegistry[i].Key.Equals(key))
                {
                    _globalRegistry[i] = value;
                    return;
                }
            }
            _globalRegistry.Add(value);
        }

        public static void RegisterGlobalValue(string key, RegistryEntry value) => RegisterGlobalValue(new RegistryKey(key), value);

        public static void AddLocalRegistry(RegistryId registryId, RegistryId parrent)
        {
            if (!_localRegistry.ContainsKey(registryId.Value))
            {
                _localRegistry.Add(registryId.Value, (parrent.Value, new List<RegistryEntry>()));
            }
        }

        public static void AddLocalRegistry(string registryId, string parrent) => AddLocalRegistry(new RegistryId(registryId), new RegistryId(parrent));

        public static void RegisterLocalValue(RegistryId registryId, RegistryKey key, RegistryEntry value)
        {
            if (registryId.IsGlobal)
            {
                RegisterGlobalValue(key, value);
            }
            else if (GetLocalValue(registryId, key) == null)
            {
                _localRegistry[registryId.Value].Item2.Add(value);
            }
            else
            {
                // Update in the current scope if present; otherwise walk parent scopes to replace.
                if (_localRegistry.TryGetValue(registryId.Value, out var registry))
                {
                    var entries = registry.Item2;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (entries[i].Key.Equals(key))
                        {
                            entries[i] = value;
                            _localRegistry[registryId.Value] = (registry.Item1, entries);
                            return;
                        }
                    }
                }
                else
                {
                    var currentRegistryId = _localRegistry[registryId.Value].Item1;
                    while (currentRegistryId != "")
                    {
                        if (_localRegistry.TryGetValue(currentRegistryId, out var reg))
                        {
                            var entries = reg.Item2;
                            for (int i = 0; i < entries.Count; i++)
                            {
                                if (entries[i].Key.Equals(key))
                                {
                                    entries[i] = value;
                                    _localRegistry[registryId.Value] = (reg.Item1, entries);
                                    return;
                                }
                            }
                        }

                        currentRegistryId = _localRegistry[currentRegistryId].Item1;
                    }
                }

                // Fall back to updating the global registry if no local scope contains the key.
                for (int i = 0; i < _globalRegistry.Count; i++)
                {
                    if (_globalRegistry[i].Key.Equals(key))
                    {
                        _globalRegistry[i] = value;
                        return;
                    }
                }
            }
        }

        public static void RegisterLocalValue(string registryId, string key, RegistryEntry value) =>
            RegisterLocalValue(new RegistryId(registryId), new RegistryKey(key), value);

        public static RegistryEntry? GetLocalValue(RegistryId registryId, RegistryKey key)
        {
            // Lookup order: current scope -> parent scopes -> global registry.
            if (_localRegistry.TryGetValue(registryId.Value, out var registry))
            {
                foreach (var entry in registry.Item2)
                {
                    if (entry.Key.Equals(key))
                    {
                        return entry;
                    }
                }
            }

            if (_localRegistry.TryGetValue(registryId.Value, out var currentRegistryId))
            {
                while (currentRegistryId.Item1 != null)
                {
                    if (_localRegistry.TryGetValue(currentRegistryId.Item1, out var reg))
                    {
                        foreach (var entry in reg.Item2)
                        {
                            if (entry.Key.Equals(key))
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
                if (entry.Key.Equals(key))
                {
                    return entry;
                }
            }

            return null;
        }

        public static RegistryEntry? GetLocalValue(string registryId, string key) =>
            GetLocalValue(new RegistryId(registryId), new RegistryKey(key));

        public static void ClearAllRegistries()
        {
            _globalRegistry.Clear();
            _localRegistry.Clear();
        }
    }
}
