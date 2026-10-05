using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Rechnet zwischen den Zellen des Grundrisses und der Welt um. In einer Ebene liegt die Mitte des Startraums im Ursprung der Welt,
//auf einer Fläche die Mitte der Fläche
public sealed class LevelGrid
{
    //Raumvorlagen sind für dieses Maß gebaut. Eine Zelle ist zugleich die Breite eines Gangs
    public const float CellMeters = 4f;

    private readonly Vector3 origin;

    public LevelGrid(LevelLayout layout)
        : this(CenterOn(layout.Rooms[layout.StartRoom].Rect)) { }

    private LevelGrid(Vector3 origin)
        => this.origin = origin;

    public static LevelGrid CenteredOn(CellRect rect)
        => new(CenterOn(rect));

    private static Vector3 CenterOn(CellRect rect)
        => new(-(rect.X + rect.Width / 2f) * CellMeters, 0f, -(rect.Y + rect.Height / 2f) * CellMeters);

    public Vector3 GetCorner(int x, int y)
        => origin + new Vector3(x * CellMeters, 0f, y * CellMeters);

    public Vector3 GetCenter(Cell cell)
        => GetCorner(cell.X, cell.Y) + new Vector3(CellMeters / 2f, 0f, CellMeters / 2f);

    public Vector3 GetCenter(CellRect rect)
        => GetCorner(rect.X, rect.Y) + new Vector3(rect.Width * CellMeters / 2f, 0f, rect.Height * CellMeters / 2f);

    public Cell GetCell(Vector3 point)
        => new(Mathf.FloorToInt((point.X - origin.X) / CellMeters), Mathf.FloorToInt((point.Z - origin.Z) / CellMeters));
}
