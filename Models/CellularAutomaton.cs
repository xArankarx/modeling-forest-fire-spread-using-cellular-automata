using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Shapes;
using MFFSuCA.Enums;
using MFFSuCA.Windows;

namespace MFFSuCA.Models;

public class CellularAutomaton {
    public int SimulationSpeed { get; set; }
    
    // consider every factor that affects the spread of fire
    public double UpdateProbability(Cell cell, ref List<Cell> neighbours, ref SimulationWindow window) {
        var probability = 0.0;
        var burningNeighbours = neighbours.Count(neighbour => neighbour.BurningState == BurningState.Burning);

        // Wind direction and speed
        var windSpeed = window.WindSpeedSlider.Value;
        var windDirection = window.WindDirectionComboBox.SelectedItem as string;
        var windFactor = GetWindFactor(windSpeed, windDirection!, cell, ref neighbours);

        // Terrain type
        var terrainFactor = GetTerrainFactor(cell.TerrainType);

        // Compute final probability
        probability = burningNeighbours * windFactor * terrainFactor;

        // Cap probability to 100%
        probability = Math.Min(1.0, probability);

        return probability;
    }
    
    private static double GetWindFactor(double windSpeed, string windDirection, Cell cell, ref List<Cell> neighbours) {
        // take into account all burning neighbours from the given direction
        var burningNeighbours = neighbours.Where(neighbour => neighbour.BurningState == BurningState.Burning);
        var windFactor = windDirection switch {
            "North" => burningNeighbours.Count(neighbour => neighbour.Y - cell.Y < 0),
            "North-East" => burningNeighbours.Count(neighbour => neighbour.Y - cell.Y < 0 && neighbour.X - cell.X > 0),
            "East" => burningNeighbours.Count(neighbour => neighbour.X - cell.X > 0),
            "South-East" => burningNeighbours.Count(neighbour => neighbour.Y - cell.Y > 0 && neighbour.X - cell.X > 0),
            "South" => burningNeighbours.Count(neighbour => neighbour.Y - cell.Y > 0),
            "South-West" => burningNeighbours.Count(neighbour => neighbour.Y - cell.Y > 0 && neighbour.X - cell.X < 0),
            "West" => burningNeighbours.Count(neighbour => neighbour.X - cell.X < 0),
            "North-West" => burningNeighbours.Count(neighbour => neighbour.Y - cell.Y < 0 && neighbour.X - cell.X < 0),
            _ => 0.0
        };
        windFactor += windSpeed / 20.0;
        return windFactor;
    }

    private static double GetTerrainFactor(BrushType terrainType) {
        var terrainFactor = terrainType switch {
            BrushType.Clear => 0.0,
            BrushType.Forest => 0.7,
            BrushType.Grassland => 0.45,
            BrushType.Plain => 0.1,
            BrushType.Water => 0.0,
            BrushType.HighDensityUrbanArea => 0.05,
            BrushType.LowDensityUrbanArea => 0.1,
            _ => 0.0
        };
        return terrainFactor;
    }

}
