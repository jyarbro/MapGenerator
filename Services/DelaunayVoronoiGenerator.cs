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

            var centroid = new MapPoint(Border.ValueObject.Centroid);

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

                var badTriangles = MapTriangles.Where(o => o.ValueObject.Circumcircle.Contains(point.ValueObject)).ToList();

                var holeBoundaries = badTriangles.SelectMany(t => t.Edges)
                                                 .GroupBy(o => o)
                                                 .Where(o => o.Count() == 1)
                                                 .Select(o => o.First())
                                                 .ToList();

                foreach (var edge in holeBoundaries.Where(possibleEdge => !possibleEdge.ValueObject.Contains(point.ValueObject))) {
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

                foreach (var edge in triangle.Edges) {
                    await AddSegment(edge);
                    //await WaitForContinue();

                    var neighbors = MapTriangles.Where(other => other.Edges.Contains(edge) && triangle != other);

                    foreach (var neighbor in neighbors) {
                        var neighborOriginalColor = neighbor.CanvasPolygon.Stroke;
                        var neighborOriginalThickness = neighbor.CanvasPolygon.StrokeThickness;
                        neighbor.CanvasPolygon.Stroke = new SolidColorBrush(Colors.Blue);
                        neighbor.CanvasPolygon.StrokeThickness = 5;
                        //await WaitForContinue();

                        var point1 = new MapPoint(triangle.ValueObject.Centroid);
                        var point2 = new MapPoint(neighbor.ValueObject.Centroid);

                        await AddPoint(point1);
                        await AddPoint(point2);

                        //await WaitForContinue();

                        await AddSegment(new MapSegment(point1, point2));

                        //await WaitForContinue();

                        neighbor.CanvasPolygon.Stroke = neighborOriginalColor;
                        neighbor.CanvasPolygon.StrokeThickness = neighborOriginalThickness;
                    }

                    await RemoveSegment(edge);
                }

                triangle.CanvasPolygon.Stroke = triangleOriginalColor;
                triangle.CanvasPolygon.StrokeThickness = triangleOriginalThickness;
            }
        }
    }
}
