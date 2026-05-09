using System.Runtime.CompilerServices;

namespace Aprillz.MewUI.Rendering;

public static class RenderDeviceCompatibilityExtensions
{
    private static readonly ConditionalWeakTable<IGraphicsFactory, IRenderDevice> Devices = new();

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
        if (factory is IRenderDevice renderDevice)
        {
            return renderDevice;
        }

        return Devices.GetValue(factory, static f => new GraphicsFactoryRenderDeviceAdapter(f));
    }
}
