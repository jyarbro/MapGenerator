using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services;
using Nrrdio.MapGenerator.Services.Models;
using Nrrdio.Utilities.Maths;

namespace Nrrdio.MapGenerator.App.ViewModels;

public class MainPageViewModel : ObservableRecipient {
    public int CanvasWidth { get; private set; }
    public int CanvasHeight { get; private set; }

    IGenerator Generator { get; }
    ILogger<MainPageViewModel> Log { get; }

    Canvas? OutputCanvas { get; set; }

    public MainPageViewModel(
        IGenerator generator,
        ILogger<MainPageViewModel> log
    ) {
        Generator = generator;
        Log = log;
    }

    public void SetCanvas(Canvas canvas) {
        OutputCanvas = canvas;
        CanvasHeight = (int)OutputCanvas.ActualHeight;
        CanvasWidth = (int)OutputCanvas.ActualWidth;
    }

    public async Task Start() {
        Log.LogTrace(nameof(Start));

        Debug.Assert(OutputCanvas is not null);

        OutputCanvas.Children.Clear();

        await Task.Delay(10);

        var borderVertices = new List<MapPoint>();
        
        var borderPolygon = new Circle(new Point(CanvasWidth / 2, CanvasHeight / 2), 300).ToPolygon(5);

        foreach (var point in borderPolygon.Vertices) {
            borderVertices.Add(new MapPoint(point));
        }

        Generator.Initialize(OutputCanvas);

        var polygons = await Generator.GenerateWithReturn(5, borderVertices);
        var nestedPolygons = new List<MapPolygon>();

        foreach (var polygon in polygons.ToList()) {
            var result = await Generator.GenerateWithReturn(5, polygon.Vertices.Cast<MapPoint>());
            nestedPolygons.AddRange(result);
        }

        Log.LogInformation("Done. Redraw to continue.");
    }

    public void Continue() => Generator.Continue = true;
}