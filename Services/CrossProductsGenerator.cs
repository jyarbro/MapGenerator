using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Nrrdio.MapGenerator.Services.Models;

namespace Nrrdio.MapGenerator.Services;

public class CrossProductsGenerator : GeneratorBase, IGenerator {
    public CrossProductsGenerator(ILogger<GeneratorBase> log) : base(log) { }

    public async Task Generate(int points, IEnumerable<MapPoint> borderVertices) {
        if (!Initialized) {
            throw new InvalidOperationException("Must initialize first");
        }
        
        Log.LogTrace(nameof(Generate));

        Clear();

        Border = new MapPolygon(borderVertices);

        var point1 = AddPoint(5, 10);
        var point2 = AddPoint(50, 300);

        var segment1 = AddSegment(point1, point2);
        segment1.ShowSubdued();

        AddNext(point2, segment1, 65, 270);
        AddNext(point2, segment1, 75, 180);
        AddNext(point2, segment1, 65, 320);

        AddNext(point2, segment1, 30, 330);
        AddNext(point2, segment1, 15, 280);
    }

    public Task<IEnumerable<MapPolygon>> GenerateWithReturn(int points, IEnumerable<MapPoint> borderPoints) => throw new NotImplementedException();

    void AddNext(MapPoint originPoint, MapSegment originSegment, int x, int y) {
        var point = AddPoint(x, y);

        var segment = AddSegment(originPoint, point);
        segment.ShowSubdued();

        //AddText($"{originSegment.AngleTo(segment)}", point.X, point.Y);
        //AddText($"{point.NearLine(originSegment)}", point.X, point.Y);
        AddText($"{originSegment.AngleTo(segment) * point.NearLine(originSegment)}", point.X, point.Y);
    }

    MapPoint AddPoint(int x, int y) {
        var point = new MapPoint(x, y);
        AddPoint(ref point);
        point.ShowSubdued();

        return point;
    }

    void AddText(string content, double left, double bottom) {
        var textBlock = new TextBlock {
            FontSize = 14,
            Text = content
        };

        Canvas.SetTop(textBlock, bottom);
        Canvas.SetLeft(textBlock, left + 15);

        textBlock.RenderTransform = new TransformGroup {
            Children = new TransformCollection {
                new TranslateTransform {
                    Y = -14
                },
                new ScaleTransform {
                   ScaleY = -1
                }
            }
        };

        OutputCanvas.Children.Add(textBlock);
    }
}
