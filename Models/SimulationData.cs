using System.Collections.Generic;

namespace MFFSuCA.Models; 

/// <summary>
/// Record, representing data from simulation.
/// </summary>
public record SimulationData {
    /// <summary>
    /// Total amount of cells with vegetation in the simulation.
    /// </summary>
    public double TotalVegetation { get; init; }
    
    /// <summary>
    /// List, containing amount of burning cells in each step of the simulation.
    /// </summary>
    public List<int> BurningArea { get; } = new();
    
    /// <summary>
    /// List, containing amount of burned cells in each step of the simulation.
    /// </summary>
    public List<int> BurnedArea { get; } = new();
    
    /// <summary>
    /// List, containing burning speed in each step of the simulation.
    /// </summary>
    public List<int> BurningSpeed { get; } = new();
    
    /// <summary>
    /// List, containing percent of damaged vegetation in each step of the simulation.
    /// </summary>
    public List<double> DamagedVegetation { get; } = new();
}
