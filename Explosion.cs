using Engine.Nodes;
using Microsoft.Xna.Framework;

namespace BulletHail;

public class Explosion(Vector2 position) : Node2D(position)
{
    public int Lifetime;
}