using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Rechnet zwischen den Zellen des Grundrisses und der Welt um. Die Mitte des Startraums liegt im Ursprung der Welt
public sealed class LevelGrid
{
    //Raumvorlagen sind für dieses Maß gebaut. Eine Zelle ist zugleich die Breite eines Gangs
    public const float CellMeters = 4f;

    private readonly Vector3 origin;

    public LevelGrid(LevelLayout layout)
    {
        var start = layout.Rooms[layout.StartRoom].Rect;

        origin = new Vector3(-(start.X + start.Width / 2f) * CellMeters, 0f, -(start.Y + start.Height / 2f) * CellMeters);
    }

    public Vector3 GetCorner(int x, int y)
        => origin + new Vector3(x * CellMeters, 0f, y * CellMeters);

    public Vector3 GetCenter(Cell cell)
        => GetCorner(cell.X, cell.Y) + new Vector3(CellMeters / 2f, 0f, CellMeters / 2f);

    public Vector3 GetCenter(CellRect rect)
        => GetCorner(rect.X, rect.Y) + new Vector3(rect.Width * CellMeters / 2f, 0f, rect.Height * CellMeters / 2f);

    public Cell GetCell(Vector3 point)
        => new(Mathf.FloorToInt((point.X - origin.X) / CellMeters), Mathf.FloorToInt((point.Z - origin.Z) / CellMeters));
}
