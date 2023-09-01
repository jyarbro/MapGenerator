using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;

namespace Nrrdio.MapGenerator.Services.Models;
public class MapSegment : Segment {
    public MapPoint MapPoint1 => (MapPoint) Point1;
    public MapPoint MapPoint2 => (MapPoint) Point2;
    public MapPoint[] EndPoints { get; } = new MapPoint[2];

    public Microsoft.UI.Xaml.Shapes.Path CanvasPath { get; }
    
    LineGeometry CanvasGeometry { get; }

    Windows.UI.Color SubduedColor = Colors.Blue;
    Windows.UI.Color SubduedAltColor = Colors.Purple;
    const int SubduedSize = 1;

    Windows.UI.Color HighlightedColor = Colors.LightSteelBlue;
    Windows.UI.Color HighlightedColorAlt = Colors.Orange;
    const int HighlightedSize = 3;

    public MapSegment(MapPoint point1, MapPoint point2) : base(point1, point2) {
        EndPoints[0] = point1;
        EndPoints[1] = point2;

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

    public void ShowSubdued() {
        CanvasPath.Visibility = Visibility.Visible;
        CanvasPath.Stroke = new SolidColorBrush(SubduedColor);
        CanvasPath.StrokeThickness = SubduedSize;
    }

    public void ShowSubduedAlt() {
        ShowSubdued();
        CanvasPath.Stroke = new SolidColorBrush(SubduedAltColor);
    }

    public void ShowHighlighted() {
        CanvasPath.Visibility = Visibility.Visible;
        CanvasPath.Stroke = new SolidColorBrush(HighlightedColor);
        CanvasPath.StrokeThickness = HighlightedSize;
    }

    public void ShowHighlightedAlt() {
        CanvasPath.Visibility = Visibility.Visible;
        CanvasPath.Stroke = new SolidColorBrush(HighlightedColorAlt);
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
