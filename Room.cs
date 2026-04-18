using System.Collections.Generic;
using Engine;
using Engine.Nodes;
using Microsoft.Xna.Framework;

namespace BulletHail;

public class Room()
{
    public List<TileLayerNode> TileLayers = [];
    public List<ColliderNode> Walls = [];
    public List<Enemy> Enemies = [];
    public List<Vector2> PatrolPoints = [];

    public int RoomType = 0;

    public Point Dimensions;
    public int Width => Dimensions.X;
    public int Height => Dimensions.Y;

    public void Draw(Camera camera)
    {
        foreach (var layer in TileLayers)
        {
            layer.Draw(null, camera, Vector2.Zero);
        }
    }
}