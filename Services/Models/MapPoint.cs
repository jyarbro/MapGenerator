using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;
using System.Collections.Generic;

namespace Nrrdio.MapGenerator.Services.Models; 
public class MapPoint : Point {
    public IList<MapPolygon> AdjacentMapPolygons { get; } = new List<MapPolygon>();
    public IList<MapSegment> AdjacentMapSegments { get; } = new List<MapSegment>();
    public Microsoft.UI.Xaml.Shapes.Ellipse CanvasPoint { get; }

    public MapPoint(Point point) : this(point.X, point.Y) { }
    public MapPoint(MapPoint point) : this(point.X, point.Y) { }
    public MapPoint(double x, double y) : base(x, y) {
        CanvasPoint = new Microsoft.UI.Xaml.Shapes.Ellipse {
            Fill = new SolidColorBrush(Colors.Red),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 3,
            Height = 3,
            Margin = new Thickness(X - 0.5 * 3, Y - 0.5 * 3, 0, 0)
        };
    }
}
