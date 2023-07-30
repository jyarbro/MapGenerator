using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Nrrdio.MapGenerator.Services.Models;
using System.Diagnostics;

namespace Nrrdio.MapGenerator.Services;

public class VoronoiGenerator : GeneratorBase, IGenerator {
    public VoronoiGenerator(ILogger<GeneratorBase> log) : base(log) { }

    public async Task Generate(int points, IEnumerable<MapPoint> borderVertices) {
        if (!Initialized) {
            throw new InvalidOperationException("Must initialize first");
        }

        await GenerateWithReturn(points, borderVertices);
    }

    public async Task<IEnumerable<MapPolygon>> GenerateWithReturn(int points, IEnumerable<MapPoint> borderVertices) {
        Log.LogTrace(nameof(Generate));

        var debug = true;

        if (!Initialized) {
            throw new InvalidOperationException("Must initialize first");
        }

        Iteration++;

        if (debug) {
            Log.LogInformation($"Seed: {Seed}");

            Log.LogInformation($"Move to other monitor now. Iteration {Iteration}");
            await WaitForContinue();
        }

        await Clear(); 

        PointCount = points;
        Border = new MapPolygon(borderVertices);

        Border.ShowPathSubdued();

        GeneratePoints();

        await AddBorderTriangles();

        if (debug) {
            ResetCanvas();
        }

        await AddDelaunayTriangles();

        if (debug) {
            ResetCanvas();
        }

        await AddVoronoiEdgesFromCircumcircles();

        if (debug) {
            ResetCanvas();
        }

        await AddMissingVoronoiEdges();

        if (debug) {
            ResetCanvas();
        }

        await ChopBorder();

        if (debug) {
            ResetCanvas();
        }

        await FixBorderWinding();

        if (debug) {
            ResetCanvas();
        }

        //await CheckSegments();

        if (debug) {
            ResetCanvas();
        }

        await FindPolygons();
        
        Log.LogInformation($"Total polygons: {MapPolygons.Count}");
        Log.LogInformation("Done");

        return MapPolygons;
    }

    void ResetCanvas() {
        foreach (var point in MapPoints) {
            point.Hide();
        }

        foreach (var segment in MapSegments) {
            segment.ShowSubdued();
        }

        foreach (var polygon in MapPolygons) {
            polygon.HideCircumcircle();
            polygon.HidePath();
            polygon.HidePolygon();
        }
    }

    async Task DebugCheckSegments() {
        Log.LogInformation("Checking Segments");

        var borderSegments = MapSegments.Where(o1 => Border.MapSegments.Any(o2 => o1.Intersects(o2).IntersectionEnd is not null)).ToList();
        var nonBorderSegments = MapSegments.Where(o1 => Border.MapSegments.All(o2 => o1.Intersects(o2).IntersectionEnd is null)).ToList();

        Debug.Assert(MapSegments.Count == borderSegments.Count + nonBorderSegments.Count);

        foreach (var segment in borderSegments) {
            segment.ShowHighlightedRand();
        }

        await WaitForContinue();

        foreach (var segment in borderSegments) {
            segment.ShowSubdued();
        }

        foreach (var segment in nonBorderSegments) {
            segment.ShowHighlightedRand();
        }

        await WaitForContinue();
    }

    async Task AddBorderTriangles() {
        Log.LogTrace("Adding border triangles");

        var debug = false;
        //var debug = Iteration > 1;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(AddBorderTriangles)}");
        }

        var borderVertices = Border.Vertices.Count;
        int j;

        var centroid = new MapPoint(Border.Centroid);
        AddPoint(centroid);

        if (debug) {
            centroid.ShowHighlighted();

            await Task.Delay(100);
        }

        var borderPoints = Border.MapPoints.ToList();

        for (var i = 0; i < borderVertices; i++) {
            j = (i + 1) % borderVertices;

            var triangle = new MapPolygon(centroid, borderPoints[i], borderPoints[j]);
            AddPolygon(triangle);
            AddPoint(borderPoints[i]);

            if (debug) {
                triangle.ShowPathSubdued();
                borderPoints[i].ShowSubdued();

                await Task.Delay(100);
            }
        }

