using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;

namespace Nrrdio.MapGenerator.Services.Models {
    public class MapSegment : Segment {
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
    }
}
