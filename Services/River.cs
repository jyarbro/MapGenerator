using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Nrrdio.MapGenerator.Services.Models;
using Nrrdio.Utilities;
using Nrrdio.Utilities.Maths;

namespace Nrrdio.MapGenerator.Services;

public class River {
    public Random Random { get; set; }
    public int Seed { get; set; }

    ILogger<River> Log { get; }
    ICanvasWrapper Canvas { get; }
    Wait Wait { get; }

    MapPolygon Border { get; set; }
    List<MapSegment> Segments { get; set; }

#pragma warning disable CS8618
    public River(
        ILogger<River> log,
        ICanvasWrapper canvas,
        Wait wait
    ) {
        Log = log;
        Canvas = canvas;
        Wait = wait;
    }
#pragma warning restore CS8618

    public async Task Start(List<MapSegment> segments, MapPolygon border) {
        var randomBorderIndex = Random.Next(0, border.MapSegments.Count);

        var riverStart = border.MapSegments[randomBorderIndex];
        MapSegment? riverEnd = null;

        while (riverEnd is null) {
            randomBorderIndex = Random.Next(0, border.MapSegments.Count);

            if (border.MapSegments[randomBorderIndex].AngleTo(riverStart) != 0) {
                riverEnd = border.MapSegments[randomBorderIndex];
            }
        }

        var riverSegment = new MapSegment(new MapPoint(riverStart.Midpoint), new MapPoint(riverEnd.Midpoint));
        //Canvas.Children.Add(riverSegment.CanvasPath);
        //riverSegment.ShowHighlighted();

        var riverPathEdges = segments.Where(o => o.Intersects(riverSegment).Intersects).ToList();
        
        foreach (var segment in riverPathEdges) {
            Canvas.Children.Remove(segment.CanvasPath);
        }

        var midpoints = riverPathEdges.Select(o => new MapPoint(o.Midpoint)).Distinct().OrderBy(o => o.Distance(riverSegment.Point1)).ToList();

        var river = new Polyline {
            Stroke = new SolidColorBrush(Colors.Orange),
            StrokeThickness = 1
        };

        var points = new PointCollection();

        for (var i = 0; i < midpoints.Count - 1; i++) {
            int p0i, p1i, p2i, p3i;

            p0i = i > 0 ? i - 1 : i;
            p1i = i;
            p2i = i < midpoints.Count - 1 ? i + 1 : i;

            if (i < midpoints.Count - 2) {
                p3i = i + 2;
            }
            else {
                p3i = i + 1;
            }

            var p0 = midpoints[p0i];
            var p1 = midpoints[p1i];
            var p2 = midpoints[p2i];
            var p3 = midpoints[p3i];

            var spline = new CatmullRomSpline(p0, p1, p2, p3);

            for (var t = 0d; t < 1; t += 0.1) {
                var point = spline.Interpolate(t);

                points.Add(new Windows.Foundation.Point(point.X, point.Y));
            }
        }

        river.Points = points;

        Canvas.Children.Add(river);

        //for (var i = 1; i < midpoints.Count; i++) {
        //    var segment = new MapSegment(midpoints[i - 1], midpoints[i]);
        //    Canvas.Children.Add(segment.CanvasPath);
        //    segment.ShowHighlightedAlt();
        //}

        await Wait.For(1);
    }
}
