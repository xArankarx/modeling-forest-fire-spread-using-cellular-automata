using MFFSuCA.Enums;

namespace MFFSuCA.Models;

/// <summary>
/// Class representing a cell bound to a specific rectangle on the map.
/// </summary>
public class Cell {
    /// <summary>
    /// Current burning state of the cell.
    /// </summary>
    public BurningState BurningState { get; set; }

    /// <summary>
    /// Type of the terrain.
    /// </summary>
    public BrushType TerrainType { get; init; }

    /// <summary>
    /// Current burning time of the cell.
    /// </summary>
    public int BurningTime { get; set; }

    /// <summary>
    /// Maximum burning time of the cell.
    /// </summary>
    public int MaximumBurningTime => GetMaximumBurningTime();

    /// <summary>
    /// X coordinate of the cell.
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// Y coordinate of the cell.
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// Creates a new cell with the given coordinates and terrain type.
    /// </summary>
    /// <param name="x">X coordinate of the cell.</param>
    /// <param name="y">Y coordinate of the cell.</param>
    /// <param name="terrainType">Terrain type of the cell.</param>
    private Cell(int x, int y, BrushType terrainType = BrushType.Clear) {
        X = x;
        Y = y;
        TerrainType = terrainType;
        BurningState = BurningState.Unburned;
    }

    /// <summary>
    /// Creates a new cell with (0, 0) coordinates and Clear terrain type.
    /// </summary>
    public Cell() : this(0, 0) { }

    /// <summary>
    /// Gets the maximum burning time of the cell based on its terrain type.
    /// </summary>
    /// <returns>Maximum burning time of the cell.</returns>
    private int GetMaximumBurningTime() {
        return TerrainType switch {
            BrushType.Forest => 5,
            BrushType.Grassland => 3,
            BrushType.Plain => 2,
            BrushType.Mountain => 0,
            BrushType.Water => 0,
            BrushType.HighDensityUrbanArea => 1,
            BrushType.LowDensityUrbanArea => 1,
            BrushType.Clear => 0,
            _ => 0
        };
    }
}
