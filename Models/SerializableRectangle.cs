using System;
using MFFSuCA.Enums;

namespace MFFSuCA.Models;

[Serializable]
public record SerializableRectangle(double Left = 0, double Top = 0, BrushType BrushType = BrushType.Clear) {
    public SerializableRectangle() : this(0) { }
}
