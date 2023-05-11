using System.Collections.Generic;

namespace MFFSuCA.Models; 

public class SimulationData {
    public double TotalVegetation { get; init; }
    
    public List<int> BurningArea { get; } = new();
    
    public List<int> BurnedArea { get; } = new();
    
    public List<int> BurningSpeed { get; } = new();
    
    public List<double> DamagedVegetation { get; } = new();
}
