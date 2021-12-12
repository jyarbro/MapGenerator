using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.Utilities.Maths;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Nrrdio.MapGenerator.Services {
    public class MainPageViewModel {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Seed {
            get => seed;
            set {
                if (seed != value) {
                    seed = value;
                    OnPropertyChanged(nameof(Seed));
                }
            }
        }
        int seed = new Random().Next();

        public int PointCount {
            get => pointCount;
            set {
                if (pointCount != value) {
                    pointCount = value;
                    OnPropertyChanged(nameof(PointCount));
                }
            }
        }
        int pointCount = 200;

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

        public MainPageViewModel(
            ILogger<MainPageViewModel> log,
            Generator generator,
            Visualizer visualizer
        ) {
            Log = log;
            Generator = generator;
            Visualizer = visualizer;
        }

        public void UpdateCanvas(Canvas canvas) {
            OutputCanvas = canvas;

            CanvasHeight = (int)OutputCanvas.ActualHeight;
            CanvasWidth = (int)OutputCanvas.ActualWidth;

            var canvasArea = new Polygon(new Point(0, 0), new Point(0, CanvasHeight), new Point(CanvasWidth, CanvasHeight), new Point(CanvasWidth, 0));
            Generator.SetBorder(canvasArea);

            Log.LogInformation(nameof(UpdateCanvas));
        }

        public void GenerateAndDraw() {
            OutputCanvas.Children.Clear();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var points = Generator.Points(PointCount, Seed);
            var triangles = Generator.DelaunayTriangles(points);
            var triangleEdges = Generator.TriangleEdges(triangles);
            var voronoiEdges = Generator.VoronoiEdges(triangles);

            stopwatch.Stop();
            Log.LogInformation($"Generator duration: {stopwatch.ElapsedMilliseconds / 1000f} sec");
            stopwatch.Restart();

            var trianglePath = Visualizer.GetPath(triangleEdges, 1, Colors.LightSteelBlue);
            var voronoiPath = Visualizer.GetPath(voronoiEdges, 2, Colors.DarkViolet);
            var pointShapes = Visualizer.GetPointShapes(points, 3, Colors.Red);

            stopwatch.Stop();
            Log.LogInformation($"Visualizer duration: {stopwatch.ElapsedMilliseconds / 1000f} sec");
            stopwatch.Restart();

            OutputCanvas.Children.Add(trianglePath);
            OutputCanvas.Children.Add(voronoiPath);

            foreach (var pointShape in pointShapes) {
                OutputCanvas.Children.Add(pointShape);
            }

            stopwatch.Stop();

            Log.LogInformation($"Canvas duration: {stopwatch.ElapsedMilliseconds / 1000f} sec");
        }

        public void OnDrawButtonClick(object sender, RoutedEventArgs e) {
            GenerateAndDraw();
        }

        public void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

// Old resize handler with built in waiting until user is done.
// Save until window resizing is implemented.

//public void OnWindowSizeChanged(object sender, SizeChangedEventArgs e) {
//    Waiting = true;
//    if (SizeChangedTask is null || SizeChangedTask.IsCompleted) {
//        SizeChangedTask = UpdateCanvasSize();
//    }
//}

//await Task.Run(() => {
//    while (Waiting) {
//        Waiting = false;
//        Thread.Sleep(250);
//    }
//}); 

//Task SizeChangedTask { get; set; }
//bool Waiting { get; set; }
