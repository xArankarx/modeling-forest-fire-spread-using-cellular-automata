using MFFSuCA.Enums;

namespace MFFSuCA.Models; 

public class Cell {
    public BurningState BurningState { get; set; }
    
    public BrushType TerrainType { get; set; }
    
    public double SpreadProbability { get; set; }
    
    public int X { get; set; }
    
    public int Y { get; set; }
    
    public Cell(int x, int y, BrushType terrainType) {
        X = x;
        Y = y;
        TerrainType = terrainType;
        BurningState = BurningState.Unburned;
    }
    
    public Cell(int x, int y) : this(x, y, BrushType.Clear) { }
    
    public Cell() : this(0, 0) { }
    
    
}