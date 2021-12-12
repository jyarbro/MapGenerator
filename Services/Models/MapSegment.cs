using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;

namespace Nrrdio.MapGenerator.Services.Models {
    internal class MapSegment {
        public Segment ValueObject { get; }
        public MapPoint Point1 { get; }
        public MapPoint Point2 { get; }
        public LineGeometry CanvasObject { get; }

        public MapSegment(MapPoint point1, MapPoint point2) {
            Point1 = point1;
            Point2 = point2;
            ValueObject = new Segment(Point1.ValueObject, Point2.ValueObject);

            CanvasObject = new LineGeometry {
                StartPoint = new Windows.Foundation.Point {
                    X = Point1.X,
                    Y = Point1.Y
                },
                EndPoint = new Windows.Foundation.Point {
                    X = Point2.X,
                    Y = Point2.Y
                }
            };
        }

        public override bool Equals(object obj) => (obj is MapSegment other) && Equals(other);
        public bool Equals(MapSegment other) => ValueObject.Equals(other.ValueObject);
        public static bool Equals(MapSegment left, MapSegment right) => left.Equals(right);
        public override int GetHashCode() => ValueObject.GetHashCode();

    }
}
