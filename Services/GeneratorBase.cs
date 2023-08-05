using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services.Models;
using Nrrdio.Utilities.Maths;
using System.Diagnostics;

namespace Nrrdio.MapGenerator.Services;
public class GeneratorBase {
    public bool Continue { get; set; }
    public int Seed {
        get => _Seed;
        set {
            _Seed = value;
            Random = new Random(_Seed);
        }
    }
    int _Seed;

    public Random Random { get; private set; } = new();

    protected ILogger<GeneratorBase> Log { get; }
    protected List<MapPoint> MapPoints { get; private set; } = new();
    protected List<MapSegment> MapSegments { get; private set; } = new();
    protected List<MapPolygon> MapPolygons { get; private set; } = new();

    protected Canvas OutputCanvas { get; set; }
    protected MapPolygon Border { get; set; }
    protected int PointCount { get; set; }
    protected bool Initialized { get; set; }
    protected int Iteration { get; set; }

    public GeneratorBase(
        ILogger<GeneratorBase> log
    ) {
        Log = log;
        Seed = Random.Next();
        //Seed = 1335106969;
        //Seed = 1586145746;
    }

    public void Initialize(Canvas outputCanvas) {
        OutputCanvas = outputCanvas;

        Initialized = true;
    }

    protected void GeneratePoints() {
        Log.LogTrace("Adding points");

        double pointX;
        double pointY;

        var minX = Border.Vertices.Min(point => point.X);
        var minY = Border.Vertices.Min(point => point.Y);
        var maxX = Border.Vertices.Max(point => point.X);
        var maxY = Border.Vertices.Max(point => point.Y);

        var rangeX = maxX - minX;
        var rangeY = maxY - minY;

        var i = 0;

        while (i < PointCount) {
            pointX = minX + Random.NextDouble() * rangeX;
            pointY = minY + Random.NextDouble() * rangeY;
            var point = new Point(pointX, pointY);

            if (Border.Contains(point)) {
                var mapPoint = new MapPoint(point);
                AddPoint(ref mapPoint);

                i++;
            }
        }

        MapPoints = MapPoints.OrderBy(point => point.Y).ThenBy(point => point.X).Cast<MapPoint>().ToList();
    }

    #region Polygons
    protected void AddPolygon(MapPolygon polygon) {
        MapPolygons.Add(polygon);

        OutputCanvas.Children.Add(polygon.CanvasCircumcircle);
        OutputCanvas.Children.Add(polygon.CanvasPolygon);
        OutputCanvas.Children.Add(polygon.CanvasPath);

        foreach (var point in polygon.MapPoints) {
            if (!OutputCanvas.Children.Contains(point.CanvasPoint)) {
                OutputCanvas.Children.Add(point.CanvasPoint);
            }
        }

        foreach (var segment in polygon.MapSegments) {
            OutputCanvas.Children.Add(segment.CanvasPath);
        }
    }

    protected void HidePolygons() {
        foreach (var polygon in MapPolygons.ToList()) {
            polygon.HideCircumcircle();
            polygon.HidePath();
            polygon.HidePolygon();
        }
    }

    protected void ClearPolygons() {
        foreach (var polygon in MapPolygons.ToList()) {
            RemovePolygon(polygon);
        }

        Debug.Assert(MapPolygons.Count == 0);
    }

    protected void RemovePolygon(MapPolygon polygon) {
        OutputCanvas.Children.Remove(polygon.CanvasPolygon);
        OutputCanvas.Children.Remove(polygon.CanvasPath);
        OutputCanvas.Children.Remove(polygon.CanvasCircumcircle);

        foreach (var point in polygon.MapPoints.Where(o => !MapPoints.Contains(o))) {
            OutputCanvas.Children.Remove(point.CanvasPoint);
        }

        foreach (var segment in polygon.MapSegments.Where(o => !MapSegments.Contains(o))) {
            OutputCanvas.Children.Remove(segment.CanvasPath);
        }

        if (!MapPolygons.Remove(polygon)) {
            throw new InvalidOperationException("Polygon not found in MapPolygons");
        }
    } 
    #endregion

    #region Segments
    protected MapSegment AddSegment(MapPoint point1, MapPoint point2) {
        if (point1 == point2) {
            throw new ArgumentException("The points must be different.");
        }

        AddPoint(ref point1);
        AddPoint(ref point2);

        // Don't look for reverse, because we need to add reverse segments later
        var segment = MapSegments.FirstOrDefault(o => o.Point1 == point1 && o.Point2 == point2);

        if (segment is null) {
            segment = new MapSegment(point1, point2);
            MapSegments.Add(segment);
            OutputCanvas.Children.Add(segment.CanvasPath);
        }

        return segment;
    }

    protected void HideSegments() {
        foreach (var segment in MapSegments.ToList()) {
            segment.Hide();
        }
    }

    protected void ClearSegments() {
        foreach (var segment in MapSegments.ToList()) {
            RemoveSegment(segment);
        }

        Debug.Assert(MapSegments.Count == 0);
    }

    protected void RemoveSegment(MapSegment segment) {
        OutputCanvas.Children.Remove(segment.CanvasPath);

        if (!MapSegments.Remove(segment)) {
            throw new InvalidOperationException("Segment not found in MapSegments");
        }
    }
    #endregion

    #region Points
    protected void AddPoint(ref MapPoint point) {
        var existingPoint = MapPoints.FirstOrDefault(point.Equals);

        if (existingPoint is null) {
            MapPoints.Add(point);

            if (!OutputCanvas.Children.Contains(point.CanvasPoint)) {
                OutputCanvas.Children.Add(point.CanvasPoint);
            }
        }
        else {
            point = existingPoint;
        }
    }

    protected void HidePoints() {
        foreach (var point in MapPoints.ToList()) {
            point.Hide();
        }
    }

    protected void ClearPoints() {
        foreach (var point in MapPoints.ToList()) {
            RemovePoint(point);
        }

        Debug.Assert(MapPoints.Count == 0);
    }

    protected void RemovePoint(MapPoint point) {
        OutputCanvas.Children.Remove(point.CanvasPoint);

        var preCount = MapPoints.Count;

        if (!MapPoints.Remove(point)) {
            throw new InvalidOperationException("Point not found in MapPoints");
        }

        Debug.Assert(preCount == MapPoints.Count + 1);
    }

    #endregion

    protected async Task WaitForContinue() {
        //Log.LogInformation("Wait");

        Continue = false;

        await Task.Run(() => {
            while (!Continue) {
                Thread.Sleep(10);
            }
        });
    }
}
