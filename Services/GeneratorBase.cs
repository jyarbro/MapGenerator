using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services.Models;
using Nrrdio.Utilities.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nrrdio.MapGenerator.Services {
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

        public GeneratorBase(
            ILogger<GeneratorBase> log
        ) {
            Log = log;
            Seed = Random.Next();
        }

        protected async Task GeneratePoints() {
            Log.LogInformation("Adding points");

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

                if (Border.Contains()) {
                    var mapPoint = new MapPoint(point);
                    await AddPoint(mapPoint);

                    i++;
                }
            }

            MapPoints = MapPoints.OrderBy(point => point.Y).ThenBy(point => point.X).Cast<MapPoint>().ToList();
        }

        protected async Task AddPoint(Point point) => await AddPoint(new MapPoint(point));
        protected async Task AddPoint(MapPoint point) {
            await Task.Delay(0);

            if (!MapPoints.Contains(point)) {
                MapPoints.Add(point);
                OutputCanvas.Children.Add(point.CanvasPoint);
            }
        }

        protected async Task AddPolygon(MapPolygon polygon) {
            await Task.Delay(0);
            MapPolygons.Add(polygon);
            OutputCanvas.Children.Add(polygon.CanvasCircumCircle);
            OutputCanvas.Children.Add(polygon.CanvasPolygon);
        }

        protected async Task AddSegment(MapSegment segment) {
            await Task.Delay(0);

            if (!MapSegments.Contains(segment)) {
                MapSegments.Add(segment);
                OutputCanvas.Children.Add(segment.CanvasPath);
            }
        }

        protected async Task ClearPolygons() {
            foreach (var polygon in MapPolygons.ToList()) {
                await RemovePolygon(polygon);
            }

            Debug.Assert(MapPolygons.Count == 0);
        }

        protected async Task RemovePolygon(MapPolygon polygon) {
            await Task.Delay(0);

            OutputCanvas.Children.Remove(polygon.CanvasPolygon);
            OutputCanvas.Children.Remove(polygon.CanvasCircumCircle);
            
            if (!MapPolygons.Remove(polygon)) {
                throw new InvalidOperationException("Polygon not found in MapPolygons");
            }

            polygon.Dispose();
        }

        protected async Task ClearSegments() {
            foreach (var segment in MapSegments.ToList()) {
                await RemoveSegment(segment);
            }

            Debug.Assert(MapSegments.Count == 0);
        }

        protected async Task RemoveSegment(MapSegment segment) {
            await Task.Delay(0);

            OutputCanvas.Children.Remove(segment.CanvasPath);
            
            if (!MapSegments.Remove(segment)) {
                throw new InvalidOperationException("Segment not found in MapSegments");
            }

            segment.Dispose();
        }

        protected async Task ClearPoints() {
            foreach (var point in MapPoints.ToList()) {
                await RemovePoint(point);
            }

            Debug.Assert(MapPoints.Count == 0);
        }

        protected async Task RemovePoint(MapPoint point) {
            await Task.Delay(0);

            OutputCanvas.Children.Remove(point.CanvasPoint);

            var preCount = MapPoints.Count;

            if (!MapPoints.Remove(point)) {
                throw new InvalidOperationException("Point not found in MapPoints");
            }

            Debug.Assert(preCount == MapPoints.Count + 1);
        }

        protected async Task Clear() {
            await ClearPolygons();
            await ClearSegments();
            await ClearPoints();
        }

        protected async Task WaitForContinue() {
            Continue = false;

            await Task.Run(() => {
                while (!Continue) {
                    Thread.Sleep(10);
                }
            });
        }
    }
}
