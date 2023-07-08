using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;
using System.Diagnostics;

namespace Nrrdio.MapGenerator.Services.Models;
public class MapSegment : Segment, IDisposable {
    public MapPoint MapPoint1 => (MapPoint) Point1;
    public MapPoint MapPoint2 => (MapPoint) Point2;
    public MapPoint[] EndPoints { get; } = new MapPoint[2];

    public LineGeometry CanvasGeometry { get; }
    public Microsoft.UI.Xaml.Shapes.Path CanvasPath { get; }

    Windows.UI.Color SubduedColor = Colors.Blue;
    const int SubduedSize = 1;

    Windows.UI.Color HighlightedColor = Colors.Purple;
    Windows.UI.Color HighlightedAltColor = Colors.Orange;
    const int HighlightedSize = 5;

    public MapSegment(MapPoint point1, MapPoint point2) : base(point1, point2) {
        EndPoints[0] = point1;
        EndPoints[1] = point2;

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
            Visibility = Visibility.Collapsed,
            Stroke = new SolidColorBrush(SubduedColor),
            StrokeThickness = SubduedSize,
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
            currentPoint = nextSegment.Point1;
            nextPoint = nextSegment.Point2;

            copy.Remove(nextSegment);
            vertices.Add(currentPoint);
        }

        return vertices.Cast<MapPoint>().ToList();
    }

    public void ShowSubdued() {
        CanvasPath.Visibility = Visibility.Visible;
        CanvasPath.Stroke = new SolidColorBrush(SubduedColor);
        CanvasPath.StrokeThickness = SubduedSize;
    }

    public void ShowHighlighted() {
        CanvasPath.Visibility = Visibility.Visible;
        CanvasPath.Stroke = new SolidColorBrush(HighlightedColor);
        CanvasPath.StrokeThickness = HighlightedSize;
    }

    public void ShowHighlightedAlt() {
        CanvasPath.Visibility = Visibility.Visible;
        CanvasPath.Stroke = new SolidColorBrush(HighlightedAltColor);
        CanvasPath.StrokeThickness = HighlightedSize;
    }

    public void ShowHighlightedRand() {
        var rand = new Random();
        var r = rand.Next(40, 255);
        var g = rand.Next(40, 255);
        var b = rand.Next(40, 255);

        CanvasPath.Visibility = Visibility.Visible;
        CanvasPath.Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b)));
        CanvasPath.StrokeThickness = HighlightedSize;
    }
    
    public void Hide() {
        CanvasPath.Visibility = Visibility.Collapsed;
    }
}
