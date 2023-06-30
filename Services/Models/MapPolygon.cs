using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Nrrdio.Utilities.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nrrdio.MapGenerator.Services.Models; 
public class MapPolygon : Polygon, IDisposable {
    public IEnumerable<MapPoint> MapPoints => Vertices.Cast<MapPoint>();
    public IEnumerable<MapSegment> MapSegments => Edges.Cast<MapSegment>();

    public Microsoft.UI.Xaml.Shapes.Path CanvasPath { get; }
    public Microsoft.UI.Xaml.Shapes.Polygon CanvasPolygon { get; }
    public Microsoft.UI.Xaml.Shapes.Ellipse CanvasCircumcircle { get; }

    Windows.UI.Color SubduedColor = Colors.LightSteelBlue;
    const int SubduedSize = 2;

    Windows.UI.Color HighlightedColor = Colors.Purple;
    const int HighlightedSize = 6;

    public MapPolygon(params Point[] points) { throw new NotImplementedException("Points must be MapPoints"); }
    public MapPolygon(params MapPoint[] points) : this(points.AsEnumerable()) { }
    public MapPolygon(IEnumerable<MapPoint> points) : base(points) {
        // Notify our neighbors that we've moved in next door.        
        foreach (var vertex in Vertices.Cast<MapPoint>()) {
            vertex.AdjacentMapPolygons.Add(this);
        }

        // Add edges
        Edges.Clear();

        var mapPoints = MapPoints.ToList();

        for (var i = 0; i < VertexCount; i++) {
            var j = (i + 1) % VertexCount;
            Edges.Add(new MapSegment(mapPoints[i], mapPoints[j]));
        }

        // Construct the canvas path
        CanvasPath = new Microsoft.UI.Xaml.Shapes.Path {
            Stroke = new SolidColorBrush(SubduedColor),
            StrokeThickness = SubduedSize
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

        CanvasPath.Data = geometryGroup;

        // Construct the canvas polygon
        CanvasPolygon = new Microsoft.UI.Xaml.Shapes.Polygon {
            Visibility = Visibility.Collapsed,
            Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 40, 40, 180))
        };

        var pointCollection = new PointCollection();

        foreach (var vertex in Vertices) {
            pointCollection.Add(new Windows.Foundation.Point(vertex.X, vertex.Y));
        }

        CanvasPolygon.Points = pointCollection;

        // Construct the canvas circumcircle
        CanvasCircumcircle = new Microsoft.UI.Xaml.Shapes.Ellipse {
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

    public void Subdue() {
        CanvasPath.Stroke = new SolidColorBrush(SubduedColor);
        CanvasPath.StrokeThickness = SubduedSize;
    }

    public void Highlight() {
        CanvasPath.Stroke = new SolidColorBrush(HighlightedColor);
        CanvasPath.StrokeThickness = HighlightedSize;
    }

    public void ShowPath() {
        CanvasPath.Visibility = Visibility.Visible;
    }

    public void HidePath() {
        CanvasPath.Visibility = Visibility.Collapsed;
    }

    public void ShowPolygon() {
        CanvasPolygon.Visibility = Visibility.Visible;
    }

    public void HidePolygon() {
        CanvasPolygon.Visibility = Visibility.Collapsed;
    }

    public void ShowCircumcircle() {
        CanvasCircumcircle.Visibility = Visibility.Visible;
    }

    public void HideCircumcircle() {
        CanvasCircumcircle.Visibility = Visibility.Collapsed;
    }
}
