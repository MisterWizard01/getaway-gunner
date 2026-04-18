using System.IO;
using System.Runtime.CompilerServices;
using Engine.JsonConverters;
using Engine.Managers;
using Engine.Nodes;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace BulletHail;

public class CustomTiledParser : TiledParser
{
    public override (string name, Node node) ParseObject(JuicyContentManager contentManager, JObject obj)
    {
        string name = obj.Value<string>("name");
        Node objectNode;
        //var properties = ParseProperties(contentManager, obj);

        switch (obj.Value<string>("type"))
        {
            default:
                var position = new Vector2(obj.Value<float>("x"), obj.Value<float>("y"));
                if (obj.Value<float>("width") > 0 && obj.Value<float>("height") > 0)
                {
                    var dimensions = new Vector2(obj.Value<float>("width"), obj.Value<float>("height"));
                    objectNode = new ColliderNode(position + dimensions / 2, dimensions);
                }
                else
                {
                    objectNode = new Node2D(position);
                }
                break;
        }
        return (name, objectNode);
    }

    public Room ParseRoom(JuicyContentManager juicyCM, string path)
    {
        using StreamReader reader = new(path);
        var json = reader.ReadToEnd();
        var jObject = JObject.Parse(json);
        var scene = ParseMap(juicyCM, jObject);
        var room = new Room()
        {
            Dimensions = new((int)jObject["width"] * (int)jObject["tilewidth"], (int)jObject["height"] * (int)jObject["tileheight"]),
        };

        for (int i = 0; i < scene.CountChildren; i++)
        {
            var layer = scene.GetChild(i) as TileLayerNode;
            if (layer is not null)
                room.TileLayers.Add(layer);
        }

        for (var i = 0; i < scene.GetChild("Walls").CountChildren; i++)
        {
            var wall = scene.GetChild("Walls").GetChild(i) as ColliderNode;
            if (wall is not null)
                room.Walls.Add(wall);
        }

        var enemySpawns = scene.GetChild("Enemy spawns");
        if (enemySpawns is not null) {
            for (var i = 0; i < enemySpawns.CountChildren; i++)
            {
                var spawnPoint = scene.GetChild("Enemy spawns").GetChild(i) as Node2D;
                if (spawnPoint is not null)
                    room.Enemies.Add(new Enemy(spawnPoint.Position)
                    {
                        Sprites = [juicyCM.GenerateSprite("spritesheet", "enemydown")],
                        Colliders = [new ColliderNode(0, 0, 16, 16)],
                        Velocity = Game1.PickRandomVelocity(Game1.enemySpeed),
                    });
            }
        }

        var patrolPoints = scene.GetChild("Patrol points");
        if (patrolPoints is not null) {
            for (var i = 0; i < patrolPoints.CountChildren; i++)
            {
                var patrolPoint = patrolPoints.GetChild(i) as Node2D;
                if (patrolPoint is not null)
                    room.PatrolPoints.Add(patrolPoint.Position);
            }
        }
        return room;
    }
}