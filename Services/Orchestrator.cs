using Microsoft.Extensions.Logging;
using Nrrdio.MapGenerator.Services.Models;
using Nrrdio.Utilities.Maths;

namespace Nrrdio.MapGenerator.Services;

public class Orchestrator {
    public bool Continue { get; set; }
    public int Seed {
        get => _Seed;
        set {
            _Seed = value;
            Random = new Random(_Seed);
        }
    }
    int _Seed;

    Random Random { get; set; } = new();

    ICanvasWrapper Canvas { get; }
    ILogger<Orchestrator> Log { get; }
    VoronoiGenerator VoronoiGenerator { get; }

    public Orchestrator(
        ICanvasWrapper canvas,
        ILogger<Orchestrator> log,
        VoronoiGenerator voronoiGenerator
    ) {
        Canvas = canvas;
        Log = log;
        VoronoiGenerator = voronoiGenerator;
        Seed = Random.Next();

        VoronoiGenerator.Random = Random;
        VoronoiGenerator.Seed = Seed;
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

        var polygons = await VoronoiGenerator.Generate(5, borderVertices);
        var nestedPolygons = new List<MapPolygon>();

        foreach (var polygon in polygons.ToList()) {
            var result = await VoronoiGenerator.Generate(5, polygon.Vertices.Cast<MapPoint>());
            nestedPolygons.AddRange(result);
        }

        Log.LogInformation("Done. Redraw to continue.");
    }
}
