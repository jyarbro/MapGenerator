using Microsoft.Extensions.Logging;
using Nrrdio.MapGenerator.Services.Models;
using Nrrdio.Utilities;
using WinRT;

namespace Nrrdio.MapGenerator.Services;
public class Relaxer {
    public Random Random { get; set; }
    public int Seed { get; set; }

    ILogger<Relaxer> Log { get; }
    ICanvasWrapper Canvas { get; }
    Wait Wait { get; }

    MapPolygon Border { get; set; }
    List<MapSegment> Segments { get; set; }

#pragma warning disable CS8618
    // Random set by Orchestrator
    // Border set in Start
    // Segments set in Start
    public Relaxer(
        ILogger<Relaxer> log,
        ICanvasWrapper canvas,
        Wait wait
    ) {
        Log = log;
        Canvas = canvas;
        Wait = wait;
    }
#pragma warning restore CS8618

    /*

    My "inverted" lloyd's relaxation:
	    Find each unique vertex not on the border.
	    Maybe merge vertices close to each other?
	    Find the outer most point of every segment connected to the vertex.
	    Generate a polygon from those points.
	    Find the centroid.
        Move the vertex closer to the centroid.

     */

    public async Task Start(List<MapSegment> segments, MapPolygon border) {
        Border = border;

        // sorting these helps debugging
        Segments = segments.OrderBy(o => o.Point1.X).ToList();

        var vertices = Segments.SelectMany(o => o.EndPoints).Distinct().Where(o1 => border.MapSegments.All(o2 => !o2.Contains(o1))).ToList();

        foreach (var vertex in vertices) {
            var vertexPolygon = await CreatePolygonAroundVertex(vertex);

            if (vertexPolygon is null) {
                Log.LogInformation("Polygon is null");
                continue;
            }

            for (var i = 0; i < 5; i++) {
                var intermediatePoint = await FindIntermediatePoint(vertex, vertexPolygon);
                await MoveVertexToIntermediatePoint(vertex, intermediatePoint);
            }
        }
    }

    async Task<MapPolygon?> CreatePolygonAroundVertex(MapPoint vertex) {
        var debug = false;
        Wait.Delay = 200;
        Wait.Pause = true;

        var segments = Segments.Where(o => o.EndPoints.Contains(vertex)).ToList();
        var polygonVertices = segments.SelectMany(o => o.EndPoints).Distinct().Where(o => o != vertex).ToList();

        if (debug) {
            vertex.ShowHighlighted();

            foreach (var segment in segments) {
                segment.ShowHighlightedAlt();
            }
        }

        var polygon = default(MapPolygon);

        if (polygonVertices.Count > 2) {
            polygon = new MapPolygon(polygonVertices);

            if (debug) {
                Canvas.Children.Add(polygon.CanvasPath);
                polygon.ShowPathHighlighted();
            }
        }

        if (debug) {
            await Wait.ForDelay();

            foreach (var segment in segments) {
                segment.ShowSubdued();
            }

            vertex.Hide();

            if (polygon is not null) {
                Canvas.Children.Remove(polygon.CanvasPath);
            }
        }

        return polygon;
    }

    async Task<MapPoint> FindIntermediatePoint(MapPoint originalVertex, MapPolygon vertexPolygon) {
        var debug = false;
        Wait.Delay = 200;
        Wait.Pause = true;

        var centroidMapPoint = new MapPoint(vertexPolygon.Centroid);
        var intermediatePoint = new MapPoint(originalVertex.Lerp(centroidMapPoint, 0.25));

        if (debug) {
            Canvas.Children.Add(centroidMapPoint.CanvasPoint);
            centroidMapPoint.ShowHighlighted();

            Canvas.Children.Add(intermediatePoint.CanvasPoint);
            intermediatePoint.ShowHighlightedRand();

            await Wait.ForDelay();

            Canvas.Children.Remove(centroidMapPoint.CanvasPoint);
            Canvas.Children.Remove(intermediatePoint.CanvasPoint);
        }

        return intermediatePoint;
    }

    async Task MoveVertexToIntermediatePoint(MapPoint vertex, MapPoint intermediatePoint) {
        var debug = false;
        Wait.Delay = 100;
        Wait.Pause = false;

        var segments = Segments.Where(o => o.EndPoints.Contains(vertex)).ToList();
        var polygonVertices = segments.SelectMany(o => o.EndPoints).Where(o => o != vertex).ToList();

        foreach (var segment in segments) {
            Canvas.Children.Remove(segment.CanvasPath);
            Segments.Remove(segment);
        }

        foreach (var point in polygonVertices) {
            var segment = new MapSegment(point, intermediatePoint);
            Canvas.Children.Add(segment.CanvasPath);

            segment.ShowSubdued();

            Segments.Add(segment);
        }

        if (debug) {
            await Wait.ForDelay();
        }
    }
}
