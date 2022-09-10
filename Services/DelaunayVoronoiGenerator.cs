using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Nrrdio.MapGenerator.Services.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Nrrdio.MapGenerator.Services; 

public class DelaunayVoronoiGenerator : GeneratorBase {
    public DelaunayVoronoiGenerator(ILogger<GeneratorBase> log) : base(log) { }

    public async Task Generate(int points, MapPolygon border, Canvas outputCanvas) {
        Log.LogTrace(nameof(Generate));

        Seed = new Random().Next();
        OutputCanvas = outputCanvas;

        await Clear();

        PointCount = points;
        Border = border;

        await GeneratePoints();
        await AddBorderTriangles();
        await AddDelaunayTriangles();
        await AddVoronoiEdges();
        await ChopBorder();
        await FindPolygons();
    }

    async Task AddBorderTriangles() {
        Log.LogInformation("Adding border triangles");

        var borderVertices = Border.Vertices.Count;
        int j;

        var centroid = new MapPoint(Border.Centroid);
        await AddPoint(centroid);

        var borderPoints = Border.MapPoints.ToList();

        for (var i = 0; i < borderVertices; i++) {
            j = (i + 1) % borderVertices;

            var triangle = new MapPolygon(centroid, borderPoints[i], borderPoints[j]);
            await AddPoint(borderPoints[i]);
            await AddPolygon(triangle);
        }
    }

    // https://www.codeguru.com/cplusplus/delaunay-triangles/
    async Task AddDelaunayTriangles() {
        Log.LogInformation("Adding delaunay triangles");

        foreach (var mapPoint in MapPoints) {
            var originalPointSize = mapPoint.CanvasPoint.Width;

            // Highlight
            mapPoint.CanvasPoint.Width = 10;
            mapPoint.CanvasPoint.Height = 10;

            var badTriangles = MapPolygons.Where(polygon => polygon.Circumcircle.Contains(mapPoint)).ToList();

            // Use this block instead when debugging bad triangles.
            //var badTriangles = new List<MapPolygon>();

            //foreach (var mapPolygon in MapPolygons) {
            //    if (mapPolygon.Circumcircle.Contains(mapPoint)) {
            //        mapPolygon.CanvasPolygon.Stroke = new SolidColorBrush(Colors.Purple);
            //        mapPolygon.CanvasPolygon.StrokeThickness = 6;
            //        mapPolygon.CanvasCircumCircle.Visibility = Visibility.Visible;
            //        badTriangles.Add(mapPolygon);
            //    }
            //}

            // The inner shared edges will have a count > 1, so this only selects edges with a count of 1
            var holeBoundaries = badTriangles.SelectMany(polygon => polygon.MapSegments)
                                             .GroupBy(segment => segment)
                                             .Where(group => group.Count() == 1)
                                             .Select(group => group.First())
                                             .ToList();

            // DEGENERATE - Edge contains point - A solution may involve splitting the edge at the point and doing other smart stuff.
            Debug.Assert(!holeBoundaries.Any(edge => edge.Contains(mapPoint) && mapPoint != edge.Point1 && mapPoint != edge.Point2));

            foreach (var mapSegment in holeBoundaries) {
                await Task.Delay(0);

                OutputCanvas.Children.Add(mapSegment.CanvasPath);
                //mapSegment.CanvasPath.Stroke = new SolidColorBrush(Colors.Purple);
                //mapSegment.CanvasPath.StrokeThickness = 6;
            }

            var holeVertices = MapSegment.ArrangedVertices(holeBoundaries);

            // fill hole with new triangles.
            for (var i = 0; i < holeVertices.Count; i++) {
                var j = (i + 1) % holeVertices.Count;

                if (holeVertices[i] != mapPoint && holeVertices[j] != mapPoint) {
                    var triangle = new MapPolygon(mapPoint, holeVertices[i], holeVertices[j]);
                    await AddPolygon(triangle);
                }
            }

            await Task.Delay(0);

            foreach (var mapSegment in holeBoundaries) {
                mapSegment.CanvasPath.Stroke = new SolidColorBrush(Colors.SteelBlue);
                mapSegment.CanvasPath.StrokeThickness = 1;
                OutputCanvas.Children.Remove(mapSegment.CanvasPath);
            }

            foreach (var mapPolygon in badTriangles) {
                mapPolygon.CanvasCircumCircle.Visibility = Visibility.Collapsed;
                await RemovePolygon(mapPolygon);
            }

            // Un-highlight
            mapPoint.CanvasPoint.Width = originalPointSize;
            mapPoint.CanvasPoint.Height = originalPointSize;
        }
    }

