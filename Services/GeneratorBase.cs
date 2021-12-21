using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services.Models;
using Nrrdio.Utilities.Maths;
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
        protected HashSet<MapPolygon> MapPolygons { get; } = new();
        
        protected Canvas OutputCanvas { get; set; }

        public GeneratorBase(
            ILogger<GeneratorBase> log
        ) {
            Log = log;
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
            await Task.Delay(2);
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

        protected async Task RemovePolygon(MapPolygon polygon) {
            await Task.Delay(0);

            foreach (MapPoint vertex in polygon.Vertices) {
                vertex.AdjacentMapPolygons.Remove(polygon);
            }

            OutputCanvas.Children.Remove(polygon.CanvasPolygon);
            OutputCanvas.Children.Remove(polygon.CanvasCircumCircle);
            MapPolygons.Remove(polygon);
        }

        protected async Task RemoveSegment(MapSegment segment) {
            await Task.Delay(0);

            OutputCanvas.Children.Remove(segment.CanvasPath);
            MapSegments.Remove(segment);
        }

        protected async Task RemovePoint(MapPoint point) {
            await Task.Delay(0);

            OutputCanvas.Children.Remove(point.CanvasPoint);
            MapPoints.Remove(point);
        }

        protected void Clear() {
            OutputCanvas.Children.Clear();
            MapPoints.Clear();
            MapSegments.Clear();
            MapPolygons.Clear();
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
