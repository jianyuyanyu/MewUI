namespace Aprillz.MewUI.Rendering;

public interface IRenderDevice : IDisposable
{
    GraphicsBackend Backend { get; }

    IRenderSurface CreateSurface(RenderSurfaceDescriptor descriptor);

    IGraphicsContext CreateContext(IRenderSurface surface);

    IImage CreateImageView(IRenderSurface surface);

    bool TryReadPixels(IRenderSurface source, Span<byte> destination, int destinationStrideBytes);

    IRenderOperation RequestReadback(IRenderSurface source);

    IRenderOperation FlushAsyncWork();

    IRenderResourceCache? ResourceCache { get; }
}
