using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Nrrdio.MapGenerator.Services.Models; 
public class MapSegment : Segment, IDisposable {
    public MapPoint MapPoint1 => (MapPoint) Point1;
    public MapPoint MapPoint2 => (MapPoint) Point2;

    public LineGeometry CanvasGeometry { get; }
    public Microsoft.UI.Xaml.Shapes.Path CanvasPath { get; }

    public MapSegment(MapPoint point1, MapPoint point2) : base(point1, point2) {
        point1.AdjacentMapSegments.Add(this);
        point2.AdjacentMapSegments.Add(this);

        CanvasGeometry = new LineGeometry {
            StartPoint = new Windows.Foundation.Point {
                X = Point1.X,
                Y = Point1.Y
            },
            EndPoint = new Windows.Foundation.Point {
                X = Point2.X,
                Y = Point2.Y
            }
        };

        var geometryGroup = new GeometryGroup();
        geometryGroup.Children.Add(CanvasGeometry);

        CanvasPath = new Microsoft.UI.Xaml.Shapes.Path {
            Stroke = new SolidColorBrush(Colors.Blue),
            StrokeThickness = 3,
            Data = geometryGroup
        };
    }

    public void Dispose() {
        MapPoint1.AdjacentMapSegments.Remove(this);
        MapPoint2.AdjacentMapSegments.Remove(this);
    }

    public static IList<MapPoint> ArrangedVertices(IList<MapSegment> segments) {
        var copy = new List<MapSegment>(segments);

        var nextSegment = copy[0];
        copy.Remove(nextSegment);

        var currentPoint = nextSegment.Point1;
        var nextPoint = nextSegment.Point2;
        
        var vertices = new List<Point> {
            currentPoint 
        };

        while (copy.Count > 0) {
            var nextSegments = copy.Where(o => o.Point1 == nextPoint || o.Point2 == nextPoint).ToList();

            Debug.Assert(nextSegments.Count == 1);

            nextSegment = nextSegments[0];

            copy.Remove(nextSegment);
            currentPoint = nextSegment.Point1;
            nextPoint = nextSegment.Point2;
            vertices.Add(currentPoint);
        }

        return vertices.Cast<MapPoint>().ToList();
    }
}
