using System.Collections;
using System.Collections.Concurrent;

namespace Backend.Infrastructure;

public class ConcurrentHashSet<T> : IReadOnlyCollection<T>
    where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dict = new();

    public bool Add(T item) => _dict.TryAdd(item, 0);

    public bool Remove(T item) => _dict.TryRemove(item, out _);

    public bool Contains(T item) => _dict.ContainsKey(item);

    public void Clear() => _dict.Clear();

    public IEnumerator<T> GetEnumerator() => _dict.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _dict.Count;
    public bool IsEmpty => _dict.IsEmpty;
    public IEnumerable<T> Items => _dict.Keys;
}
