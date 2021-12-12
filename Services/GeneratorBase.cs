using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nrrdio.MapGenerator.Services {
    public class GeneratorBase {
        public bool Continue { get; set; }
        public int Seed { get; set; }

        protected ILogger<GeneratorBase> Log { get; }
        protected HashSet<MapPoint> MapPoints { get; } = new();
        protected HashSet<MapSegment> MapSegments { get; } = new();
        protected HashSet<MapPolygon> MapTriangles { get; } = new();
        
        protected Canvas OutputCanvas { get; set; }

        public GeneratorBase(
            ILogger<GeneratorBase> log
        ) {
            Log = log;
        }

        protected async Task AddPoint(MapPoint point) {
            await Task.Delay(1);

            if (!MapPoints.Contains(point)) {
                MapPoints.Add(point);
                OutputCanvas.Children.Add(point.CanvasPoint);
            }
        }

        protected async Task AddPolygon(MapPolygon polygon) {
            await Task.Delay(1);
            MapTriangles.Add(polygon);
            OutputCanvas.Children.Add(polygon.CanvasCircumCircle);
            OutputCanvas.Children.Add(polygon.CanvasPolygon);
        }

        protected async Task AddSegment(MapSegment segment) {
            await Task.Delay(1);

            if (!MapSegments.Contains(segment)) {
                MapSegments.Add(segment);
                OutputCanvas.Children.Add(segment.CanvasPath);
            }
        }

        protected async Task RemovePolygon(MapPolygon polygon) {
            await Task.Delay(1);

            foreach (var vertex in polygon.Vertices) {
                vertex.AdjacentPolygons.Remove(polygon);
            }

            OutputCanvas.Children.Remove(polygon.CanvasPolygon);
            OutputCanvas.Children.Remove(polygon.CanvasCircumCircle);
            MapTriangles.Remove(polygon);
        }

        protected async Task RemoveSegment(MapSegment segment) {
            await Task.Delay(1);

            OutputCanvas.Children.Remove(segment.CanvasPath);
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
