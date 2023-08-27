using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace Nrrdio.MapGenerator.Services;
public class CanvasWrapper : ICanvasWrapper {
    public UIElementCollection Children => XamlCanvas.Children;
    public int Height { get; set; }
    public int Width { get; set; }

    ILogger<CanvasWrapper> Log { get; }
    Canvas XamlCanvas { get; set; }

    bool Initialized = false;

#pragma warning disable CS8618 // XamlCanvas is null
    public CanvasWrapper(
        ILogger<CanvasWrapper> log
    ) {
        Log = log;
    }
#pragma warning restore CS8618

    public void Initialize(Canvas outputCanvas) {
        Log.LogTrace("CanvasWrapper initialized");

        XamlCanvas = outputCanvas;
        Height = (int)outputCanvas.Height;
        Width = (int)outputCanvas.Width;

        Initialized = true;
    }
}
