using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Nrrdio.MapGenerator.Services.Models;
using System.Diagnostics;
using Nrrdio.Utilities.Maths;
using Nrrdio.Utilities;

namespace Nrrdio.MapGenerator.Services;

public class VoronoiTesselator {
    public Random Random { get; set; }
    public int Seed { get; set; }

    ILogger<VoronoiTesselator> Log { get; }
    ICanvasWrapper Canvas { get; }
    Wait Wait { get; }

    List<MapPoint> MapPoints { get; set; } = new();
    List<MapSegment> MapSegments { get; set; } = new();
    List<MapPolygon> MapPolygons { get; set; } = new();

    MapPolygon Border { get; set; }
    int PointCount { get; set; }
    int Iteration { get; set; }

#pragma warning disable CS8618 // Border is null
    public VoronoiTesselator(
        ILogger<VoronoiTesselator> log,
        ICanvasWrapper canvas,
        Wait wait
    ) {
        Log = log;
        Canvas = canvas;
        Wait = wait;
    }
#pragma warning restore CS8618

    public async Task<IEnumerable<MapPolygon>> Start(int points, MapPolygon border) {
        Log.LogTrace(nameof(Start));

        Iteration++;

        MapPolygons.Clear();
        MapSegments.Clear();
        MapPoints.Clear();

        PointCount = points;
        Border = border;

        Border.ShowPathSubdued();

        GeneratePoints();

        await AddBorderTriangles(clearCanvas: true);
        await AddDelaunayTriangles(clearCanvas: true);
        await AddVoronoiEdgesFromCircumcircles(clearCanvas: true);
        await AddMissingVoronoiEdges(clearCanvas: true);
        await ChopBorder(clearCanvas: true);
        await FixBorderWinding(clearCanvas: true);
        await FindPolygons();

        foreach (var segment in MapPolygons.SelectMany(o => o.MapSegments)) {
            segment.ShowSubdued();
        }

        await Wait.For(10);

        return MapPolygons;
    }

    #region Canvas Content

    void ClearCanvasArtifacts() {
        foreach (var polygon in MapPolygons) {
            polygon.HidePath();
            polygon.HideCircumcircle();
            polygon.HidePolygon();

            foreach (var segment in polygon.MapSegments) {
                segment.Hide();
            }
        }

        foreach (var segment in MapSegments) {
            segment.Hide();
        }

        foreach (var point in MapPoints) {
            point.Hide();
        }
    }

    void GeneratePoints() {
        Log.LogTrace("Adding points");

        double pointX;
        double pointY;

        var minX = Border.Vertices.Min(point => point.X);
        var minY = Border.Vertices.Min(point => point.Y);
        var maxX = Border.Vertices.Max(point => point.X);
        var maxY = Border.Vertices.Max(point => point.Y);

        var rangeX = maxX - minX;
        var rangeY = maxY - minY;

        var i = 0;

        while (i < PointCount) {
            pointX = minX + Random.NextDouble() * rangeX;
            pointY = minY + Random.NextDouble() * rangeY;
            var point = new Point(pointX, pointY);

            if (Border.Contains(point)) {
                var mapPoint = new MapPoint(point);
                AddPoint(ref mapPoint);

                i++;
            }
        }

        MapPoints = MapPoints.OrderBy(point => point.Y).ThenBy(point => point.X).Cast<MapPoint>().ToList();
    }

