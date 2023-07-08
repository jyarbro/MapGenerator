using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Nrrdio.MapGenerator.Services.Models;
using System.Diagnostics;

namespace Nrrdio.MapGenerator.Services;

public class DelaunayVoronoiGenerator : GeneratorBase, IGenerator {

    public DelaunayVoronoiGenerator(ILogger<GeneratorBase> log) : base(log) { }

    public async Task Generate(int points, IEnumerable<MapPoint> borderVertices) {
        if (!Initialized) {
            throw new InvalidOperationException("Must initialize first");
        }

        await WaitForContinue();
        await GenerateWithReturn(points, borderVertices);
    }

    public async Task<IEnumerable<MapPolygon>> GenerateWithReturn(int points, IEnumerable<MapPoint> borderVertices) {
        Log.LogTrace(nameof(Generate));
        
        if (!Initialized) {
            throw new InvalidOperationException("Must initialize first");
        }

        await Clear();

        PointCount = points;
        Border = new MapPolygon(borderVertices);

        GeneratePoints();
        AddBorderTriangles();
        AddDelaunayTriangles();
        
        await AddVoronoiEdges();
        ResetMapSegments();
        
        await ChopBorder();
        ResetMapSegments();
        
        await FixBorderWinding();
        ResetMapSegments();
        
        //await CheckSegments();
        ResetMapSegments();

        await FindPolygons();
        
        foreach (var segment in MapPolygons.SelectMany(o => o.Edges).Cast<MapSegment>()) {
            OutputCanvas.Children.Add(segment.CanvasPath);
            segment.ShowSubdued();
        }

        Log.LogInformation($"Total polygons: {MapPolygons.Count}");
        Log.LogInformation("Done");

        return MapPolygons;
    }

    void ResetMapSegments() {
        foreach (var segment in MapSegments) {
            segment.ShowSubdued();
        }
    }

    void AddBorderTriangles() {
        Log.LogTrace("Adding border triangles");

        var borderVertices = Border.Vertices.Count;
        int j;

        var centroid = new MapPoint(Border.Centroid);
        AddPoint(centroid);

        var borderPoints = Border.MapPoints.ToList();

        for (var i = 0; i < borderVertices; i++) {
            j = (i + 1) % borderVertices;

            var triangle = new MapPolygon(centroid, borderPoints[i], borderPoints[j]);
            AddPolygon(triangle);

            AddPoint(borderPoints[i]);
        }
    }

    // https://www.codeguru.com/cplusplus/delaunay-triangles/
    void AddDelaunayTriangles() {
        Log.LogTrace("Adding delaunay triangles");

        foreach (var mapPoint in MapPoints) {
            mapPoint.Highlight();

            //Use this block when bad triangles logic is okay.
            //var badTriangles = MapPolygons.Where(polygon => polygon.Circumcircle.Contains(mapPoint)).ToList();

            //Use this block instead when debugging bad triangles.
            var badTriangles = new List<MapPolygon>();

            foreach (var mapPolygon in MapPolygons) {
                if (mapPolygon.Circumcircle.Contains(mapPoint)) {
                    mapPolygon.ShowPath();
                    mapPolygon.ShowCircumcircle();
                    badTriangles.Add(mapPolygon);
                }
            }

            var holeBoundaries = badTriangles.SelectMany(polygon => polygon.MapSegments).ToList();

            // Remove inner edges by finding other similar edges
            foreach (var holeSegment in holeBoundaries.ToList()) {
                var conflicts = holeBoundaries.Where(o => 
                    (o.Point1 == holeSegment.Point1 && o.Point2 == holeSegment.Point2) ||
                    (o.Point1 == holeSegment.Point2 && o.Point2 == holeSegment.Point1))
                    .ToList();

                if (conflicts.Count > 1) {
                    foreach (var conflict in conflicts) {
                        holeBoundaries.Remove(conflict);
                    }
                }
            }

            // DEGENERATE - Edge contains point - A solution may involve splitting the edge at the point and doing other smart stuff.
            Debug.Assert(!holeBoundaries.Any(edge => edge.Contains(mapPoint) && mapPoint != edge.Point1 && mapPoint != edge.Point2));

            var holeVertices = MapSegment.ArrangedVertices(holeBoundaries);

            // fill hole with new triangles.
            for (var i = 0; i < holeVertices.Count; i++) {
                var j = (i + 1) % holeVertices.Count;

                if (holeVertices[i] != mapPoint && holeVertices[j] != mapPoint) {
                    var triangle = new MapPolygon(mapPoint, holeVertices[i], holeVertices[j]);
                    AddPolygon(triangle);
                }
            }

            foreach (var mapPolygon in badTriangles) {
                RemovePolygon(mapPolygon);
            }

            mapPoint.Subdue();
        }
    }

