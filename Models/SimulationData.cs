using System.Collections.Generic;

namespace MFFSuCA.Models; 

// TODO: Charts window, gather data from simulation

public class SimulationData {
    public int TotalVegetation { get; set; }
    
    public List<int> BurningArea { get; set; } = new();
    
    public List<int> BurnedArea { get; set; } = new();
    
    public List<int> BurningSpeed { get; set; } = new();
    
    public List<double> DamagedVegetation { get; set; } = new();
}
