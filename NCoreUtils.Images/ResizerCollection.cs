using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images;

public class ResizerCollection : IReadOnlyDictionary<string, IResizerFactory>
{
    readonly Dictionary<string, IResizerFactory> _factories;

    public IResizerFactory this[string key] => _factories[key];

    public IEnumerable<string> Keys => _factories.Keys;

    public IEnumerable<IResizerFactory> Values => _factories.Values;

    public int Count => _factories.Count;

    public ResizerCollection(IReadOnlyDictionary<string, IResizerFactory> factories)
    {
        _factories = new Dictionary<string, IResizerFactory>();
        foreach (var kv in factories)
        {
            _factories.Add(kv.Key, kv.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool ContainsKey(string key)
        => _factories.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, IResizerFactory>> GetEnumerator()
        => _factories.GetEnumerator();

#if !NET6_0_OR_GREATER
    bool IReadOnlyDictionary<string, IResizerFactory>.TryGetValue(string key, out IResizerFactory value)
        => TryGetValue(key, out value!);
#endif

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out IResizerFactory value)
        => _factories.TryGetValue(key, out value);
}