    async Task AddVoronoiEdges() {
        Log.LogTrace("Starting voronoi edges");

        var delaunayTriangles = new List<MapPolygon>(MapPolygons);

        // Erase everything and start over from the copy of the delaunay triangle list
        OutputCanvas.Children.Clear();
        MapPolygons.Clear();
        MapSegments.Clear();
        MapPoints.Clear();

        foreach (var triangle in delaunayTriangles) {
            foreach (MapSegment edge in triangle.Edges) {
                var neighbors = delaunayTriangles.Where(o => triangle != o && o.Edges.Any(oe => (edge.Point1 == oe.Point1 && edge.Point2 == oe.Point2) || (edge.Point1 == oe.Point2 && edge.Point2 == oe.Point1)));

                // This will often generate triangles that are far above/below/beside the border.
                // This is because the circumcircle center of the edge triangles is sometimes very far out.
                // This could perhaps be avoided if the border corners aren't used, and instead
                // if we generated random points outside the border area.
                foreach (var neighbor in neighbors) {
                    var point1 = MapPoints.FirstOrDefault(point => point == triangle.Circumcircle.Center);
                    point1 ??= new MapPoint(triangle.Circumcircle.Center);

                    var point2 = MapPoints.FirstOrDefault(point => point == neighbor.Circumcircle.Center);
                    point2 ??= new MapPoint(neighbor.Circumcircle.Center);

                    var segment = MapSegments.FirstOrDefault(o => o.EndPoints.Contains(point1) && o.EndPoints.Contains(point2));

                    // Check if there's a segment going in either direction. We will duplicate it later.
                    if (segment is null) {
                        segment = AddSegment(point1, point2);
                        segment.ShowHighlightedRand();
                    }
                }
            }
        }
    }

