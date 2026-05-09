namespace Aprillz.MewUI.Rendering;

public sealed class RenderResourceCache : IRenderResourceCache, IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<RenderCacheKey, RenderCacheEntry> _entries = new();
    private readonly List<PendingRelease> _pendingReleases = new();
    private bool _disposed;

    public bool TryGet(RenderCacheKey key, out IRenderCacheEntry entry)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            DrainCompletedReleases_NoLock();
            if (_entries.TryGetValue(key, out var cached))
            {
                entry = cached;
                return true;
            }
        }

        entry = null!;
        return false;
    }

    public IRenderCacheEntry Add(
        RenderCacheKey key,
        IRenderSurface surface,
        IImage image,
        IRenderOperation? safeToDisposeAfter = null)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(image);

        lock (_gate)
        {
            ThrowIfDisposed();
            DrainCompletedReleases_NoLock();

            if (_entries.Remove(key, out var existing))
            {
                ReleaseEntry_NoLock(existing);
            }

            var entry = new RenderCacheEntry(key, surface, image, safeToDisposeAfter);
            _entries.Add(key, entry);
            return entry;
        }
    }

    public void Release(RenderCacheKey key)
    {
        lock (_gate)
        {
            if (_disposed) return;
            if (_entries.Remove(key, out var existing))
            {
                ReleaseEntry_NoLock(existing);
            }
        }
    }

    public void ReleaseLater(IDisposable resource, IRenderOperation safeAfter)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(safeAfter);

        lock (_gate)
        {
            if (_disposed)
            {
                resource.Dispose();
                safeAfter.Dispose();
                return;
            }

            if (safeAfter.IsCompleted)
            {
                resource.Dispose();
                safeAfter.Dispose();
            }
            else
            {
                _pendingReleases.Add(new PendingRelease(resource, safeAfter));
            }
        }
    }

    public void Trim(RenderCacheTrimReason reason)
    {
        lock (_gate)
        {
            if (_disposed) return;
            foreach (var entry in _entries.Values)
            {
                ReleaseEntry_NoLock(entry);
            }

            _entries.Clear();
            DrainCompletedReleases_NoLock();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var entry in _entries.Values)
            {
                entry.Dispose();
            }

            _entries.Clear();

            foreach (var pending in _pendingReleases)
            {
                pending.Resource.Dispose();
                pending.Operation.Dispose();
            }

            _pendingReleases.Clear();
        }
    }

    private void ReleaseEntry_NoLock(RenderCacheEntry entry)
    {
        if (entry.SafeToDisposeAfter is { } operation && !operation.IsCompleted)
        {
            _pendingReleases.Add(new PendingRelease(entry, operation));
            return;
        }

        entry.Dispose();
    }

    private void DrainCompletedReleases_NoLock()
    {
        for (int i = _pendingReleases.Count - 1; i >= 0; i--)
        {
            var pending = _pendingReleases[i];
            if (!pending.Operation.IsCompleted)
            {
                continue;
            }

            _pendingReleases.RemoveAt(i);
            pending.Resource.Dispose();
            pending.Operation.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed class RenderCacheEntry : IRenderCacheEntry
    {
        private bool _disposed;

        public RenderCacheEntry(RenderCacheKey key, IRenderSurface surface, IImage image, IRenderOperation? safeToDisposeAfter)
        {
            Key = key;
            Surface = surface;
            Image = image;
            SafeToDisposeAfter = safeToDisposeAfter;
        }

        public RenderCacheKey Key { get; }

        public IRenderSurface Surface { get; }

        public IImage Image { get; }

        public IRenderOperation? SafeToDisposeAfter { get; }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Image.Dispose();
            Surface.Dispose();
            SafeToDisposeAfter?.Dispose();
        }
    }

    private readonly record struct PendingRelease(IDisposable Resource, IRenderOperation Operation);
}
