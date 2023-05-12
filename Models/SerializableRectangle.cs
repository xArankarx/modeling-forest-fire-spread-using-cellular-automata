using System;
using MFFSuCA.Enums;

namespace MFFSuCA.Models;

/// <summary>
/// Class representing a serializable rectangle.
/// </summary>
/// <param name="Left">Left position of the rectangle.</param>
/// <param name="Top">Top position of the rectangle.</param>
/// <param name="BrushType">Brush type of the rectangle.</param>
[Serializable]
public record SerializableRectangle(double Left = 0, double Top = 0, BrushType BrushType = BrushType.Clear) {
    /// <summary>
    /// Creates a new serializable rectangle.
    /// </summary>
    public SerializableRectangle() : this(0) { }
}