    async Task AddBorderTriangles(bool clearCanvas = true) {
        Log.LogTrace("Adding border triangles");

        var debug = false;
        Wait.Delay = 100;
        Wait.Pause = false;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(AddBorderTriangles)}");
            await Wait.For(1);
        }

        var borderVertices = Border.Vertices.Count;
        int j;

        var centroid = new MapPoint(Border.Centroid);
        var borderPoints = Border.MapPoints.ToList();

        for (var i = 0; i < borderVertices; i++) {
            j = (i + 1) % borderVertices;

            var triangle = new MapPolygon(centroid, borderPoints[i], borderPoints[j]);
            AddPolygon(triangle);

            var point = borderPoints[i];

            if (debug) {
                triangle.ShowPathSubdued();
                await Wait.ForDelay();
            }
        }

        if (debug) {
            Log.LogInformation($"Done {nameof(AddBorderTriangles)}");
            await Wait.For(1);
        }

        if (clearCanvas) {
            centroid.Hide();
            ClearCanvasArtifacts();
        }
    }

    // https://www.codeguru.com/cplusplus/delaunay-triangles/
    async Task AddDelaunayTriangles(bool clearCanvas = true) {
        Log.LogTrace("Adding delaunay triangles");

        var debug = false;
        Wait.Delay = 100;
        Wait.Pause = false;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(AddDelaunayTriangles)}");

            foreach (var polygon in MapPolygons) {
                polygon.ShowPathSubdued();
            }

            await Wait.For(1);
        }

        foreach (var mapPoint in MapPoints.ToList()) {
            if (debug) {
                mapPoint.ShowHighlighted();
            }

            var badTriangles = new List<MapPolygon>();

            foreach (var polygon in MapPolygons) {
                if (polygon.Circumcircle.Contains(mapPoint)) {
                    badTriangles.Add(polygon);

                    if (debug) {
                        foreach (var segment in polygon.MapSegments) {
                            segment.ShowHighlighted();
                        }
                    }
                }

                if (debug) {
                    polygon.ShowCircumcircle();
                    await Wait.ForDelay();
                    polygon.HideCircumcircle();
                }
            }

            var holeBoundaries = badTriangles.SelectMany(polygon => polygon.MapSegments).ToList();

            // Remove inner edges by finding other similar edges
            foreach (var holeSegment in holeBoundaries.ToList()) {
                var conflicts = holeBoundaries
                    .Where(o => o.EndPoints.Contains(holeSegment.MapPoint1) &&
                                o.EndPoints.Contains(holeSegment.MapPoint2))
                    .ToList();

                if (conflicts.Count > 1) {
                    foreach (var conflict in conflicts) {
                        if (debug) {
                            conflict.ShowHighlightedRand();
                            await Wait.ForDelay();
                        }

                        holeBoundaries.Remove(conflict);
                    }
                }
            }

            if (debug) {
                Debug.Assert(!holeBoundaries.Any(edge => edge.Contains(mapPoint) && mapPoint != edge.Point1 && mapPoint != edge.Point2),
                    $"Degenerate case.\nSeed: {Seed}\nIteration: {Iteration}",
                    "Edge contains point. A solution may involve splitting the edge at the point.");
            }

            try {
                var holeVertices = await ArrangeHoleBoundaries();

                // Remove polygons first so the segments/points don't get removed that are similar to the hole boundaries.
                foreach (var polygon in badTriangles) {
                    RemovePolygon(polygon);
                }

                // fill hole with new triangles.
                for (var i = 0; i < holeVertices.Count; i++) {
                    var j = (i + 1) % holeVertices.Count;

                    if (holeVertices[i] != mapPoint && holeVertices[j] != mapPoint) {
                        var polygon = new MapPolygon(mapPoint, holeVertices[i], holeVertices[j]);
                        AddPolygon(polygon);

                        if (debug) {
                            polygon.ShowPathSubdued();
                            await Wait.ForDelay();
                        }
                    }
                }
            }
            catch (InvalidOperationException e) { }

            async Task<IList<MapPoint>> ArrangeHoleBoundaries() {
                var copy = new List<MapSegment>(holeBoundaries);

                var nextSegment = copy[0];
                copy.Remove(nextSegment);

                var currentPoint = nextSegment.MapPoint1;
                var nextPoint = nextSegment.MapPoint2;

                var vertices = new List<MapPoint> {
                    currentPoint
                };

                while (copy.Count > 0) {
                    var nextSegments = copy.Where(o => o.EndPoints.Contains(nextPoint)).ToList();

                    // Rather frequently two non-adjacent circumcircles will capture a point,
                    // but the triangle between them will not capture it. This results in a degenerate
                    // hole boundary that needs to be ignored.
                    // I could try to fix this by finding any triangles where more than 1 edge is present
                    // in the other identified triangles.
                    if (nextSegments.Count != 1) {
                        Log.LogInformation($"Degenerate case.\nSeed: {Seed}\nIteration: {Iteration}");
                        throw new InvalidOperationException("Degenerate case");
                    }

                    nextSegment = nextSegments[0];
                    currentPoint = nextSegment.MapPoint1;
                    nextPoint = nextSegment.MapPoint2;

                    copy.Remove(nextSegment);
                    vertices.Add(currentPoint);
                }

                return vertices.Cast<MapPoint>().ToList();
            }

            if (debug) {
                mapPoint.Hide();
            }
        }

        if (debug) {
            Log.LogInformation($"Done {nameof(AddDelaunayTriangles)}");
            await Wait.For(1);
        }

        if (clearCanvas) {
            ClearCanvasArtifacts();
        }
    }

    async Task AddVoronoiEdgesFromCircumcircles(bool clearCanvas = true) {
        Log.LogTrace("Starting voronoi edges");

        var debug = false;
        Wait.Delay = 100;
        Wait.Pause = false;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(AddVoronoiEdgesFromCircumcircles)}");

            foreach (var polygon in MapPolygons) {
                polygon.ShowPathSubdued();
            }

            await Wait.For(1);
        }

        ClearPoints();

        foreach (var polygon in MapPolygons) {
            if (debug) {
                polygon.ShowCircumcircle();
            }

            foreach (var edge in polygon.MapSegments) {
                if (debug) {
                    edge.ShowHighlighted();
                    await Wait.ForDelay();
                }

                var neighbors = MapPolygons.Where(o => polygon != o && o.Edges.Any(oe => (edge.EndPoints.Contains(oe.Point1) && edge.EndPoints.Contains(oe.Point2)))).ToList();

                if (neighbors.Count > 1) {
                    Log.LogInformation($"Degenerate case.\nSeed: {Seed}\nIteration: {Iteration}");
                    await Wait.ForDelay();
                }

                //Debug.Assert(neighbors.Count <= 1,
                //    "Edges should never have more than 1 neighbor.");

                var neighbor = neighbors.FirstOrDefault();

                // This generates triangles that are far above/below/beside the border.
                // This is because the circumcircle center of the edge triangles is sometimes very far out.

                if (neighbor is null) {
                    continue;
                }

                var point1 = new MapPoint(polygon.Circumcircle.Center);
                AddPoint(ref point1);

                var point2 = new MapPoint(neighbor.Circumcircle.Center);
                AddPoint(ref point2);

                if (debug) {
                    point1.ShowHighlighted();
                    point2.ShowHighlighted();

                    if (neighbor is not null) {
                        neighbor.ShowCircumcircle();
                        neighbor.ShowPathHighlightedAlt();
                    }
                }

                // This differs from AddSegment because it checks both directions.
                var segment = MapSegments.FirstOrDefault(o => o.EndPoints.Contains(point1) && o.EndPoints.Contains(point2));

                if (segment is null) {
                    segment = AddSegment(point1, point2);

                    if (debug) {
                        segment.ShowHighlightedRand();
                        await Wait.ForDelay();
                    }
                }

                if (debug) {
                    if (neighbor is not null) {
                        neighbor.ShowPathSubdued();
                        neighbor.HideCircumcircle();
                    }

                    edge.Hide();
                }
            }

            if (debug) {
                polygon.HideCircumcircle();
            }
        }

        if (debug) {
            await Wait.For(1);

            foreach (var point in MapPoints) {
                point.Hide();
            }

            foreach (var segment in MapSegments) {
                segment.ShowSubdued();
            }

            foreach (var polygon in MapPolygons) {
                polygon.HidePath();
            }

            Log.LogInformation($"Done {nameof(AddVoronoiEdgesFromCircumcircles)}");
            await Wait.For(1);
        }

        if (clearCanvas) {
            ClearCanvasArtifacts();
        }
    }

    async Task AddMissingVoronoiEdges(bool clearCanvas = true) {
        Log.LogTrace("Adding missing voronoi edges");

        var debug = false;
        Wait.Delay = 100;
        Wait.Pause = false;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(AddMissingVoronoiEdges)}");

            foreach (var segment in MapSegments) {
                segment.ShowSubdued();
            }

            await Wait.For(1);
        }


        foreach (var point in MapPoints.ToList()) {
            // Ignore points outside of the border because those are healthy extremes.
            if (!Border.Contains(point)) {
                continue;
            }

            // Ignore vertices that have 3 lines. For some reason these don't point
            // to convex polys on the edge of the border.
            if (MapSegments.Count(o => o.EndPoints.Contains(point)) >= 3) {
                continue;
            }

            if (debug) {
                point.ShowHighlighted();
                await Wait.For(1);
            }

            foreach (var polygon in MapPolygons) {
                if (debug) {
                    polygon.ShowCircumcircle();

                    await Wait.ForDelay();
                }

                if (polygon.Circumcircle.Center.Equals(point)) {
                    if (debug) {
                        polygon.ShowPathHighlighted();
                        await Wait.For(1);
                    }

                    var segment = polygon.MapSegments.First(o1 => Border.MapSegments.Any(o2 => o2.Intersects(o1).IntersectionEnd is not null));

                    var slopeNegReciprocal = -1 / segment.Slope;
                    var distance = Math.Pow((polygon.Circumcircle.Center - segment.Midpoint).Magnitude, 2);

                    var deltaX = Math.Sqrt(distance / (1 + Math.Pow(slopeNegReciprocal, 2)));
                    var deltaY = slopeNegReciprocal * deltaX;

                    double startX, startY, endX, endY;

                    if (segment.Slope == 0) {
                        startX = polygon.Circumcircle.Center.X;
                        startY = polygon.Circumcircle.Center.Y;
                        endX = polygon.Circumcircle.Center.X;

                        if (polygon.Circumcircle.Center.Y > segment.Midpoint.Y) {
                            endY = polygon.Circumcircle.Center.Y - (2 * distance);
                        }
                        else {
                            endY = polygon.Circumcircle.Center.Y + (2 * distance);
                        }
                    }
                    else {
                        startX = segment.Midpoint.X + deltaX;
                        startY = segment.Midpoint.Y + deltaY;
                        endX = segment.Midpoint.X - deltaX;
                        endY = segment.Midpoint.Y - deltaY;
                    }

                    var bisector = AddSegment(new MapPoint(startX, startY), new MapPoint(endX, endY));

                    if (debug) {
                        bisector.ShowSubdued();
                        await Wait.ForDelay();
                        polygon.HidePath();
                    }
                }

                if (debug) {
                    polygon.HideCircumcircle();
                }
            }

            if (debug) {
                await Wait.For(1);
                point.Hide();
            }
        }

        ClearPolygons();
        ClearPoints();

        if (debug) {
            Log.LogInformation($"Done {nameof(AddMissingVoronoiEdges)}");
            await Wait.For(1);
        }

        if (clearCanvas) {
            ClearCanvasArtifacts();
        }
    }

    async Task ChopBorder(bool clearCanvas = true) {
        Log.LogTrace("Chopping borders");

        var debug = false;
        Wait.Delay = 100;
        Wait.Pause = false;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(ChopBorder)}");

            foreach (var segment in MapSegments) {
                segment.ShowSubdued();
            }

            await Wait.For(1);
        }

        // Find all segments where at least one point lies outside of the borders
        var externalSegments = MapSegments.Where(segment => !Border.Contains(segment.Point1) || !Border.Contains(segment.Point2) || Border.MapSegments.Any(o => o.Intersects(segment).Intersects)).ToList();

        foreach (var borderEdge in Border.MapSegments) {
            if (debug) {
                borderEdge.ShowHighlightedAlt();
            }

            var borderPoints = new List<MapPoint> {
                borderEdge.MapPoint1,
                borderEdge.MapPoint2,
            };

            foreach (var segment in externalSegments.ToList()) {
                if (debug) {
                    segment.ShowHighlighted();

                    await Wait.ForDelay();
                }

                var (intersects, intersection, intersectionEnd) = borderEdge.Intersects(segment);

                // The lines intersect but one does not contain the other
                if (intersects && intersectionEnd is null) {
                    var borderIntersect = new MapPoint(intersection);

                    if (!borderPoints.Contains(borderIntersect)) {
                        borderPoints.Add(borderIntersect);
                    }

                    // The line begins within the border
                    if (Border.Contains(segment.Point1) && segment.MapPoint1 != borderIntersect) {
                        var newSegment = AddSegment(segment.MapPoint1, borderIntersect);

                        if (debug) {
                            newSegment.ShowHighlightedRand();
                        }
                    }
                    // The line ends within the border
                    else if (Border.Contains(segment.Point2)) {
                        var newSegment = AddSegment(borderIntersect, segment.MapPoint2);

                        if (debug) {
                            newSegment.ShowHighlightedRand();
                        }
                    }
                    // The line cross over the border but starts and stops outside of it.
                    else {
                        var newSegment = AddSegment(borderIntersect, segment.MapPoint1.RightSideOfLine(borderEdge) ? segment.MapPoint2 : segment.MapPoint1);

                        // Add the newSegment to the list so that the next border will chop the other end.
                        externalSegments.Add(newSegment);

                        if (debug) {
                            newSegment.ShowSubdued();
                        }
                    }

                    externalSegments.Remove(segment);
                }
            }

            borderPoints = borderPoints.OrderBy(o => (o - borderEdge.Point1).Magnitude).ToList();

            // Add new segments across the border between all the intersections, creating a
            // multi-part border.
            // Subtract 1 so we don't loop the end back to the beginning.
            for (var i = 0; i < borderPoints.Count - 1; i++) {
                var j = (i + 1) % borderPoints.Count;

                var newBorderSegment = AddSegment(borderPoints[i], borderPoints[j]);

                if (debug) {
                    newBorderSegment.ShowSubdued();
                    await Wait.ForDelay();
                }
            }

            if (debug) {
                borderEdge.Hide();
            }
        }

        externalSegments = MapSegments.Where(segment => !Border.Contains(segment.Point1) || !Border.Contains(segment.Point2)).ToList();

        if (debug) {
            foreach (var segment in MapSegments) {
                segment.ShowSubdued();
            }

            foreach (var segment in externalSegments) {
                segment.ShowHighlighted();
            }

            await Wait.For(1);
        }

        // Remove all the segments that have at least 1 point outside of the border
        foreach (var segment in externalSegments) {
            RemoveSegment(segment);

            if (debug) {
                await Wait.ForDelay();
            }
        }

        if (debug) {
            foreach (var borderSegment in Border.MapSegments) {
                foreach (var segment in MapSegments.Where(o => o.Intersects(borderSegment).IntersectionEnd is not null).ToList()) {
                    segment.ShowHighlightedAlt();
                    await Wait.ForDelay();
                }

                Log.LogInformation($"{borderSegment} contains {MapSegments.Where(o => o.Intersects(borderSegment).IntersectionEnd is not null).Count()} segments");
            }

            Log.LogInformation($"Done {nameof(ChopBorder)}");
            await Wait.For(1);
        }

        if (clearCanvas) {
            ClearCanvasArtifacts();
        }
    }

    async Task FixBorderWinding(bool clearCanvas = true) {
        Log.LogTrace("Fixing border winding");

        var debug = false;
        Wait.Delay = 100;
        Wait.Pause = false;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(FixBorderWinding)}");

            foreach (var segment in MapSegments) {
                segment.ShowSubdued();
            }

            await Wait.For(1);
        }

        var sortedBorders = Border.MapSegments.OrderBy(o => o.Point1.Y).ThenBy(o => o.Point1.X).ToList();

        var borderStart = sortedBorders[0];
        var borderNext = sortedBorders[1];

        var mapSegmentsOnBorder = MapSegments.Where(o1 => Border.MapSegments.Any(o2 => o2.Intersects(o1).IntersectionEnd is not null));
        var currentSegment = mapSegmentsOnBorder.Where(o => o.EndPoints.Contains(borderStart.MapPoint1) && o.Intersects(borderStart).IntersectionEnd is not null).First();
        var startPoint = currentSegment.MapPoint1;

        if (currentSegment.Point1 == borderNext.Point1 || currentSegment.Point2 == borderNext.Point2) {
            currentSegment = FlipSegment(currentSegment);
            startPoint = currentSegment.MapPoint1;
        }

        if (debug) {
            startPoint.ShowHighlighted();

            await Wait.ForDelay();
        }

        while (true) {
            if (debug) {
                currentSegment.ShowHighlightedRand();

                await Wait.ForDelay();
            }

            if (currentSegment.Point2 == startPoint) {
                break;
            }

            var nextSegment = mapSegmentsOnBorder.First(o => o.EndPoints.Contains(currentSegment.Point2) && !o.EndPoints.Contains(currentSegment.Point1));

            if (currentSegment.Point1 == nextSegment.Point1 || currentSegment.Point2 == nextSegment.Point2) {
                nextSegment = FlipSegment(nextSegment);
            }

            currentSegment = nextSegment;
        }

        if (debug) {
            Log.LogInformation($"Done {nameof(FixBorderWinding)}");
            await Wait.For(1);

            startPoint.Hide();
        }

        if (clearCanvas) {
            ClearCanvasArtifacts();
        }

        MapSegment FlipSegment(MapSegment currentSegment) {
            var point1 = currentSegment.MapPoint2;
            var point2 = currentSegment.MapPoint1;

            RemoveSegment(currentSegment);

            return AddSegment(point1, point2);
        }
    }

    async Task FindPolygons() {
        Log.LogTrace("Finding polygons");

        var debug = false;
        Wait.Delay = 100;
        Wait.Pause = false;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(FindPolygons)}");
            await Wait.For(1);

            foreach (var segment in MapSegments) {
                segment.ShowSubdued();
            }

            await Wait.For(1);
        }

        var nonBorderSegments = MapSegments.Where(o1 => Border.MapSegments.All(o2 => o1.Intersects(o2).IntersectionEnd is null)).ToList();

        // Add the reverse of all segments except on borders
        foreach (var segment in nonBorderSegments) {
            AddSegment(segment.MapPoint2, segment.MapPoint1);

            if (debug) {
                segment.ShowHighlightedRand();
                await Wait.ForDelay();
            }
        }

        if (debug) {
            foreach (var segment in nonBorderSegments) {
                segment.ShowSubdued();
            }
        }

        while (MapSegments.Any()) {
            var currentSegment = MapSegments.First();
            var polygonSegments = new List<MapSegment>();

            await FindLeftestSegment(currentSegment, polygonSegments);

            var polygonVertices = new List<MapPoint>();

            foreach (var polygonSegment in polygonSegments) {
                if (!polygonVertices.Contains(polygonSegment.Point1)) {
                    polygonVertices.Add(polygonSegment.MapPoint1);
                }

                if (!polygonVertices.Contains(polygonSegment.Point2)) {
                    polygonVertices.Add(polygonSegment.MapPoint2);
                }

                RemoveSegment(polygonSegment);

                if (debug) {
                    await Wait.ForDelay();
                }
            }

            if (polygonVertices.Count < 3) {
                if (debug) {
                    foreach (var point in polygonVertices) {
                        point.ShowHighlighted();
                    }

                    Log.LogInformation($"Degenerate case.\nSeed: {Seed}\nIteration: {Iteration}");
                    await Wait.ForContinue();

                    //Debug.Assert(polygonVertices.Count >= 3,
                    //    $"Degenerate case.\nSeed: {Seed}\nIteration: {Iteration}");
                }

                break;
            }

            var polygon = new MapPolygon(polygonVertices);

            AddPolygon(polygon);

            if (debug) {
                polygon.ShowPolygon();
                //AddText($"{MapPolygons.Count}: {polygon.VertexCount}", polygon.Centroid.X, polygon.Centroid.Y);

                await Wait.ForDelay();
            }
        }

        void AddText(string content, double left, double bottom) {
            var textBlock = new TextBlock {
                FontSize = 14,
                Text = content
            };

            Microsoft.UI.Xaml.Controls.Canvas.SetTop(textBlock, bottom);
            Microsoft.UI.Xaml.Controls.Canvas.SetLeft(textBlock, left + 15);

            textBlock.RenderTransform = new TransformGroup {
                Children = new TransformCollection {
                new TranslateTransform {
                    X = -15,
                    Y = -14
                },
                new ScaleTransform {
                   ScaleY = -1
                }
            }
            };

            Canvas.Children.Add(textBlock);
        }

        async Task FindLeftestSegment(MapSegment currentSegment, List<MapSegment> polygonSegments) {
            polygonSegments.Add(currentSegment);

            var otherSegments = MapSegments.Where(o => o.Point1 == currentSegment.Point2 && o.Point2 != currentSegment.Point1).ToList();

            if (debug) {
                currentSegment.ShowHighlighted();

                foreach (var otherSegment in otherSegments) {
                    otherSegment.ShowHighlightedRand();
                }

                await Wait.ForDelay();
            }

            MapSegment? nextSegment = default;
            var farthestLeft = double.MinValue;

            // Finds the left-most segment to continue mapping the polygon.
            // I chose left for positive winding, no other reason.
            foreach (var otherSegment in otherSegments) {
                if (debug) {
                    otherSegment.ShowHighlightedAlt();

                    Log.LogInformation($"Count: {otherSegments.Count}");

                    await Wait.ForDelay();
                }

                // We have completed the polygon
                if (polygonSegments.Contains(otherSegment)) {
                    if (debug) {
                        otherSegment.ShowSubdued();
                    }

                    break;
                }

                var farPointCross = Convert.ToSingle(otherSegment.Point2.NearLine(currentSegment));

                // If both points happen to be on the right side of the line, if I ever support
                // concave polygons then this will ensure that the leftmost rotated segment is still
                // selected. Also solves degenerate case with colinears.
                var currentAngle = currentSegment.AngleTo(otherSegment);
                var otherSegmentLeftness = currentAngle * (farPointCross < 0 ? -1 : 1);

                if (otherSegmentLeftness > farthestLeft) {
                    if (debug) {
                        Log.LogInformation($"Leftness {otherSegmentLeftness} > current {farthestLeft}");
                        await Wait.ForDelay();
                    }

                    farthestLeft = otherSegmentLeftness;
                    nextSegment = otherSegment;
                }

                if (debug) {
                    otherSegment.ShowSubdued();
                }
            }

            if (nextSegment is null) {
                return;
            }

            await FindLeftestSegment(nextSegment, polygonSegments);
        }
    }

    #endregion

    #region Polygons

    void AddPolygon(MapPolygon polygon) {
        MapPolygons.Add(polygon);

        Canvas.Children.Add(polygon.CanvasCircumcircle);
        Canvas.Children.Add(polygon.CanvasPolygon);
        Canvas.Children.Add(polygon.CanvasPath);

        foreach (var point in polygon.MapPoints) {
            if (!Canvas.Children.Contains(point.CanvasPoint)) {
                Canvas.Children.Add(point.CanvasPoint);
            }
        }

        foreach (var segment in polygon.MapSegments) {
            Canvas.Children.Add(segment.CanvasPath);
        }
    }

    void HidePolygons() {
        foreach (var polygon in MapPolygons.ToList()) {
            polygon.HideCircumcircle();
            polygon.HidePath();
            polygon.HidePolygon();
        }
    }

    void ClearPolygons() {
        foreach (var polygon in MapPolygons.ToList()) {
            RemovePolygon(polygon);
        }

        Debug.Assert(MapPolygons.Count == 0);
    }

    void RemovePolygon(MapPolygon polygon) {
        Canvas.Children.Remove(polygon.CanvasPolygon);
        Canvas.Children.Remove(polygon.CanvasPath);
        Canvas.Children.Remove(polygon.CanvasCircumcircle);

        foreach (var point in polygon.MapPoints.Where(o => !MapPoints.Contains(o))) {
            Canvas.Children.Remove(point.CanvasPoint);
        }

        foreach (var segment in polygon.MapSegments.Where(o => !MapSegments.Contains(o))) {
            Canvas.Children.Remove(segment.CanvasPath);
        }

        if (!MapPolygons.Remove(polygon)) {
            throw new InvalidOperationException("Polygon not found in MapPolygons");
        }
    }

    #endregion

    #region Segments

    MapSegment AddSegment(MapPoint point1, MapPoint point2) {
        if (point1 == point2) {
            throw new ArgumentException("The points must be different.");
        }

        AddPoint(ref point1);
        AddPoint(ref point2);

        // Don't look for reverse, because we need to add reverse segments later
        var segment = MapSegments.FirstOrDefault(o => o.Point1 == point1 && o.Point2 == point2);

        if (segment is null) {
            segment = new MapSegment(point1, point2);
            MapSegments.Add(segment);
            Canvas.Children.Add(segment.CanvasPath);
        }

        return segment;
    }

    void HideSegments() {
        foreach (var segment in MapSegments.ToList()) {
            segment.Hide();
        }
    }

    void ClearSegments() {
        foreach (var segment in MapSegments.ToList()) {
            RemoveSegment(segment);
        }

        Debug.Assert(MapSegments.Count == 0);
    }

    void RemoveSegment(MapSegment segment) {
        Canvas.Children.Remove(segment.CanvasPath);

        if (!MapSegments.Remove(segment)) {
            throw new InvalidOperationException("Segment not found in MapSegments");
        }
    }

    #endregion

    #region Points

    void AddPoint(ref MapPoint point) {
        var existingPoint = MapPoints.FirstOrDefault(point.Equals);

        if (existingPoint is null) {
            MapPoints.Add(point);

            if (!Canvas.Children.Contains(point.CanvasPoint)) {
                Canvas.Children.Add(point.CanvasPoint);
            }
        }
        else {
            point = existingPoint;
        }
    }

    void HidePoints() {
        foreach (var point in MapPoints.ToList()) {
            point.Hide();
        }
    }

    void ClearPoints() {
        foreach (var point in MapPoints.ToList()) {
            RemovePoint(point);
        }

        Debug.Assert(MapPoints.Count == 0);
    }

    void RemovePoint(MapPoint point) {
        Canvas.Children.Remove(point.CanvasPoint);

        var preCount = MapPoints.Count;

        if (!MapPoints.Remove(point)) {
            throw new InvalidOperationException("Point not found in MapPoints");
        }

        Debug.Assert(preCount == MapPoints.Count + 1);
    }

    #endregion
}
