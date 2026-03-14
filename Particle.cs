using Engine;
using Engine.Nodes;
using Microsoft.Xna.Framework;

namespace BulletHail;

public class Particle(Vector2 position) : GameObject(position)
{
    public Vector2 Acceleration;

    public override void Update(Node parent, int frameNumber, InputState inputState)
    {
        base.Update(parent, frameNumber, inputState);
        Velocity += Acceleration;
    }
}