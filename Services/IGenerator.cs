using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services.Models;

namespace Nrrdio.MapGenerator.Services;

public interface IGenerator {
    bool Continue { get; set; }
    void Initialize(Canvas outputCanvas);
    Task Generate(int points, IEnumerable<MapPoint> borderPoints);
    Task<IEnumerable<MapPolygon>> GenerateWithReturn(int points, IEnumerable<MapPoint> borderPoints);
}
