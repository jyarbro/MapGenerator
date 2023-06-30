using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Nrrdio.MapGenerator.Services.Models;
using System;
using System.Threading.Tasks;

namespace Nrrdio.MapGenerator.Services;

public class CrossProductsGenerator : GeneratorBase, IGenerator {
    public CrossProductsGenerator(ILogger<GeneratorBase> log) : base(log) { }

    public async Task Generate(int points, MapPolygon border, Canvas outputCanvas) {
        Log.LogTrace(nameof(Generate));

        Seed = new Random().Next();
        OutputCanvas = outputCanvas;

        await Clear();

        var point1 = await AddPoint(5, 10);
        var point2 = await AddPoint(50, 300);

        var segment1 = new MapSegment(point1, point2);
        await AddSegment(segment1);

        await AddNext(point2, segment1, 65, 270);
        await AddNext(point2, segment1, 75, 180);
        await AddNext(point2, segment1, 65, 320);
        await AddNext(point2, segment1, 50, 160);

        await AddNext(point2, segment1, 30, 300);
        await AddNext(point2, segment1, 35, 280);
    }

    async Task AddNext(MapPoint originPoint, MapSegment originSegment, int x, int y) {
        var point = await AddPoint(x, y);
        var segment = new MapSegment(originPoint, point);

        await AddSegment(segment);

        AddText($"{originSegment.AngleTo(segment)}", point.X, point.Y);
        //LabelPoint(point, originSegment);
    }

    async Task<MapPoint> AddPoint(int x, int y) {
        var point = new MapPoint(x, y);
        await AddPoint(point);
        point.Highlight();

        return point;
    }

    void LabelPoint(MapPoint point, MapSegment segment) {
        AddText($"{point.NearLine(segment)}", point.X, point.Y);
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
