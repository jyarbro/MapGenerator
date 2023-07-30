using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;

namespace Nrrdio.MapGenerator.Services.Models;
public class MapPolygon : Polygon {
    public IList<MapPoint> MapPoints { get; init; }
    public IList<MapSegment> MapSegments { get; init; } = new List<MapSegment>();

    public Microsoft.UI.Xaml.Shapes.Path CanvasPath { get; }
    public Microsoft.UI.Xaml.Shapes.Polygon CanvasPolygon { get; }
    public Microsoft.UI.Xaml.Shapes.Ellipse CanvasCircumcircle { get; }

    Windows.UI.Color SubduedColor = Colors.Blue;
    Windows.UI.Color SubduedAltColor = Colors.Green;
    const int SubduedSize = 1;

    Windows.UI.Color HighlightedColor = Colors.LightSteelBlue;
    Windows.UI.Color HighlightedColorAlt = Colors.Orange;
    const int HighlightedSize = 3;

    public MapPolygon(params Point[] points) { throw new NotImplementedException("Points must be MapPoints"); }
    public MapPolygon(params MapPoint[] points) : this(points.AsEnumerable()) { }
    public MapPolygon(IEnumerable<MapPoint> points) : base(points) {
        MapPoints = new List<MapPoint>(points);

        for (var i = 0; i < VertexCount; i++) {
            var j = (i + 1) % VertexCount;
            MapSegments.Add(new MapSegment(MapPoints[i], MapPoints[j]));
        }

        // Construct the canvas path
        CanvasPath = new Microsoft.UI.Xaml.Shapes.Path {
            Visibility = Visibility.Collapsed,
            Stroke = new SolidColorBrush(SubduedColor),
            StrokeThickness = SubduedSize
        };

        var geometryGroup = new GeometryGroup();

        foreach (var edge in Edges) {
            var startX = edge.Point1.X;
            var startY = edge.Point1.Y;
            var endX = edge.Point2.X;
            var endY = edge.Point2.Y;

            var lineGeometry = new LineGeometry {
                StartPoint = new Windows.Foundation.Point {
                    X = startX,
                    Y = startY
                },
                EndPoint = new Windows.Foundation.Point {
                    X = endX,
                    Y = endY
                }
            };

            geometryGroup.Children.Add(lineGeometry);
        }

        CanvasPath.Data = geometryGroup;

        // Construct the canvas polygon
        CanvasPolygon = new Microsoft.UI.Xaml.Shapes.Polygon {
            Visibility = Visibility.Collapsed,
            Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 40, 40, 180))
        };

        var pointCollection = new PointCollection();

        foreach (var vertex in Vertices) {
            pointCollection.Add(new Windows.Foundation.Point(vertex.X, vertex.Y));
        }

        CanvasPolygon.Points = pointCollection;

        // Construct the canvas circumcircle
        CanvasCircumcircle = new Microsoft.UI.Xaml.Shapes.Ellipse {
            Visibility = Visibility.Collapsed,
            Stroke = new SolidColorBrush(Colors.DarkGray),
            StrokeThickness = 1,
            Width = Circumcircle.Radius * 2,
            Height = Circumcircle.Radius * 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(Circumcircle.Center.X - Circumcircle.Radius, Circumcircle.Center.Y - Circumcircle.Radius, 0, 0),
        };
    }

    public void ShowPointsSubdued() {
        foreach (var point in MapPoints) {
            point.ShowSubdued();
        }
    }

    public void ShowSegmentsSubdued() {
        foreach(var segment in MapSegments) {
            segment.ShowSubdued();
        }
    }

    public void ShowPathSubdued() {
        CanvasPath.Visibility = Visibility.Visible;
        CanvasPath.Stroke = new SolidColorBrush(SubduedColor);
        CanvasPath.StrokeThickness = SubduedSize;
    }

    public void ShowPathSubduedAlt() {
        ShowPathSubdued();
        CanvasPath.Stroke = new SolidColorBrush(SubduedAltColor);
    }

    public void ShowPathHighlighted() {
        CanvasPath.Visibility = Visibility.Visible;
        CanvasPath.Stroke = new SolidColorBrush(HighlightedColor);
        CanvasPath.StrokeThickness = HighlightedSize;
    }

    public void ShowPathHighlightedAlt() {
        ShowPathHighlighted();
        CanvasPath.Stroke = new SolidColorBrush(HighlightedColorAlt);
    }

    public void HidePath() {
        CanvasPath.Visibility = Visibility.Collapsed;
    }

    public void ShowPolygon() {
        CanvasPolygon.Visibility = Visibility.Visible;
    }

    public void HidePolygon() {
        CanvasPolygon.Visibility = Visibility.Collapsed;
    }

    public void ShowCircumcircle() {
        CanvasCircumcircle.Visibility = Visibility.Visible;
    }

    public void HideCircumcircle() {
        CanvasCircumcircle.Visibility = Visibility.Collapsed;
    }
}
