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

        //var borderVertices = new[] {
        //    new MapPoint(50, 25),
        //    new MapPoint(35, 100),
        //    new MapPoint(67, 250),
        //    new MapPoint(90, 220),
        //    new MapPoint(200, 30),
        //    new MapPoint(180, 0)
        //};

        Generator.Initialize(OutputCanvas);

        var polygons = await Generator.GenerateWithReturn(5, borderVertices);

        foreach (var polygon in polygons.ToList()) {
            await Generator.Generate(5, polygon.Vertices.Cast<MapPoint>());
        }
    }

    public void Continue() => Generator.Continue = true;
}