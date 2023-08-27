using Microsoft.UI.Xaml.Controls;

namespace Nrrdio.MapGenerator.Services;
public interface ICanvasWrapper {
    UIElementCollection Children { get; }
    int Height { get; set; }
    int Width { get; set; }

    void Initialize(Canvas outputCanvas);
}