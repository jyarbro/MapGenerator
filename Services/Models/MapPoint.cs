using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;
using System.Collections.Generic;

namespace Nrrdio.MapGenerator.Services.Models {
    public class MapPoint {
        public Point ValueObject { get; }
        public double X => ValueObject.X;
        public double Y => ValueObject.Y;

        public IList<MapPolygon> AdjacentPolygons { get; } = new List<MapPolygon>();
        public Microsoft.UI.Xaml.Shapes.Ellipse CanvasPoint { get; }

        public MapPoint(double x, double y) : this(new Point(x, y)) { }
        public MapPoint(Point point) {
            ValueObject = point;

            CanvasPoint = new Microsoft.UI.Xaml.Shapes.Ellipse {
                Fill = new SolidColorBrush(Colors.Red),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 3,
                Height = 3,
                Margin = new Thickness(ValueObject.X - 0.5 * 3, ValueObject.Y - 0.5 * 3, 0, 0)
            };
        }
    }
}
