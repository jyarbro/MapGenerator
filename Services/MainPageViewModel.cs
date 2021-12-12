using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Nrrdio.MapGenerator.Services.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nrrdio.MapGenerator.Services {
    public class MainPageViewModel {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool Continue { get; set; }

        public int Seed {
            get => seed;
            set {
                if (seed != value) {
                    seed = value;
                    OnPropertyChanged(nameof(Seed));
                }
            }
        }
        int seed;

        public int PointCount {
            get => pointCount;
            set {
                if (pointCount != value) {
                    pointCount = value;
                    OnPropertyChanged(nameof(PointCount));
                }
            }
        }
        int pointCount = 10;

        public int CanvasWidth {
            get => canvasWidth;
            set {
                if (canvasWidth != value) {
                    canvasWidth = value;
                    OnPropertyChanged(nameof(CanvasWidth));
                }
            }
        }
        int canvasWidth;

        public int CanvasHeight {
            get => canvasHeight;
            set {
                if (canvasHeight != value) {
                    canvasHeight = value;
                    OnPropertyChanged(nameof(CanvasHeight));
                }
            }
        }
        int canvasHeight;

        ILogger<MainPageViewModel> Log { get; }
        Generator Generator { get; }
        Visualizer Visualizer { get; }

        Canvas OutputCanvas { get; set; }
        MapPolygon Border { get; set; }
        HashSet<MapPoint> MapPoints { get; } = new HashSet<MapPoint>();
        HashSet<MapSegment> MapSegments { get; } = new HashSet<MapSegment>();
        HashSet<MapPolygon> MapTriangles { get; } = new HashSet<MapPolygon>();

        public MainPageViewModel(
            ILogger<MainPageViewModel> log,
            Generator generator,
            Visualizer visualizer
        ) {
            Log = log;
            Generator = generator;
            Visualizer = visualizer;
        }

        public void SetCanvas(Canvas canvas) {
            Log.LogTrace(nameof(SetCanvas));
            OutputCanvas = canvas;
            CanvasHeight = (int)OutputCanvas.ActualHeight;
            CanvasWidth = (int)OutputCanvas.ActualWidth;
        }

        public async Task GenerateAndDraw() {
            Log.LogTrace(nameof(GenerateAndDraw));

            MapPoints.Clear();
            MapTriangles.Clear();
            OutputCanvas.Children.Clear();

            Seed = new Random().Next();

            Border = new MapPolygon(new MapPoint(0, 0),
                                    new MapPoint(0, CanvasHeight),
                                    new MapPoint(CanvasWidth, CanvasHeight),
                                    new MapPoint(CanvasWidth, 0));

            await AddPoints();
            await AddBorderTriangles();
            await AddDelaunayTriangles();
            await AddVoronoiEdges();

            //var voronoiEdges = Generator.VoronoiEdges(triangles);
            //var voronoiPath = Visualizer.GetPath(voronoiEdges, 2, Colors.DarkViolet);
            //OutputCanvas.Children.Add(voronoiPath);
        }

        async Task AddPoints() {
            Log.LogInformation("Adding points");

            var random = new Random(seed);

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
                await Task.Delay(50);
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

        async Task AddPoint(MapPoint point) {
            await Task.Delay(1);

            if (!MapPoints.Contains(point)) {
                MapPoints.Add(point);
                OutputCanvas.Children.Add(point.CanvasPoint);
            }
        }

        async Task AddPolygon(MapPolygon polygon) {
            await Task.Delay(3);
            MapTriangles.Add(polygon);
            OutputCanvas.Children.Add(polygon.CanvasCircumCircle);
            OutputCanvas.Children.Add(polygon.CanvasPolygon);
        }

        async Task AddSegment(MapSegment segment) {
            await Task.Delay(3);

            if (!MapSegments.Contains(segment)) {
                MapSegments.Add(segment);
                OutputCanvas.Children.Add(segment.CanvasPath);
            }
        }

        async Task RemovePolygon(MapPolygon polygon) {
            await Task.Delay(1);

            foreach (var vertex in polygon.Vertices) {
                vertex.AdjacentPolygons.Remove(polygon);
            }

            OutputCanvas.Children.Remove(polygon.CanvasPolygon);
            OutputCanvas.Children.Remove(polygon.CanvasCircumCircle);
            MapTriangles.Remove(polygon);
        }

        async Task RemoveSegment(MapSegment segment) {
            await Task.Delay(1);

            OutputCanvas.Children.Remove(segment.CanvasPath);
        }

        public void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        async Task WaitForContinue() {
            Continue = false;

            await Task.Run(() => {
                while (!Continue) {
                    Thread.Sleep(10);
                }
            });
        }
    }
}