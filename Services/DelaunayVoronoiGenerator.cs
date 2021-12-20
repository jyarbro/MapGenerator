using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Nrrdio.MapGenerator.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nrrdio.MapGenerator.Services {
    public class DelaunayVoronoiGenerator : GeneratorBase {
        MapPolygon Border { get; set; }
        int PointCount { get; set; }

        public DelaunayVoronoiGenerator(
            ILogger<GeneratorBase> log
        ) : base(log) { }

        public async Task Generate(int points, MapPolygon border, Canvas outputCanvas) {
            Log.LogTrace(nameof(Generate));

            Seed = new Random().Next();
            OutputCanvas = outputCanvas;

            MapPoints.Clear();
            MapTriangles.Clear();
            OutputCanvas.Children.Clear();

            PointCount = points;
            Border = border;

            await AddPoints();
            await AddBorderTriangles();
            await AddDelaunayTriangles();
            await AddVoronoiEdges();
            await RemovePoints();
            await RemoveTriangles();
            await ChopBorder();
            await LloydsRelaxation();
        }

        async Task AddPoints() {
            Log.LogInformation("Adding points");

            var random = new Random(Seed);

            double pointX;
            double pointY;

            var minX = Border.Vertices.Min(o => o.X);
            var minY = Border.Vertices.Min(o => o.Y);
            var maxX = Border.Vertices.Max(o => o.X);
            var maxY = Border.Vertices.Max(o => o.Y);

            var rangeX = maxX - minX;
            var rangeY = maxY - minY;

            var i = 0;

            while (i < PointCount) {
                pointX = minX + random.NextDouble() * rangeX;
                pointY = minY + random.NextDouble() * rangeY;

                var point = new MapPoint(pointX, pointY);
                await AddPoint(point);

                i++;
            }
        }

        async Task AddBorderTriangles() {
            Log.LogInformation("Adding border triangles");

            var borderVertices = Border.Vertices.Count;
            int j;

            var centroid = new MapPoint(Border.Centroid);

            for (var i = 0; i < borderVertices; i++) {
                j = (i + 1) % borderVertices;

                var triangle = new MapPolygon(centroid, Border.Vertices[i], Border.Vertices[j]);
                await AddPolygon(triangle);
            }
        }

        // https://www.codeguru.com/cplusplus/delaunay-triangles/
        async Task AddDelaunayTriangles() {
            Log.LogInformation("Adding delaunay triangles");

            foreach (var point in MapPoints) {
                var originalPointSize = point.CanvasPoint.Width;

                point.CanvasPoint.Width = 10;
                point.CanvasPoint.Height = 10;

                var badTriangles = MapTriangles.Where(o => o.Circumcircle.Contains(point)).Cast<MapPolygon>().ToList();

                // The inner shared edges will have a count > 1
                var holeBoundaries = badTriangles.SelectMany(t => t.Edges)
                                                 .GroupBy(o => o)
                                                 .Where(o => o.Count() == 1)
                                                 .Select(o => o.First())
                                                 .ToList();

                foreach (var edge in holeBoundaries.Where(possibleEdge => !possibleEdge.Contains(point))) {
                    var triangle = new MapPolygon(point, edge.Point1, edge.Point2);
                    await AddPolygon(triangle);
                }

                foreach (var polygon in badTriangles) {
                    await RemovePolygon(polygon);
                }

                point.CanvasPoint.Width = originalPointSize;
                point.CanvasPoint.Height = originalPointSize;

                //await WaitForContinue();
            }
        }

        async Task AddVoronoiEdges() {
            Log.LogInformation("Starting voronoi edges");

            var voronoiEdges = new List<MapSegment>();

            foreach (var triangle in MapTriangles) {
                var triangleOriginalColor = triangle.CanvasPolygon.Stroke;
                var triangleOriginalThickness = triangle.CanvasPolygon.StrokeThickness;
                triangle.CanvasPolygon.Stroke = new SolidColorBrush(Colors.Red);
                triangle.CanvasPolygon.StrokeThickness = 5;

                foreach (MapSegment edge in triangle.Edges) {
                    await AddSegment(edge);

                    var neighbors = MapTriangles.Where(other => other.Edges.Contains(edge) && triangle != other);

                    foreach (var neighbor in neighbors) {
                        var neighborOriginalColor = neighbor.CanvasPolygon.Stroke;
                        var neighborOriginalThickness = neighbor.CanvasPolygon.StrokeThickness;
                        neighbor.CanvasPolygon.Stroke = new SolidColorBrush(Colors.Blue);
                        neighbor.CanvasPolygon.StrokeThickness = 5;

                        var point1 = new MapPoint(triangle.Circumcircle.Center);
                        var point2 = new MapPoint(neighbor.Circumcircle.Center);

                        await AddPoint(point1);
                        await AddPoint(point2);

                        await AddSegment(new MapSegment(point1, point2));

                        neighbor.CanvasPolygon.Stroke = neighborOriginalColor;
                        neighbor.CanvasPolygon.StrokeThickness = neighborOriginalThickness;
                    }

                    await RemoveSegment(edge);
                }

                triangle.CanvasPolygon.Stroke = triangleOriginalColor;
                triangle.CanvasPolygon.StrokeThickness = triangleOriginalThickness;
            }
        }

        async Task RemovePoints() {
            foreach (var point in MapPoints) {
                await RemovePoint(point);
            }
        }

        async Task RemoveTriangles() {
            foreach (var triangle in MapTriangles) {
                await RemovePolygon(triangle);
            }
        }

        async Task ChopBorder() {
            var externalSegments = MapSegments.Where(segment => !Border.Contains(segment.Point1) || !Border.Contains(segment.Point2)).ToList();

            foreach (MapSegment borderEdge in Border.Edges) {
                var borderPoints = new List<MapPoint> {
                    (MapPoint) borderEdge.Point1,
                    (MapPoint) borderEdge.Point2
                };

                borderEdge.CanvasPath.StrokeThickness = 5;
                borderEdge.CanvasPath.Stroke = new SolidColorBrush(Colors.Black);

                await AddSegment(borderEdge);

                foreach (var segment in externalSegments) {
                    Log.LogInformation($"{segment}");

                    var origThickness = segment.CanvasPath.StrokeThickness;
                    var origStroke = segment.CanvasPath.Stroke;
                    segment.CanvasPath.StrokeThickness = 5;
                    segment.CanvasPath.Stroke = new SolidColorBrush(Colors.Purple);

                    var (intersects, intersection, intersectionEnd) = borderEdge.Intersects(segment);

                    if (intersects && intersectionEnd is null) {
                        await RemoveSegment(segment);

                        var borderIntersect = new MapPoint(intersection);

                        if (Border.Contains(segment.Point1)) {
                            await AddSegment(new MapSegment((MapPoint) segment.Point1, borderIntersect));
                        }
                        else {
                            await AddSegment(new MapSegment(borderIntersect, (MapPoint) segment.Point2));
                        }

                        borderPoints.Add(borderIntersect);
                    }

                    //await WaitForContinue();

                    segment.CanvasPath.StrokeThickness = origThickness;
                    segment.CanvasPath.Stroke = origStroke;
                }

                await RemoveSegment(borderEdge);

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
            // maybe add points to left[] and right[] then remove as iterating.or maybe add to l/ r as edges iterated.
            await Task.Delay(0);
        }

        async Task LloydsRelaxation() {
            // move poly centroid to circumcircle centroid? or maybe redraw everything off of circumcircles.
            await Task.Delay(0);
        }
    }
}
