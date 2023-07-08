using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services;
using Nrrdio.MapGenerator.Services.Models;

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

        var borderVertices = new[] {
            new MapPoint(0, 0),
            new MapPoint(0, CanvasHeight),
            new MapPoint(CanvasWidth, CanvasHeight),
            new MapPoint(CanvasWidth, 0)
        };

        Generator.Initialize(OutputCanvas);

        var polygons = await Generator.GenerateWithReturn(30, borderVertices);

        //foreach (var polygon in polygons) {
        //    await Generator.Generate(10, polygon.Vertices.Cast<MapPoint>());
        //}
    }

    public void Continue() => Generator.Continue = true;
}