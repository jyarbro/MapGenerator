using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services;
using Nrrdio.MapGenerator.Services.Models;
using System.Threading.Tasks;

namespace Nrrdio.MapGenerator.Client;
public class MainPageViewModel {
    public int CanvasWidth { get; private set; }
    public int CanvasHeight { get; private set; }

    DelaunayVoronoiGenerator DelaunayVoronoiGenerator { get; }
    ILogger<MainPageViewModel> Log { get; }

    Canvas OutputCanvas { get; set; }

    public MainPageViewModel(
        DelaunayVoronoiGenerator delaunayVoronoiGenerator,
        ILogger<MainPageViewModel> log
    ) {
        DelaunayVoronoiGenerator = delaunayVoronoiGenerator;
        Log = log;
    }

    public void SetCanvas(Canvas canvas) {
        OutputCanvas = canvas;
        CanvasHeight = (int)OutputCanvas.ActualHeight;
        CanvasWidth = (int)OutputCanvas.ActualWidth;
    }

    public async Task Start() {
        Log.LogTrace(nameof(Start));

        var border = new MapPolygon(new MapPoint(0, 0),
                                    new MapPoint(0, CanvasHeight),
                                    new MapPoint(CanvasWidth, CanvasHeight),
                                    new MapPoint(CanvasWidth, 0));

        await DelaunayVoronoiGenerator.Generate(30, border, OutputCanvas);
    }

    public void Continue() => DelaunayVoronoiGenerator.Continue = true;
}