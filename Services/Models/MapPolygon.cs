using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;
using System.Collections.Generic;
using System.Linq;

namespace Nrrdio.MapGenerator.Services.Models {
    public class MapPolygon : Polygon {
        public Microsoft.UI.Xaml.Shapes.Path CanvasPolygon { get; }
        public Microsoft.UI.Xaml.Shapes.Ellipse CanvasCircumCircle { get; }

        public MapPolygon(params Point[] points) : this(points.Cast<MapPoint>().ToList()) { }
        public MapPolygon(params MapPoint[] points) : this(points.ToList()) { }
        public MapPolygon(IEnumerable<MapPoint> points) : base(points) {
            foreach (MapPoint vertex in Vertices) {
                vertex.AdjacentMapPolygons.Add(this);
            }

            Edges.Clear();

            for (int i = 0; i < VertexCount; i++) {
                var j = (i + 1) % VertexCount;
                Edges.Add(new MapSegment(Vertices[i] as MapPoint, Vertices[j] as MapPoint));
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
                Width = Circumcircle.Radius * 2,
                Height = Circumcircle.Radius * 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(Circumcircle.Center.X - Circumcircle.Radius, Circumcircle.Center.Y - Circumcircle.Radius, 0, 0),
            };
        }
    }
}
