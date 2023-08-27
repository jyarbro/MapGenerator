using Nrrdio.MapGenerator.Services.Models;

namespace Nrrdio.MapGenerator.Services;

public interface IGenerator {
    bool Continue { get; set; }
    Task<IEnumerable<MapPolygon>> Generate(int points, IEnumerable<MapPoint> borderPoints);
}