        if (debug) {
            Log.LogInformation($"Done {nameof(AddBorderTriangles)}");
            await WaitForContinue();
        }
    }

    // https://www.codeguru.com/cplusplus/delaunay-triangles/
    async Task AddDelaunayTriangles() {
        Log.LogTrace("Adding delaunay triangles");

        var debug = false;
        //var debug = Iteration > 1;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(AddDelaunayTriangles)}");

            OutputCanvas.Children.Add(Border.CanvasPath);
            Border.ShowPathSubduedAlt();

            //await WaitForContinue();
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
                        //polygon.ShowCircumcircle();

                        foreach (var segment in polygon.MapSegments) {
                            segment.ShowHighlighted();
                        }

                        await Task.Delay(100);
                        //await WaitForContinue();
                    }
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

                            await Task.Delay(100);
                            //await WaitForContinue();
                        }

                        holeBoundaries.Remove(conflict);
                    }
                }
            }

            // DEGENERATE - Edge contains point - A solution may involve splitting the edge at the point.
            Debug.Assert(!holeBoundaries.Any(edge => edge.Contains(mapPoint) && mapPoint != edge.Point1 && mapPoint != edge.Point2));

            // DEGENERATE - This fails sometimes where too many segments come out of one vertex.
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
                        polygon.ShowPathHighlighted();

                        await Task.Delay(100);
                        //await WaitForContinue();

                        polygon.ShowPathSubdued();
                    }
                }
            }

            mapPoint.ShowSubdued();

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

                    if (nextSegments.Count != 1) {
                        Log.LogError("BAD SITUATION.");
                        Log.LogInformation($"Seed: {Seed}");

                        foreach (var polygon in MapPolygons) {
                            polygon.ShowPolygon();
                        }

                        foreach (var segment in holeBoundaries) {
                            segment.ShowHighlighted();
                        }

                        foreach (var segment in nextSegments) {
                            segment.ShowHighlightedRand();
                        }

                        await WaitForContinue();
                    }

                    Debug.Assert(nextSegments.Count == 1);

                    nextSegment = nextSegments[0];
                    currentPoint = nextSegment.MapPoint1;
                    nextPoint = nextSegment.MapPoint2;

                    copy.Remove(nextSegment);
                    vertices.Add(currentPoint);
                }

                return vertices.Cast<MapPoint>().ToList();
            }
        }

        if (debug) {
            Log.LogInformation($"Done {nameof(AddDelaunayTriangles)}");
            await WaitForContinue();
            OutputCanvas.Children.Remove(Border.CanvasPath);
        }
    }

    async Task AddVoronoiEdgesFromCircumcircles() {
        Log.LogTrace("Starting voronoi edges");

        var debug = false;
        //var debug = Iteration > 1;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(AddVoronoiEdgesFromCircumcircles)}");

            OutputCanvas.Children.Add(Border.CanvasPath);
            Border.ShowPathHighlighted();

            foreach (var polygon in MapPolygons) {
                polygon.ShowPathSubdued();
            }

            //await WaitForContinue();
        }

        ClearPoints();

        foreach (var polygon in MapPolygons) {
            if (debug) {
                polygon.ShowCircumcircle();
            }

            foreach (var edge in polygon.MapSegments) {
                if (debug) {
                    edge.ShowHighlighted();

                    await Task.Delay(10);
                }

                var neighbors = MapPolygons.Where(o => polygon != o && o.Edges.Any(oe => (edge.EndPoints.Contains(oe.Point1) && edge.EndPoints.Contains(oe.Point2)))).ToList();

                // Edges should never have more than 1 neighbor.
                Debug.Assert(neighbors.Count <= 1);

                var neighbor = neighbors.FirstOrDefault();

                // This generates triangles that are far above/below/beside the border.
                // This is because the circumcircle center of the edge triangles is sometimes very far out.

                if (neighbor is null) {
                    continue;
                }

                var point1 = AddPoint(polygon.Circumcircle.Center);
                var point2 = AddPoint(neighbor.Circumcircle.Center);

                if (debug) {
                    point1.ShowHighlighted();
                    point2.ShowHighlighted();

                    if (neighbor is not null) {
                        neighbor.ShowCircumcircle();
                    }
                }

                // This differs from AddSegment because it checks both directions.
                var segment = MapSegments.FirstOrDefault(o => o.EndPoints.Contains(point1) && o.EndPoints.Contains(point2));

                if (segment is null) {
                    segment = AddSegment(point1, point2);

                    if (debug) {
                        segment.ShowHighlightedRand();

                        await Task.Delay(100);
                        //await WaitForContinue();
                    }
                }

                if (debug && neighbor is not null) {
                    OutputCanvas.Children.Remove(neighbor.CanvasCircumcircle);
                    neighbor.ShowPathSubdued();
                }
            }

            if (debug) {
                OutputCanvas.Children.Remove(polygon.CanvasCircumcircle);
                await Task.Delay(10);
            }
        }

        if (debug) {
            Log.LogInformation($"Done {nameof(AddVoronoiEdgesFromCircumcircles)}");
            await WaitForContinue();
            OutputCanvas.Children.Remove(Border.CanvasPath);
        }
    }

    async Task AddMissingVoronoiEdges() {
        Log.LogTrace("Adding missing voronoi edges");

        var debug = false;
        //var debug = Iteration > 1;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(AddMissingVoronoiEdges)}");

            OutputCanvas.Children.Add(Border.CanvasPath);
            Border.ShowPathHighlighted();

            //await WaitForContinue();
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
                point.ShowHighlightedAlt();
            }

            foreach (var polygon in MapPolygons) {
                if (polygon.Circumcircle.Center.Equals(point)) {
                    if (debug) {
                        polygon.ShowPathHighlighted();
                        await Task.Delay(100);
                    }

                    var segment = polygon.MapSegments.First(o1 => Border.MapSegments.Any(o2 => o2.Intersects(o1).IntersectionEnd is not null));

                    var slopeNegReciprocal = -1 / segment.Slope;
                    var distance = Math.Pow((polygon.Circumcircle.Center - segment.Midpoint).Magnitude, 2);

                    var deltaX = Math.Sqrt(distance / (1 + Math.Pow(slopeNegReciprocal, 2)));
                    var deltaY = slopeNegReciprocal * deltaX;

                    var startX = segment.Midpoint.X + deltaX;
                    var startY = segment.Midpoint.Y + deltaY;
                    var endX = segment.Midpoint.X - deltaX;
                    var endY = segment.Midpoint.Y - deltaY;

                    var bisector = AddSegment(new MapPoint(startX, startY), new MapPoint(endX, endY));

                    if (debug) {
                        bisector.ShowHighlighted();
                    }
                }
            }
        }

        if (debug) {
            Log.LogInformation($"Done {nameof(AddMissingVoronoiEdges)}");
            await WaitForContinue();
            OutputCanvas.Children.Remove(Border.CanvasPath);
        }
    }

    async Task ChopBorder() {
        Log.LogTrace("Chopping borders");

        var debug = false;
        //var debug = Iteration > 1;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(ChopBorder)}");
            await WaitForContinue();
        }

        // Find all segments where at least one point lies outside of the borders
        var externalSegments = MapSegments.Where(segment => !Border.Contains(segment.Point1) || !Border.Contains(segment.Point2) || Border.MapSegments.Any(o => o.Intersects(segment).Intersects)).ToList();

        foreach (var borderEdge in Border.MapSegments) {
            var borderPoints = new List<MapPoint> {
                borderEdge.MapPoint1,
                borderEdge.MapPoint2,
            };

            foreach (var segment in externalSegments) {
                segment.ShowHighlighted();

                if (debug) {
                    await Task.Delay(10);
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
                        newSegment.ShowHighlightedRand();
                    }
                    // The line ends within the border
                    else if (Border.Contains(segment.Point2)) {
                        var newSegment = AddSegment(borderIntersect, segment.MapPoint2);
                        newSegment.ShowHighlightedRand();
                    }
                    // The line cross over the border but starts and stops outside of it.
                    else {
                        // We don't know which part of the split line crosses the other border, so try both.
                        // This is unfortunately run twice, once for each border it crosses.
                        // I could possibly figure out if left or right is "inside" and then use that to 
                        // determine which point needs to be checked for intersecting.

                        var part1 = new MapSegment(segment.MapPoint1, borderIntersect);
                        var part2 = new MapSegment(borderIntersect, segment.MapPoint2);

                        foreach (var otherBorder in Border.MapSegments.Where(o => o != borderEdge)) {
                            var otherIntersect = part1.Intersects(otherBorder);

                            if (otherIntersect.Intersects && !part1.EndPoints.Contains(otherIntersect.Intersection)) {
                                var newSegment = AddSegment(borderIntersect, new MapPoint(otherIntersect.Intersection));
                                newSegment.ShowHighlightedRand();
                            }

                            otherIntersect = part2.Intersects(otherBorder);

                            if (otherIntersect.Intersects && !part2.EndPoints.Contains(otherIntersect.Intersection)) {
                                var newSegment = AddSegment(borderIntersect, new MapPoint(otherIntersect.Intersection));
                                newSegment.ShowHighlightedRand();
                            }
                        }
                    }

                    if (debug) {
                        await Task.Delay(10);
                    }
                }

                segment.ShowSubdued();

                if (debug) {
                    await Task.Delay(10);
                }
            }

            borderPoints = borderPoints.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();

            // Add new segments across the border between all the intersections, creating a
            // multi-part border.
            // Subtract 1 so we don't loop the end back to the beginning.
            for (var i = 0; i < borderPoints.Count - 1; i++) {
                var j = (i + 1) % borderPoints.Count;

                var newSegment = AddSegment(borderPoints[i], borderPoints[j]);
                newSegment.ShowSubdued();

                if (debug) {
                    await Task.Delay(10);
                }
            }

            if (debug) {
                //await WaitForContinue();
            }
        }

        foreach (var segment in MapSegments) {
            segment.ShowSubdued();
        }

        externalSegments = MapSegments.Where(segment => !Border.Contains(segment.Point1) || !Border.Contains(segment.Point2)).ToList();

        if (debug) {
            foreach (var segment in externalSegments) {
                segment.ShowHighlighted();
            }

            await WaitForContinue();
        }

        // Remove all the segments that have at least 1 point outside of the border
        foreach (var segment in externalSegments) {
            await RemoveSegment(segment);

            if (debug) {
                await Task.Delay(100);
            }
        }

        //foreach (var borderSegment in Border.MapSegments) {
        //    foreach (var segment in MapSegments.Where(o => o.Intersects(borderSegment).IntersectionEnd is not null).ToList()) {
        //        segment.ShowHighlightedAlt();
        //        await Task.Delay(100);
        //    }

        //    Log.LogInformation($"{borderSegment} contains {MapSegments.Where(o => o.Intersects(borderSegment).IntersectionEnd is not null).Count()} segments");
        //}

        if (debug) {
            Log.LogInformation($"Done {nameof(ChopBorder)}");
            await WaitForContinue();
            OutputCanvas.Children.Remove(Border.CanvasPath);
        }
    }

    async Task FixBorderWinding() {
        Log.LogTrace("Fixing border winding");

        var debug = false;
        //var debug = Iteration > 1;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(FixBorderWinding)}");
        }

        // Grab any horizontal border segment, lowest first
        var borderStart = Border.MapSegments.Where(o => o.Point2.X != o.Point1.X).OrderBy(o => o.Point1.Y).First();

        var startPoint = borderStart.MapPoint1;
        var startSegments = MapSegments.Where(o => o.EndPoints.Contains(startPoint));
        var currentSegment = startSegments.Where(o => o.Intersects(borderStart).IntersectionEnd is not null).First();

        // Segment is reversed, so flip it.
        if (currentSegment.Point1.X > currentSegment.Point2.X) {
            currentSegment = await FlipSegment(currentSegment);
            startPoint = currentSegment.MapPoint1;
        }

        if (debug) {
            startPoint.ShowHighlighted();
        }

        while (true) {
            if (debug) {
                currentSegment.ShowHighlighted();
            }

            var otherSegments = MapSegments.Where(o =>
                o.EndPoints.Contains(currentSegment.Point2) && !o.EndPoints.Contains(currentSegment.Point1)
            ).ToList();

            if (debug) {
                foreach (var otherSegment in otherSegments) {
                    otherSegment.ShowHighlighted();
                }
            }

            var nextSegment = currentSegment;
            var bestAngle = float.MaxValue;

            foreach (var otherSegment in otherSegments.ToList()) {
                if (debug) {
                    otherSegment.ShowHighlightedAlt();
                }

                var farPoint = otherSegment.Point2;
                var alignedOtherSegment = otherSegment;

                // otherSegment is reversed
                if (farPoint == currentSegment.Point2) {
                    alignedOtherSegment = await FlipSegment(otherSegment);
                    otherSegments.Add(alignedOtherSegment);

                    if (debug) {
                        alignedOtherSegment.ShowHighlightedAlt();
                    }

                    farPoint = alignedOtherSegment.Point2;
                }

                var otherSegmentAngle = currentSegment.AngleTo(alignedOtherSegment);

                if (otherSegmentAngle < bestAngle) {
                    if (debug) {
                        Log.LogInformation($"Other {otherSegmentAngle} < Best {bestAngle}");
                        
                        await Task.Delay(100);
                        //await WaitForContinue();
                    }

                    bestAngle = otherSegmentAngle;
                    nextSegment = alignedOtherSegment;
                }

                if (debug) {
                    alignedOtherSegment.ShowHighlighted();
                    await Task.Delay(0);
                }
            }

            if (debug) {
                currentSegment.ShowSubdued();

                foreach (var otherSegment in otherSegments) {
                    otherSegment.ShowSubdued();
                }

                await Task.Delay(100);
                //await WaitForContinue();
            }

            currentSegment = nextSegment;

            if (nextSegment.Point2 == startPoint) {
                break;
            }
        }

        if (debug) {
            Log.LogInformation($"Done {nameof(FixBorderWinding)}");
            await WaitForContinue();
            OutputCanvas.Children.Remove(Border.CanvasPath);
        }

        async Task<MapSegment> FlipSegment(MapSegment currentSegment) {
            var point1 = currentSegment.MapPoint2;
            var point2 = currentSegment.MapPoint1;

            await RemoveSegment(currentSegment);

            return AddSegment(point1, point2);
        }
    }

    async Task FindPolygons() {
        Log.LogTrace("Finding polygons");

        //var debug = false;
        var debug = Iteration > 1;

        if (debug) {
            Log.LogInformation($"Debugging {nameof(FixBorderWinding)}");
        }

        var nonBorderSegments = MapSegments.Where(o1 => Border.MapSegments.All(o2 => o1.Intersects(o2).IntersectionEnd is null)).ToList();

        // Add the reverse of all segments except on borders
        foreach (var segment in nonBorderSegments) {
            AddSegment(segment.MapPoint2, segment.MapPoint1);
        }

        ClearPolygons();

        while (MapSegments.Any()) {
            var currentSegment = MapSegments.First();
            currentSegment.ShowHighlighted();

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

                await RemoveSegment(polygonSegment);
            }

            if (debug && polygonVertices.Count < 3) {
                // we about to crash
                await WaitForContinue();
            }

            Debug.Assert(polygonVertices.Count >= 3);

            var polygon = new MapPolygon(polygonVertices);
            
            AddPolygon(polygon);

            if (debug) {
                polygon.ShowPolygon();
                AddText($"{MapPolygons.Count}: {polygon.VertexCount}", polygon.Centroid.X, polygon.Centroid.Y);

                await Task.Delay(100);
                //await WaitForContinue();
            }
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

        async Task FindLeftestSegment(MapSegment currentSegment, List<MapSegment> polygonSegments) {
            currentSegment.ShowHighlighted();

            polygonSegments.Add(currentSegment);

            var otherSegments = MapSegments.Where(o => o.Point1 == currentSegment.Point2 && o.Point2 != currentSegment.Point1).ToList();

            foreach (var otherSegment in otherSegments) {
                otherSegment.ShowHighlightedRand();
            }

            MapSegment? nextSegment = default;
            var farthestLeft = float.MinValue;

            // Finds the left-most segment to continue mapping the polygon.
            // I chose left for positive winding, no other reason.
            foreach (var otherSegment in otherSegments) {
                otherSegment.ShowHighlightedAlt();

                if (debug) {
                    await Task.Delay(100);
                    //await WaitForContinue();
                }

                // We have completed the polygon
                if (polygonSegments.Contains(otherSegment)) {
                    otherSegment.ShowSubdued();
                    break;
                }

                var farPointCross = Convert.ToSingle(otherSegment.Point2.NearLine(currentSegment));

                // If both points happen to be on the right side of the line, if I ever support
                // convex polygons then this will ensure that the leftmost rotated segment is still
                // selected. Also solves degenerate case with colinears.
                var currentAngle = currentSegment.AngleTo(otherSegment);
                var otherSegmentLeftness = currentAngle * (farPointCross < 0 ? -1 : 1);

                if (otherSegmentLeftness > farthestLeft) {
                    farthestLeft = otherSegmentLeftness;
                    nextSegment = otherSegment;
                }

                otherSegment.ShowSubdued();
            }

            if (nextSegment is null) {
                return;
            }

            await FindLeftestSegment(nextSegment, polygonSegments);
        }
    }
}
