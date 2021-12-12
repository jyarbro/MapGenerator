using Nrrdio.Utilities.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Nrrdio.MapGenerator.Services {
    public class Generator {
        public Polygon Border { get; set; }

        public void SetBorder(Polygon border) {
            if (!border.IsConvex()) {
                throw new ArgumentException("Border polygon must be convex because I'm too lazy to program around complex polys.");
            }

            Border = border;
        }

        public List<Point> Points(int amount, int seed) {
            var random = new Random(seed);
            var points = new List<Point>();

            points.AddRange(Border.Vertices);

            double pointX;
            double pointY;

            var minX = Border.Vertices.Min(o => o.X);
            var minY = Border.Vertices.Min(o => o.Y);
            var maxX = Border.Vertices.Max(o => o.X);
            var maxY = Border.Vertices.Max(o => o.Y);

            var rangeX = maxX - minX;
            var rangeY = maxY - minY;

            var i = 0;

            while (i < amount) {
                pointX = minX + random.NextDouble() * rangeX;
                pointY = minY + random.NextDouble() * rangeY;

                var point = new Point(pointX, pointY);

                if (Border.Contains(point)) {
                    points.Add(point);
                }

                i++;
            }

            return points;
        }

        // https://www.codeguru.com/cplusplus/delaunay-triangles/
        public IEnumerable<Polygon> DelaunayTriangles(IEnumerable<Point> points) {
            var borderVertices = Border.Vertices.Count;
            int j;

            var triangles = new HashSet<Polygon>();

            for (var i = 0; i < borderVertices; i++) {
                j = (i + 1) % borderVertices;

                var triangle = new Polygon(Border.Centroid, Border.Vertices[i], Border.Vertices[j]);
                triangles.Add(triangle);
            }

            foreach (var point in points) {
                var badTriangles = findBadTriangles(point, triangles);
                var boundaries = findHoleBoundaries(badTriangles);

                foreach (var badTriangle in badTriangles) {
                    foreach (var vertex in badTriangle.Vertices) {
                        vertex.AdjacentPolygons.Remove(badTriangle);
                    }
                }

                triangles.RemoveWhere(badTriangles.Contains);

                foreach (var edge in boundaries.Where(possibleEdge => !possibleEdge.Contains(point))) {
                    var triangle = new Polygon(point, edge.Point1, edge.Point2);
                    triangles.Add(triangle);
                }
            }

            return triangles;

            ISet<Polygon> findBadTriangles(Point target, ISet<Polygon> potentials) {
                var badTriangles = potentials.Where(o => o.Circumcircle.Contains(target));
                return new HashSet<Polygon>(badTriangles);
            }

            IEnumerable<Segment> findHoleBoundaries(ISet<Polygon> badTriangles) {
                var edges = new List<Segment>();

                foreach (var triangle in badTriangles) {
                    edges.Add(new Segment(triangle.Vertices[0], triangle.Vertices[1]));
                    edges.Add(new Segment(triangle.Vertices[1], triangle.Vertices[2]));
                    edges.Add(new Segment(triangle.Vertices[2], triangle.Vertices[0]));
                }

                return edges.GroupBy(o => o).Where(o => o.Count() == 1).Select(o => o.First());
            }
        }

        public IEnumerable<Segment> TriangleEdges(IEnumerable<Polygon> triangles) {
            var edges = new HashSet<Segment>();

            foreach (var triangle in triangles) {
                edges.Add(new Segment(triangle.Vertices[0], triangle.Vertices[1]));
                edges.Add(new Segment(triangle.Vertices[1], triangle.Vertices[2]));
                edges.Add(new Segment(triangle.Vertices[2], triangle.Vertices[0]));
            }

            return edges;
        }

        public IEnumerable<Segment> VoronoiEdges(IEnumerable<Polygon> triangles) {
            var voronoiEdges = new List<Segment>();

            foreach (var triangle in triangles) {
                foreach (var edge in triangle.Edges) {
                    var neighbors = triangles.Where(other => other.Edges.Contains(edge));

                    foreach (var neighbor in neighbors) {
                        var voronoiEdge = new Segment(triangle.Circumcircle.Center, neighbor.Circumcircle.Center);
                        voronoiEdges.Add(voronoiEdge);
                    }
                }
            }

            return voronoiEdges;
        }
    }
}
