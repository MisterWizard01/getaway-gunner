using Engine;
using Engine.Nodes;
using Microsoft.Xna.Framework;

namespace BulletHail;

public class GameObject(Vector2 position) : Node2D(position)
{
    public Vector2 Velocity;
    public SpriteNode[] Sprites = [];
    public ColliderNode[] Colliders = [];

    public override void Update(Node parent, int frameNumber, InputState inputState)
    {
        base.Update(parent, frameNumber, inputState);

        Position += Velocity;
        foreach (var sprite in Sprites)
        {
            sprite.Update(this, frameNumber, inputState);
        }
    }

    public override void Draw(Node parent, Camera camera, Vector2 referencePoint)
    {
        base.Draw(parent, camera, referencePoint);
        foreach (var sprite in Sprites)
        {
            sprite.Draw(null, camera, Position);
        }
    }
}