    async Task ChopBorder() {
        Log.LogTrace("Chopping borders");

        // Find all segments where at least one point lies outside of the borders
        var externalSegments = MapSegments.Where(segment => !Border.Contains(segment.Point1) || !Border.Contains(segment.Point2)).ToList();

        foreach (var borderEdge in Border.MapSegments) {
            var borderPoints = new List<MapPoint> {
                borderEdge.MapPoint1,
                borderEdge.MapPoint2
            };

            foreach (var segment in externalSegments) {
                segment.ShowHighlighted();

                var (intersects, intersection, intersectionEnd) = borderEdge.Intersects(segment);

                // The lines intersect but one does not contain the other
                if (intersects && intersectionEnd is null) {
                    var borderIntersect = new MapPoint(intersection);
                    borderPoints.Add(borderIntersect);

                    // The line begins within the border
                    if (Border.Contains(segment.Point1)) {
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
                }

                segment.ShowSubdued();
            }

            borderPoints = borderPoints.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();

            // Add new segments across the border between all the intersections, creating a
            // multi-part border.
            // Subtract 1 so we don't loop the end back to the beginning.
            for (var i = 0; i < borderPoints.Count - 1; i++) {
                var j = (i + 1) % borderPoints.Count;

                var newSegment = AddSegment(borderPoints[i], borderPoints[j]);
                newSegment.ShowSubdued();
            }
        }

        // Remove all the segments that have at least 1 point outside of the border
        foreach (var segment in MapSegments.Where(segment => !Border.Contains(segment.Point1) || !Border.Contains(segment.Point2)).ToList()) {
            await RemoveSegment(segment);
        }

        //foreach (var borderSegment in Border.MapSegments) {
        //    foreach (var segment in MapSegments.Where(o => o.Intersects(borderSegment).IntersectionEnd is not null).ToList()) {
        //        segment.ShowHighlightedAlt();
        //        await Task.Delay(100);
        //    }

        //    Log.LogInformation($"{borderSegment} contains {MapSegments.Where(o => o.Intersects(borderSegment).IntersectionEnd is not null).Count()} segments");
        //}
    }

    async Task FixBorderWinding() {
        Log.LogTrace("Fixing border winding");

        var orderedBorderSegments = Border.MapSegments.Where(o => o.Point2.X > o.Point1.X).OrderBy(o => o.Point1.Y);

        // Grab any horizontal border segment, lowest first
        var borderStart = Border.MapSegments.Where(o => o.Point2.X != o.Point1.X).OrderBy(o => o.Point1.Y).First();
        var startPoint = borderStart.Point1;
        
        var currentSegment = MapSegments.Where(o => o.EndPoints.Contains(startPoint) && o.Intersects(borderStart).IntersectionEnd is not null).First();

        // Segment is reversed, so flip it.
        if (currentSegment.Point1.X > currentSegment.Point2.X) {
            currentSegment = await FlipSegment(currentSegment);
        }

        while (true) {
            currentSegment.ShowHighlighted();

            var otherSegments = MapSegments.Where(o =>
                o.EndPoints.Contains(currentSegment.Point2) && !o.EndPoints.Contains(currentSegment.Point1)
            ).ToList();

            foreach (var otherSegment in otherSegments) {
                otherSegment.ShowHighlightedRand();
            }

            var nextSegment = currentSegment;
            var bestAngle = float.MinValue;

            foreach (var otherSegment in otherSegments) {
                otherSegment.ShowSubdued();

                var farPoint = otherSegment.Point2;
                var alignedOtherSegment = otherSegment;

                // otherSegment is reversed
                if (farPoint == currentSegment.Point2) {
                    alignedOtherSegment = await FlipSegment(otherSegment);
                    alignedOtherSegment.ShowSubdued();

                    farPoint = alignedOtherSegment.Point2;
                }

                var farPointCross = farPoint.NearLine(currentSegment);

                // straight ahead
                if (farPointCross <= 0) {
                    nextSegment = alignedOtherSegment;
                    break;
                }

                var otherSegmentAngle = currentSegment.AngleTo(alignedOtherSegment);

                if (otherSegmentAngle > bestAngle) {
                    bestAngle = otherSegmentAngle;
                    nextSegment = alignedOtherSegment;
                }
            }

            currentSegment.ShowSubdued();

            currentSegment = nextSegment;

            if (nextSegment.Point2 == startPoint) {
                break;
            }
        }

        async Task<MapSegment> FlipSegment(MapSegment currentSegment) {
            var point1 = currentSegment.MapPoint2;
            var point2 = currentSegment.MapPoint1;

            await RemoveSegment(currentSegment);

            return AddSegment(point1, point2);
        }
    }

    async Task CheckSegments() {
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

    async Task FindPolygons() {
        Log.LogTrace("Finding polygons");

        var nonBorderSegments = MapSegments.Where(o1 => Border.MapSegments.All(o2 => o1.Intersects(o2).IntersectionEnd is null)).ToList();

        // Add the reverse of all segments except on borders
        foreach (var segment in nonBorderSegments) {
            AddSegment(segment.MapPoint2, segment.MapPoint1);
        }

        Log.LogInformation("Move to other monitor now");
        await WaitForContinue();

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

            Debug.Assert(polygonVertices.Count > 2);

            var polygon = new MapPolygon(polygonVertices);
            AddPolygon(polygon);

            //polygon.ShowPolygon();

            //AddText($"{MapPolygons.Count}: {polygon.VertexCount}", polygon.Centroid.X, polygon.Centroid.Y);
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
                // We have completed the polygon
                if (polygonSegments.Contains(otherSegment)) {
                    break;
                }

                otherSegment.ShowHighlightedAlt();

                await Task.Delay(10);

                var farPointCross = Convert.ToSingle(otherSegment.Point2.NearLine(currentSegment));

                // If both points happen to be on the right side of the line, if I ever support
                // convex polygons then this will ensure that the leftmost rotated segment is still
                // selected. Also solves degenerate case with colinears.
                var otherSegmentLeftness = currentSegment.AngleTo(otherSegment) * farPointCross;

                if (otherSegmentLeftness > farthestLeft) {
                    farthestLeft = otherSegmentLeftness;
                    nextSegment = otherSegment;
                }

                otherSegment.ShowSubdued();

                //if (currentSegmentIsReversed) {
                //    if (farPointCross >= 0) {
                //        continue;
                //    }

                //    var otherSegmentAngle = currentSegment.AngleTo(otherSegment);

                //    if (otherSegmentIsReversed) {
                //        otherSegmentAngle = 180 - otherSegmentAngle;
                //    }

                //    if (otherSegmentAngle > 0 && otherSegmentAngle < bestAngle) {
                //        bestAngle = otherSegmentAngle;
                //        nextPoint = (MapPoint)otherSegment.Point2;
                //        nextSegment = otherSegment;
                //    }
                //}
                //else {
                //    if (farPointCross <= 0) {
                //        continue;
                //    }

                //    var otherSegmentAngle = currentSegment.AngleTo(otherSegment);

                //    if (otherSegmentIsReversed) {
                //        otherSegmentAngle = 180 - otherSegmentAngle;
                //    }

                //    if (otherSegmentAngle < 180 && otherSegmentAngle > bestAngle) {
                //        bestAngle = otherSegmentAngle;
                //        nextPoint = (MapPoint)otherSegment.Point2;
                //        nextSegment = otherSegment;
                //    }
                //}
            }

            if (nextSegment is null) {
                return;
            }

            await FindLeftestSegment(nextSegment, polygonSegments);
        }
    }
}
