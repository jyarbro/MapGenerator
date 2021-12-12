using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Nrrdio.MapGenerator.Services.Models;
using Nrrdio.Utilities.Maths;
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
        int pointCount = 50;

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
        HashSet<MapPolygon> MapPolygons { get; } = new HashSet<MapPolygon>();

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
            MapPolygons.Clear();
            OutputCanvas.Children.Clear();

            Seed = new Random().Next();

            Border = new MapPolygon(new MapPoint(0, 0),
                                    new MapPoint(0, CanvasHeight),
                                    new MapPoint(CanvasWidth, CanvasHeight),
                                    new MapPoint(CanvasWidth, 0));

            await AddPoints();
            await AddBorderTriangles();
            await AddDelaunayTriangles();

            //var triangleEdges = Generator.TriangleEdges(triangles);
            //var voronoiEdges = Generator.VoronoiEdges(triangles);

            //var voronoiPath = Visualizer.GetPath(voronoiEdges, 2, Colors.DarkViolet);

            //OutputCanvas.Children.Add(trianglePath);
            //OutputCanvas.Children.Add(voronoiPath);
        }

        async Task AddPoints() {
            Log.LogTrace(nameof(AddPoints));
            
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

                if (Border.ValueObject.Contains(point.ValueObject)) {
                    await AddPoint(point);
                }

                i++;
            }
        }

        public async Task AddBorderTriangles() {
            Log.LogTrace(nameof(AddBorderTriangles));
            
            var borderVertices = Border.Vertices.Count;
            int j;

            var centroid = new MapPoint(Border.ValueObject.Centroid);
            await AddPoint(centroid);

            for (var i = 0; i < borderVertices; i++) {
                j = (i + 1) % borderVertices;

                var triangle = new MapPolygon(centroid, Border.Vertices[i], Border.Vertices[j]);
                await AddPolygon(triangle);
            }
        }

        public async Task AddDelaunayTriangles() {
            Log.LogTrace(nameof(AddDelaunayTriangles));
            
            foreach (var point in MapPoints) {
                var originalPointSize = point.CanvasPoint.Width;

                point.CanvasPoint.Width = 10;
                point.CanvasPoint.Height = 10;

                var badTriangles = MapPolygons.Where(o => o.ValueObject.Circumcircle.Contains(point.ValueObject)).ToList();

                var holeBoundaries = badTriangles.SelectMany(t => t.Edges)
                                                 .GroupBy(o => o)
                                                 .Where(o => o.Count() == 1)
                                                 .Select(o => o.First())
                                                 .ToList();

                var geometryGroup = new GeometryGroup();

                foreach (var segment in holeBoundaries) {
                    geometryGroup.Children.Add(segment.CanvasObject);
                }

                var path = new Microsoft.UI.Xaml.Shapes.Path {
                    Stroke = new SolidColorBrush(Colors.Blue),
                    StrokeThickness = 3
                };

                path.Data = geometryGroup;

                OutputCanvas.Children.Add(path);

                //await WaitForContinue();
                await Task.Delay(10);

                foreach (var edge in holeBoundaries.Where(possibleEdge => !possibleEdge.ValueObject.Contains(point.ValueObject))) {
                    var triangle = new MapPolygon(point, edge.Point1, edge.Point2);
                    await AddPolygon(triangle);
                }

                foreach (var polygon in badTriangles) {
                    await RemovePolygon(polygon);
                }

                point.CanvasPoint.Width = originalPointSize;
                point.CanvasPoint.Height = originalPointSize;
                OutputCanvas.Children.Remove(path);

                //await WaitForContinue();
                await Task.Delay(50);
            }
        }

        async Task AddPoint(MapPoint point) {
            await Task.Delay(1);
            MapPoints.Add(point);
            OutputCanvas.Children.Add(point.CanvasPoint);
        }

        async Task AddPolygon(MapPolygon polygon) {
            await Task.Delay(3);
            MapPolygons.Add(polygon);
            OutputCanvas.Children.Add(polygon.CanvasCircumCircle);
            OutputCanvas.Children.Add(polygon.CanvasPolygon);
        }

        async Task RemovePolygon(MapPolygon polygon) {
            await Task.Delay(1);
            
            foreach (var vertex in polygon.Vertices) {
                vertex.AdjacentPolygons.Remove(polygon);
            }

            OutputCanvas.Children.Remove(polygon.CanvasPolygon);
            OutputCanvas.Children.Remove(polygon.CanvasCircumCircle);
            MapPolygons.Remove(polygon);
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