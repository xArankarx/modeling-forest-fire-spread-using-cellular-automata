using MFFSuCA.Enums;

namespace MFFSuCA.Models; 

public class Cell {
    public BurningState BurningState { get; set; }
    
    public BrushType TerrainType { get; init; }

    public int BurningTime { get; set; }
    
    public int MaximumBurningTime => GetMaximumBurningTime();
    
    public int X { get; init; }
    
    public int Y { get; init; }

    private Cell(int x, int y, BrushType terrainType = BrushType.Clear) {
        X = x;
        Y = y;
        TerrainType = terrainType;
        BurningState = BurningState.Unburned;
    }

    public Cell() : this(0, 0) { }

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
