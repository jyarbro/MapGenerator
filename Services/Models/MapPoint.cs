using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;

namespace Nrrdio.MapGenerator.Services.Models;
public class MapPoint : Point {
    public Microsoft.UI.Xaml.Shapes.Ellipse CanvasPoint { get; }

    Windows.UI.Color SubduedColor = Colors.Blue;
    const int SubduedSize = 3;

    Windows.UI.Color HighlightedColor = Colors.Red;
    Windows.UI.Color HighlightedAltColor = Colors.Orange;
    const int HighlightedSize = 8;

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

    public void ShowHighlighted() {
        CanvasPoint.Visibility = Visibility.Visible;
        CanvasPoint.Width = HighlightedSize;
        CanvasPoint.Height = HighlightedSize;
        CanvasPoint.Margin = new Thickness(X - 0.5 * HighlightedSize, Y - 0.5 * HighlightedSize, 0, 0);

        CanvasPoint.Fill = new SolidColorBrush(HighlightedColor);
    }

    public void ShowHighlightedAlt() {
        CanvasPoint.Visibility = Visibility.Visible;
        CanvasPoint.Width = HighlightedSize;
        CanvasPoint.Height = HighlightedSize;
        CanvasPoint.Margin = new Thickness(X - 0.5 * HighlightedSize, Y - 0.5 * HighlightedSize, 0, 0);

        CanvasPoint.Fill = new SolidColorBrush(HighlightedAltColor);
    }

    public void ShowHighlightedRand() {
        var rand = new Random();
        var r = rand.Next(40, 255);
        var g = rand.Next(40, 255);
        var b = rand.Next(40, 255);

        CanvasPoint.Visibility = Visibility.Visible;
        CanvasPoint.Width = HighlightedSize;
        CanvasPoint.Height = HighlightedSize; 
        CanvasPoint.Margin = new Thickness(X - 0.5 * HighlightedSize, Y - 0.5 * HighlightedSize, 0, 0);

        CanvasPoint.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b)));
    }

    public void ShowSubdued() {
        CanvasPoint.Visibility = Visibility.Visible;
        CanvasPoint.Width = SubduedSize;
        CanvasPoint.Height = SubduedSize;

        CanvasPoint.Fill = new SolidColorBrush(SubduedColor);
        CanvasPoint.Margin = new Thickness(X - 0.5 * SubduedSize, Y - 0.5 * SubduedSize, 0, 0);
    }

    public void Hide() {
        CanvasPoint.Visibility = Visibility.Collapsed;
    }
}
