using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Nrrdio.Utilities.Maths;
using System.Collections.Generic;
using Windows.UI;

namespace Nrrdio.MapGenerator.Services {
    public class Visualizer {
        public List<Ellipse> GetPointShapes(List<Point> points, double size, Color color) {
            var ellipses = new List<Ellipse>();

            foreach (var point in points) {
                var ellipse = new Ellipse {
                    Fill = new SolidColorBrush(color),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = size,
                    Height = size,
                    Margin = new Thickness(point.X - 0.5 * size, point.Y - 0.5 * size, 0, 0)
                };

                ellipses.Add(ellipse);
            }

            return ellipses;
        }

        public Path GetPath(IEnumerable<Segment> lines, double thickness, Color color) {
            var path = new Path {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness
            };

            var geometryGroup = new GeometryGroup();

            foreach (var line in lines) {
                var startX = line.Point1.X;
                var startY = line.Point1.Y;
                var endX = line.Point2.X;
                var endY = line.Point2.Y;

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

            path.Data = geometryGroup;

            return path;
        }
    }
}
