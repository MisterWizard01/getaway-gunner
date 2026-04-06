using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Engine;
using Engine.Nodes;

namespace BulletHail;

public class Room()
{
    public List<TileLayerNode> TileLayers = [];
    public List<ColliderNode> Walls = [];
    public List<Enemy> Enemies = [];

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