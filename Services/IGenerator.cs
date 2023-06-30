using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Nrrdio.MapGenerator.Services.Models;

namespace Nrrdio.MapGenerator.Services;

public interface IGenerator {
    bool Continue { get; set; }
    Task Generate(int points, MapPolygon border, Canvas outputCanvas);
}
