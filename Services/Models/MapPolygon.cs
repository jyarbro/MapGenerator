using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nrrdio.MapGenerator.Services.Models {
    public class MapPolygon : Polygon, IDisposable {
        public IEnumerable<MapPoint> MapPoints => Vertices.Cast<MapPoint>();
        public IEnumerable<MapSegment> MapSegments => Edges.Cast<MapSegment>();

        public Microsoft.UI.Xaml.Shapes.Path CanvasPolygon { get; }
        public Microsoft.UI.Xaml.Shapes.Ellipse CanvasCircumCircle { get; }

        public MapPolygon(params Point[] points) { throw new NotImplementedException("Points must be MapPoints"); }
        public MapPolygon(params MapPoint[] points) : this(points.AsEnumerable()) { }
        public MapPolygon(IEnumerable<MapPoint> points) : base(points) {
            foreach (MapPoint vertex in Vertices) {
                vertex.AdjacentMapPolygons.Add(this);
            }

            Edges.Clear();

            var mapPoints = MapPoints.ToList();

            for (int i = 0; i < VertexCount; i++) {
                var j = (i + 1) % VertexCount;
                Edges.Add(new MapSegment(mapPoints[i], mapPoints[j]));
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

        public void Dispose() {
            foreach (var point in MapPoints) {
                point.AdjacentMapPolygons.Remove(this);
            }
        }
    }
}
