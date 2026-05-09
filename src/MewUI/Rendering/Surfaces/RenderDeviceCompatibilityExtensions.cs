namespace Aprillz.MewUI.Rendering;

public static class RenderDeviceCompatibilityExtensions
{
    /// <summary>
    /// Returns the render-device view owned by <paramref name="factory"/>.
    /// </summary>
    /// <remarks>
    /// The returned instance has borrowed lifetime. Callers should dispose surfaces,
    /// contexts, images, and cache entries they create, not the device returned here.
    /// </remarks>
    public static IRenderDevice AsRenderDevice(this IGraphicsFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory;
    }
}
