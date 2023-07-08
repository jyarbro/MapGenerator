using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;
using System.Collections.Generic;

namespace Nrrdio.MapGenerator.Services.Models; 
public class MapPoint : Point {
    public IList<MapPolygon> AdjacentMapPolygons { get; } = new List<MapPolygon>();
    public IList<MapSegment> AdjacentMapSegments { get; } = new List<MapSegment>();
    public Microsoft.UI.Xaml.Shapes.Ellipse CanvasPoint { get; }

    Windows.UI.Color SubduedColor = Colors.Blue;
    const int SubduedSize = 3;

    Windows.UI.Color HighlightedColor = Colors.Red;
    Windows.UI.Color HighlightedAltColor = Colors.Orange;
    const int HighlightedSize = 10;

    public MapPoint(Point point) : this(point.X, point.Y) { }
    public MapPoint(MapPoint point) : this(point.X, point.Y) { }
    public MapPoint(double x, double y) : base(x, y) {
        CanvasPoint = new Microsoft.UI.Xaml.Shapes.Ellipse {
            Visibility = Visibility.Collapsed,
            Fill = new SolidColorBrush(SubduedColor),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = SubduedSize,
            Height = SubduedSize,
            Margin = new Thickness(X - 0.5 * SubduedSize, Y - 0.5 * SubduedSize, 0, 0)
        };
    }

    public void Highlight() {
        CanvasPoint.Visibility = Visibility.Visible;
        CanvasPoint.Width = HighlightedSize;
        CanvasPoint.Height = HighlightedSize;
        CanvasPoint.Margin = new Thickness(X - 0.5 * HighlightedSize, Y - 0.5 * HighlightedSize, 0, 0);

        CanvasPoint.Fill = new SolidColorBrush(HighlightedColor);
    }

    public void HighlightAlt() {
        CanvasPoint.Visibility = Visibility.Visible;
        CanvasPoint.Width = HighlightedSize;
        CanvasPoint.Height = HighlightedSize;
        CanvasPoint.Margin = new Thickness(X - 0.5 * HighlightedSize, Y - 0.5 * HighlightedSize, 0, 0);

        CanvasPoint.Fill = new SolidColorBrush(HighlightedAltColor);
    }

    public void Subdue() {
        CanvasPoint.Visibility = Visibility.Visible;
        CanvasPoint.Width = SubduedSize;
        CanvasPoint.Height = SubduedSize;

        CanvasPoint.Fill = new SolidColorBrush(SubduedColor);
        CanvasPoint.Margin = new Thickness(X - 0.5 * SubduedSize, Y - 0.5 * SubduedSize, 0, 0);
    }

    public void Show() {
        CanvasPoint.Visibility = Visibility.Visible;
    }

    public void Hide() {
        CanvasPoint.Visibility = Visibility.Collapsed;
    }
}
