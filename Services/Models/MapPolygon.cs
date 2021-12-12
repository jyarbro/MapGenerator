using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;
using System.Collections.Generic;
using System.Linq;

namespace Nrrdio.MapGenerator.Services.Models {
    internal class MapPolygon {
        public Polygon ValueObject { get; }
        public IList<MapPoint> Vertices { get; }
        public ISet<MapSegment> Edges { get; }
        public Microsoft.UI.Xaml.Shapes.Path CanvasPolygon { get; }
        public Microsoft.UI.Xaml.Shapes.Ellipse CanvasCircumCircle { get; }

        readonly int _VertexCount;

        public MapPolygon(params MapPoint[] points) : this(points.ToList()) { }
        public MapPolygon(IEnumerable<MapPoint> points) {
            Vertices = points.ToList();
            _VertexCount = Vertices.Count;
            ValueObject = new Polygon(Vertices.Select(p => p.ValueObject));

            foreach (var vertex in Vertices) {
                vertex.AdjacentPolygons.Add(this);
            }

            Edges = new HashSet<MapSegment> {
                new MapSegment(Vertices[0], Vertices[1]),
                new MapSegment(Vertices[1], Vertices[2]),
                new MapSegment(Vertices[2], Vertices[0])
            };

            CanvasPolygon = new Microsoft.UI.Xaml.Shapes.Path {
                Stroke = new SolidColorBrush(Colors.LightSteelBlue),
                StrokeThickness = 2
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

            CanvasPolygon.Data = geometryGroup;

            CanvasCircumCircle = new Microsoft.UI.Xaml.Shapes.Ellipse {
                Stroke = new SolidColorBrush(Colors.Azure),
                StrokeThickness = 1,
                Width = ValueObject.Circumcircle.Radius * 2,
                Height = ValueObject.Circumcircle.Radius * 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(ValueObject.Circumcircle.Center.X - ValueObject.Circumcircle.Center.X * 0.5, ValueObject.Circumcircle.Center.Y - ValueObject.Circumcircle.Center.Y * 0.5, 0, 0),
            };
        }

        public override bool Equals(object obj) => (obj is MapPolygon other) && Equals(other);
        public bool Equals(MapPolygon other) => ValueObject == other.ValueObject;
        public static bool Equals(MapPolygon left, MapPolygon right) => left.Equals(right);

        public override int GetHashCode() {
            var hashCode = Vertices[0].GetHashCode();

            for (var i = 1; i < _VertexCount; i++) {
                hashCode ^= Vertices[i].GetHashCode();
            }

            return hashCode;
        }

    }
}
