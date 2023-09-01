using Microsoft.Extensions.Logging;
using Nrrdio.MapGenerator.Services.Models;
using Nrrdio.Utilities;
using Nrrdio.Utilities.Maths;

namespace Nrrdio.MapGenerator.Services;

public class Orchestrator {
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
    Wait Wait { get; }
    VoronoiTesselator VoronoiTesselator { get; }
    Relaxer Relaxer { get; }

    public Orchestrator(
        ICanvasWrapper canvas,
        ILogger<Orchestrator> log,
        Wait wait,
        VoronoiTesselator voronoiTesselator,
        Relaxer relaxer
    ) {
        Canvas = canvas;
        Log = log;
        Wait = wait;
        VoronoiTesselator = voronoiTesselator;
        Relaxer = relaxer;
        Seed = Random.Next();

        VoronoiTesselator.Random = Random;
        VoronoiTesselator.Seed = Seed;
    }

    public async Task Start() {
        Log.LogTrace(nameof(Start));

        var debug = false;

        if (debug) {
            Log.LogInformation($"Seed: {Seed}");
            Log.LogInformation($"Move to other monitor now.");

            await Wait.ForContinue();
        }

        Canvas.Children.Clear();

        // Make sure the canvas updates
        await Wait.For(1);

        var borderVertices = new List<MapPoint>();

        var borderPolygon = new Circle(new Point(Canvas.Width / 2, Canvas.Height / 2), 300).ToPolygon(6);

        foreach (var point in borderPolygon.Vertices) {
            borderVertices.Add(new MapPoint(point));
        }

        var border = new MapPolygon(borderVertices);

        if (border.Winding == Polygon.EWinding.CLOCKWISE) {
            borderVertices.Reverse();
            border = new MapPolygon(borderVertices);
        }

        Canvas.Children.Add(border.CanvasPath);

        foreach (var segment in border.MapSegments) {
            Canvas.Children.Add(segment.CanvasPath);
        }

        var polygons = await VoronoiTesselator.Start(100, border);

        //var nestedPolygons = new List<MapPolygon>();

        //foreach (var polygon in polygons.ToList()) {
        //    var result = await VoronoiTesselator.Start(4, polygon);
        //    nestedPolygons.AddRange(result);
        //}

        var segments = polygons.SelectMany(o => o.MapSegments).ToList();

        await Relaxer.Start(segments, border);

        Log.LogInformation("Done. Redraw to continue.");
    }
}
