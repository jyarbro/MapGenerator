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
        public ISet<MapSegment> Edges { get; } = new HashSet<MapSegment>();
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

            for (int i = 0; i < _VertexCount; i++) {
                var j = (i + 1) % _VertexCount;
                Edges.Add(new MapSegment(Vertices[i], Vertices[j]));
            }

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
                Visibility = Visibility.Collapsed,
                Stroke = new SolidColorBrush(Colors.DarkGray),
                StrokeThickness = 1,
                Width = ValueObject.Circumcircle.Radius * 2,
                Height = ValueObject.Circumcircle.Radius * 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(ValueObject.Circumcircle.Center.X - ValueObject.Circumcircle.Radius, ValueObject.Circumcircle.Center.Y - ValueObject.Circumcircle.Radius, 0, 0),
            };
        }

        public override bool Equals(object obj) => (obj is MapPolygon other) && Equals(other);
        public bool Equals(MapPolygon other) => ValueObject.Equals(other.ValueObject);
        public static bool Equals(MapPolygon left, MapPolygon right) => left.Equals(right);
        public override int GetHashCode() => ValueObject.GetHashCode();
    }
}
