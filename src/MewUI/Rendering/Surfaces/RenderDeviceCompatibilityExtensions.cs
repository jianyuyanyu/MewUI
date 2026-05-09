namespace Aprillz.MewUI.Rendering;

public static class RenderDeviceCompatibilityExtensions
{
    public static IRenderDevice AsRenderDevice(this IGraphicsFactory factory)
        => new GraphicsFactoryRenderDeviceAdapter(factory);
}
