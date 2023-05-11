using System;

namespace MFFSuCA.Models; 

/// <summary>
/// Record for storing the simulation parameters.
/// </summary>
/// <param name="SimulationSpeed">Simulation speed (x1, x2, x4, x8)</param>
/// <param name="WindDirection">Wind direction (North, North-East, East, South-East, South, South-West, West, North-West)</param>
/// <param name="WindSpeed">Wind speed (0 - 67 meters per second)</param>
[Serializable]
public record SimulationParameters(string SimulationSpeed, string WindDirection, double WindSpeed);
