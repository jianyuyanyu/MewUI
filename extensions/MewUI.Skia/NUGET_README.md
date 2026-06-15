# Aprillz.MewUI.Skia

Skia rendering integration for [MewUI](https://github.com/aprillz/MewUI): a `SkiaCanvasView` control plus the
`ISkiaInteropProvider` registry. Ships a CPU-upload fallback that works on any backend; install a matching
interop package below for a zero-copy GPU path.

```csharp
var canvas = new SkiaCanvasView(surface => { /* draw with SkiaSharp */ });
```

## Zero-copy GPU bridges (interop packages)

Add the one matching your rendering backend, then activate it at startup:

| Package | Backend | Activate |
|---|---|---|
| `Aprillz.MewUI.Skia.Interop.Direct2D` | Direct2D | `SkiaDirect2DInterop.Use()` |
| `Aprillz.MewUI.Skia.Interop.Gdi` | GDI | `SkiaGdiInterop.Register()` |
| `Aprillz.MewUI.Skia.Interop.MewVG.Win32` | MewVG / Win32 | `SkiaMewVGWin32Interop.Use()` |
| `Aprillz.MewUI.Skia.Interop.MewVG.X11` | MewVG / X11 | `SkiaMewVGX11Interop.Use()` |
| `Aprillz.MewUI.Skia.Interop.MewVG.MacOS` | MewVG / macOS | `SkiaMewVGMacOSInterop.Use()` |

Without an interop package, Skia content still renders via the CPU upload fallback.

Repo: https://github.com/aprillz/MewUI