    async Task AddVoronoiEdges() {
        Log.LogInformation("Starting voronoi edges");

        var origPolygons = new List<MapPolygon>(MapPolygons);

        OutputCanvas.Children.Clear();
        MapPolygons.Clear();
        MapSegments.Clear();
        MapPoints.Clear();

        foreach (var triangle in origPolygons) {
            //await Task.Delay(0);

            foreach (MapSegment edge in triangle.Edges) {
                var neighbors = origPolygons.Where(other => other.Edges.Contains(edge) && triangle != other);

                foreach (var neighbor in neighbors) {
                    var point1 = MapPoints.FirstOrDefault(point => point == triangle.Circumcircle.Center);

                    if (point1 is null) {
                        point1 = new MapPoint(triangle.Circumcircle.Center);
                        await AddPoint(point1);
                    }

                    var point2 = MapPoints.FirstOrDefault(point => point == neighbor.Circumcircle.Center);

                    if (point2 is null) {
                        point2 = new MapPoint(neighbor.Circumcircle.Center);
                        await AddPoint(point2);
                    }

                    var voronoiEdge = new MapSegment(point1, point2);

                    await AddSegment(voronoiEdge);
                }
            }
        }
    }

    async Task ChopBorder() {
        Log.LogInformation("Chopping borders");

        var externalSegments = MapSegments.Where(segment => !Border.Contains(segment.Point1) || !Border.Contains(segment.Point2)).ToList();

        foreach (var borderEdge in Border.MapSegments) {
            var borderPoints = new List<MapPoint> {
                borderEdge.MapPoint1,
                borderEdge.MapPoint2
            };

            //await AddSegment(borderEdge);

            foreach (var segment in externalSegments) {
                //await Task.Delay(0);

                //var origThickness = segment.CanvasPath.StrokeThickness;
                //var origStroke = segment.CanvasPath.Stroke;

                // Highlight
                //segment.CanvasPath.StrokeThickness = 5;
                //segment.CanvasPath.Stroke = new SolidColorBrush(Colors.Purple);

                //await WaitForContinue();

                var (intersects, intersection, intersectionEnd) = borderEdge.Intersects(segment);

                if (intersects && intersectionEnd is null) {
                    var borderIntersect = new MapPoint(intersection);

                    await AddPoint(borderIntersect);

                    if (Border.Contains(segment.Point1)) {
                        await AddSegment(new MapSegment(segment.MapPoint1, borderIntersect));
                    }
                    else {
                        await AddSegment(new MapSegment(borderIntersect, segment.MapPoint2));
                    }

                    borderPoints.Add(borderIntersect);
                }

                //segment.CanvasPath.StrokeThickness = origThickness;
                //segment.CanvasPath.Stroke = origStroke;
            }

            //await RemoveSegment(borderEdge);

            if (borderEdge.Point1.X == borderEdge.Point2.X) {
                borderPoints = borderPoints.OrderBy(p => p.Y).ToList();
            }
            else {
                borderPoints = borderPoints.OrderBy(p => p.X).ToList();
            }

            for (var i = 0; i < borderPoints.Count; i++) {
                var j = (i + 1) % borderPoints.Count;
                await AddSegment(new MapSegment(borderPoints[i], borderPoints[j]));
            }
        }

        externalSegments = MapSegments.Where(segment => !Border.Contains(segment.Point1) || !Border.Contains(segment.Point2)).ToList();

        foreach (var segment in externalSegments) {
            await RemoveSegment(segment);
        }
    }

    async Task FindPolygons() {
        Log.LogInformation("Finding polygons");

        for (var i = 0; i < MapSegments.Count; i++) {
            var currentSegment = MapSegments[i];
            var polygonPoints = new List<MapPoint>();

            await AddSegmentToRight((MapPoint)currentSegment.Point2, (MapPoint)currentSegment.Point1, currentSegment, polygonPoints);

            Log.LogInformation("Polygon found with {Points} points", polygonPoints.Count);

            await Task.Delay(10);
            //await WaitForContinue();
        }

        async Task AddSegmentToRight(MapPoint currentPoint, MapPoint originPoint, MapSegment currentSegment, List<MapPoint> polygonPoints) {
            currentSegment.CanvasPath.StrokeThickness = 5;
            currentSegment.CanvasPath.Stroke = new SolidColorBrush(Colors.Purple);

            await Task.Delay(10);
            await WaitForContinue();

            polygonPoints.Add(currentPoint);
            polygonPoints.Add(originPoint);

            var otherSegments = MapSegments.Where(segment => (segment.Point1 != originPoint && segment.Point2 != originPoint) && !(polygonPoints.Contains(segment.Point1) && polygonPoints.Contains(segment.Point2)));

            var rightMostCross = double.MaxValue;
            MapPoint nextPoint = default;
            MapSegment nextSegment = default;

            foreach (var otherSegment in otherSegments) {
                var farPoint = otherSegment.Point1 == currentPoint ? otherSegment.Point2 : otherSegment.Point1;
                var farPointCross = farPoint.NearLine(currentSegment);

                if (farPointCross < rightMostCross) {
                    nextPoint = (MapPoint)farPoint;
                    nextSegment = otherSegment;
                    rightMostCross = farPointCross;
                }
            }

            if (nextSegment is null) {
                return;
            }

            polygonPoints.Add(nextPoint);

            await AddSegmentToRight(nextPoint, currentPoint, nextSegment, polygonPoints);
        }
    }
}
