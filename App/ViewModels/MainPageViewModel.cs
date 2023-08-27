using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Nrrdio.MapGenerator.Services;
using Nrrdio.MapGenerator.Services.Models;
using Nrrdio.Utilities.Maths;

namespace Nrrdio.MapGenerator.App.ViewModels;

public class MainPageViewModel : ObservableRecipient {
    IGenerator Generator { get; }
    ICanvasWrapper Canvas { get; set; }
    ILogger<MainPageViewModel> Log { get; }

    public MainPageViewModel(
        IGenerator generator,
        ICanvasWrapper canvas,
        ILogger<MainPageViewModel> log
    ) {
        Generator = generator;
        Canvas = canvas;
        Log = log;
    }

    public async Task Start() {
        Log.LogTrace(nameof(Start));

        Canvas.Children.Clear();

        await Task.Delay(10);

        var borderVertices = new List<MapPoint>();

        var borderPolygon = new Circle(new Point(Canvas.Width / 2, Canvas.Height / 2), 300).ToPolygon(6);

        foreach (var point in borderPolygon.Vertices) {
            borderVertices.Add(new MapPoint(point));
        }

        var polygons = await Generator.Generate(5, borderVertices);
        var nestedPolygons = new List<MapPolygon>();

        foreach (var polygon in polygons.ToList()) {
            var result = await Generator.Generate(5, polygon.Vertices.Cast<MapPoint>());
            nestedPolygons.AddRange(result);
        }

        Log.LogInformation("Done. Redraw to continue.");
    }

    public void Continue() => Generator.Continue = true;
}