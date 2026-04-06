using Engine;
using Engine.Managers;
using Engine.Nodes;
using SpriteBuilder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MathHelper = Engine.MathHelper;
using Engine.JsonConverters;
using Newtonsoft.Json.Linq;

namespace BulletHail;

enum GameState
{
    Title,
    GamePlay,
    GameOver,
}

enum InputSignal
{
    HorizontalMovement,
    VerticalMovement,
    FireHorizontal,
    FireVertical,
    Accept,
}


public class Game1 : Game
{
    #region Fields and Properties
    public readonly float TanPiOver8 = MathF.Sqrt(2) - 1;
    public readonly string[] directionNames = ["right", "downright", "down", "downleft", "left", "upleft", "up", "upright"];
    public readonly string[] slopeNames = ["horizontal", "shallow negative", "negative diagonal", "steep negative", "vertical", "steep positive", "positive diagonal", "shallow positive",
                                           "horizontal", "shallow negative", "negative diagonal", "steep negative", "vertical", "steep positive", "positive diagonal", "shallow positive"];
    
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private readonly JuicyContentManager _juicyCM;
    private Camera _camera, _guiCamera;
    private RasterizerState _rasterizerState;
    private readonly InputManager _inputManager;
    private readonly ActionScheduleManager _asm;
    private readonly CustomTiledParser _tiledParser;
    private static Random random;

    private KeyboardState _prevKeyboardState;
    private GamePadState _previousGamePadState;
    private int frameNumber;
    //private Effect _grayscaleEffect, _silhouetteEffect;
    private Effect _betterBlend;
    private Texture2D _whitePixel;

    private List<Room> roomPool;
    private Room[] level;
    private readonly int levelWidth = 7, levelHeight = 7;
    private int currentCell;
    private Room currentRoom;

    private GameState gameState;
    private bool paused;
    private int score, lives, freezeFrames, iFramesEnd;

    private GameObject ship;
    private float prevFacing, shipSpeed, facing;

    private List<GameObject> shots;
    private float shotSpeed;
    private int nextShot, shotDelay;

    public static int nextEnemy, enemyDelay, enemySpeed;

    private List<GameObject> pickups;
    private int pickupTime;

    private List<GameObject> bullets;
    private float bulletSpeed;
    
    private List<Particle> particles;

    private bool PlayerVulnerable => lives > 0 && frameNumber > iFramesEnd;
    
    #endregion

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnResizeWindow;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _inputManager = new(InputMode.KeyboardOnly, Enum.GetValues<InputSignal>().Length);
        _juicyCM = new();
        _asm = new();
        _tiledParser = new();
        random = new();
    }

    #region Startup methods
    protected override void Initialize()
    {
        DefaultControls(_inputManager);
        base.Initialize();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _camera = new(_spriteBatch, new Rectangle(0, 0, 256, 192));
        _guiCamera = new(_spriteBatch, new Rectangle(0, 0, 256, 192));
        _rasterizerState = new() { ScissorTestEnable = true };
        OnResizeWindow(null, null);
        
        gameState = GameState.Title;
        score = 0;
        lives = 3;
        freezeFrames = 0;
        iFramesEnd = 0;

        ship = new GameObject(_camera.GameRect.Center.ToVector2())
        {
            Sprites = [new SpriteNode(_juicyCM.Textures["spritesheet"], _juicyCM.Animations["shipright"])],
            Colliders = [new ColliderNode(0, 0, 6, 6)],
        };
        prevFacing = 0;
        shipSpeed = 2;

        shots = [];
        shotSpeed = 8;
        shotDelay = 5;

        enemyDelay = 120;
        enemySpeed = 1;

        pickups = [];

        bullets = [];
        bulletSpeed = 3;

        particles = [];
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        _whitePixel.SetData([Color.White]);

        //_grayscaleEffect = Content.Load<Effect>("grayscale");
        //_silhouetteEffect = Content.Load<Effect>("silhouette");
        _betterBlend = Content.Load<Effect>("betterBlend");

        var contentFolder = FileManager.GetContentFolder();
        _juicyCM.LoadTextures(Content, contentFolder);
        //_juicyCM.LoadSprites(Path.Combine(FileManager.GetContentFolder(), "sprites/Sprites.json"));

        _juicyCM.GenerateAnimationSet("ship", directionNames, 1, 0, 0, 20, 20, 0, 0, false);
        _juicyCM.GenerateAnimationSet("enemy", directionNames, 1, 0, 20, 22, 22, 0, 0, false);
        _juicyCM.GenerateAnimationSet("shot", directionNames, 1, 0, 64, 16, 16, 0, 0, false);
        _juicyCM.GenerateAnimation("pickup", 18, 0, 96, 8, 8, 0, true)
            .EndAction = AnimationEndAction.Cycle;
        _juicyCM.GenerateAnimation("bullet", 4, 192, 64, 12, 12, 0, true)
            .EndAction = AnimationEndAction.Reverse;
        _juicyCM.GenerateAnimationSet("muzzle flash", directionNames, 6, 0, 112, 32, 32, 0, 0, true);
        _juicyCM.GenerateAnimation("shot pop", 7, 0, 48, 12, 12, 0, true);
        _juicyCM.GenerateAnimation("explosion", 10, 176, 0, 32, 32, 0, true);
        _juicyCM.GenerateAnimation("explosion flash", 4, 176, 32, 32, 32, 0, true);
        _juicyCM.GenerateAnimation("spark vertical", 9, 304, 48, 3, 19, 0, true);
        _juicyCM.GenerateAnimation("spark horizontal", 9, 331, 48, 19, 3, 0, false);
        _juicyCM.GenerateAnimation("spark positive diagonal", 9, 352, 48, 16, 16, 0, true);
        _juicyCM.GenerateAnimation("spark negative diagonal", 9, 352, 64, 16, 16, 0, true);
        _juicyCM.GenerateAnimation("spark steep positive", 9, 352, 80, 16, 16, 0, true);
        _juicyCM.GenerateAnimation("spark steep negative", 9, 352, 96, 16, 16, 0, true);
        _juicyCM.GenerateAnimation("spark shallow positive", 9, 352, 112, 16, 16, 0, true);
        _juicyCM.GenerateAnimation("spark shallow negative", 9, 352, 128, 16, 16, 0, true);

        // _juicyCM.LoadFonts();
        _juicyCM.Fonts.Add("tiny mono", FontBuilder.BuildFont(_juicyCM.Textures["tiny mono"], new Point(3, 3), new Point(1, 1), ' ', false));
        _juicyCM.Fonts.Add("mostly sans", FontBuilder.BuildFont(_juicyCM.Textures["mostly sans"], new Point(5, 6), new Point(1, 1), ' ', new Point(3, 5)));
        _juicyCM.Fonts.Add("basically aseprite", FontBuilder.BuildFont(_juicyCM.Textures["basically aseprite"], new Point(5, 7), new Point(1, 1), ' ', new Point(4, 6)));
        _juicyCM.Fonts.Add("blocky sans", FontBuilder.BuildFont(_juicyCM.Textures["blocky sans"], new Point(8, 12), new Point(1, 1), ' ', new Point(6, 10)));
    
        //load tilesets
        _juicyCM.LoadTilesets(Path.Combine(contentFolder, "Tiled"));

        //load the rooms
        roomPool = [
            _tiledParser.ParseRoom(_juicyCM, Path.Combine(contentFolder, "Tiled", "cross1.tmj")),
            _tiledParser.ParseRoom(_juicyCM, Path.Combine(contentFolder, "Tiled", "cross2.tmj")),
            _tiledParser.ParseRoom(_juicyCM, Path.Combine(contentFolder, "Tiled", "cross3.tmj")),
            _tiledParser.ParseRoom(_juicyCM, Path.Combine(contentFolder, "Tiled", "cross4.tmj")),
            _tiledParser.ParseRoom(_juicyCM, Path.Combine(contentFolder, "Tiled", "cross5.tmj")),
            _tiledParser.ParseRoom(_juicyCM, Path.Combine(contentFolder, "Tiled", "cross1.tmj")),
            _tiledParser.ParseRoom(_juicyCM, Path.Combine(contentFolder, "Tiled", "cross2.tmj")),
            _tiledParser.ParseRoom(_juicyCM, Path.Combine(contentFolder, "Tiled", "cross3.tmj")),
            _tiledParser.ParseRoom(_juicyCM, Path.Combine(contentFolder, "Tiled", "cross4.tmj")),
            _tiledParser.ParseRoom(_juicyCM, Path.Combine(contentFolder, "Tiled", "cross5.tmj")),
        ];

        //generate the map
        var center = levelWidth / 2 + levelHeight / 2 * levelWidth;
        var map = new bool[49];
        var frontier = new List<int>() { center }; //add the center cell to the array
        var roomCount = 0;
        while (roomCount < 10 && frontier.Count > 0)
        {
            //pick a cell that's currently in the frontier
            var index = random.Next(frontier.Count);
            var cell = frontier[index];
            frontier.RemoveAt(index);

            //get the cell's neighbors
            // 0: invalid location (out of bounds)
            // 1: valid location
            // 4: already contains a room
            var left   = cell % levelWidth == 0 ? 0 : !map[cell - 1] ? 1 : 4;
            var top    = cell / levelWidth == 0 ? 0 : !map[cell - levelWidth] ? 1 : 4;
            var right  = cell % levelWidth == levelWidth - 1 ? 0 : !map[cell + 1] ? 1 : 4;
            var bottom = cell / levelWidth == levelHeight - 1 ? 0 : !map[cell + levelWidth] ? 1 : 4;

            //see if the cell is suitable to be made into a room
            if (left + right + top + bottom >= 8)
                continue;

            //create a room to go here
            map[cell] = true;
            roomCount++;

            //add neighbors to frontier
            if (left   == 1) frontier.Add(cell - 1);
            if (top    == 1) frontier.Add(cell - levelWidth);
            if (right  == 1) frontier.Add(cell + 1);
            if (bottom == 1) frontier.Add(cell + levelWidth);
        }

        //fill the level with rooms according to the map
        level = new Room[levelWidth * levelHeight];
        for (int i = 0; i < map.Length; i++)
        {
            if (!map[i]) continue;

            level[i] = roomPool[random.Next(roomPool.Count)];
            roomPool.Remove(level[i]);
        }
        currentCell = center;
        currentRoom = level[currentCell];
    }

    #endregion
    
    #region Update methods

    protected override void Update(GameTime gameTime)
    {
        //recieve inputs
        KeyboardState keyboardState = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
        if (gamePadState.Buttons.Back == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.Escape))
            Exit();
        if (keyboardState.IsKeyDown(Keys.F1) && _prevKeyboardState.IsKeyUp(Keys.F1))
            ToggleFullScreen();
        _inputManager.Update(_camera.GameToView(ship.Position));
        var inputState = _inputManager.InputState;

        if (gamePadState.Buttons.Start == ButtonState.Pressed && _previousGamePadState.Buttons.Start == ButtonState.Released 
        || keyboardState.IsKeyDown(Keys.Space) && _prevKeyboardState.IsKeyUp(Keys.Space))
        {
            paused = !paused;
        }

        if (paused && !(keyboardState.IsKeyDown(Keys.OemPeriod) && _prevKeyboardState.IsKeyUp(Keys.OemPeriod)))
        {
            _prevKeyboardState = keyboardState;
            _previousGamePadState = gamePadState;
            return;
        }

        if (freezeFrames > 0)
        {
            freezeFrames--;
            _prevKeyboardState = keyboardState;
            _previousGamePadState = gamePadState;
            return;
        }

        //handle any scheduled actions
        _asm.Update(frameNumber);

        //handle game states
        if (gameState == GameState.Title)
        {
            UpdatePlayer(inputState);
            if (inputState.GetInput((int)InputSignal.Accept) > 0)
            {
                gameState = GameState.GamePlay;
            }
        }
        else if (gameState == GameState.GameOver)
        {
            UpdateBullets();
            UpdateEnemies(inputState);
            if (inputState.GetInput((int)InputSignal.Accept) > 0)
            {
                //reset game
                ship.Position = _camera.GameRect.Center.ToVector2();
                shots.Clear();
                pickups.Clear();
                bullets.Clear();
                score = 0;
                lives = 3;
                iFramesEnd = 0;
                gameState = GameState.Title;
            }
        }
        else
        {
            UpdatePlayer(inputState);
            UpdateBullets();
            UpdateEnemies(inputState);
            UpdatePickups(inputState);
        }
        UpdateParticles();

        //ready next frame
        _prevKeyboardState = keyboardState;
        _previousGamePadState = gamePadState;
        prevFacing = facing;
        frameNumber++;
        base.Update(gameTime);
    }

    private void UpdatePlayer(InputState inputState)
    {
        //movement
        Vector2 moveVector = new(
            inputState.GetInput((int)InputSignal.HorizontalMovement),
            inputState.GetInput((int)InputSignal.VerticalMovement)
        );

        //snap the angle to one of 8 directions
        //normalize and change the anim frame
        if (moveVector.LengthSquared() > 0)
        {
            facing = MathHelper.VectorToAngle(moveVector);
            facing = MathHelper.Snap(facing, MathF.PI / 4);
            moveVector = MathHelper.AngleToVector(facing);
            moveVector /= moveVector.Length();
            _juicyCM.SetSpriteAnimation(ship.Sprites[0], "ship" + JuicyContentManager.DirectionString(directionNames, facing));
            if (facing != prevFacing)
            {
                ship.X = MathF.Round(ship.X);
                ship.Y = MathF.Round(ship.Y);
            }
        }
        ship.Update(null, frameNumber, inputState); // this sets the previousPosition value to the current position
        ship.Position += moveVector * shipSpeed;

        //wall collisions
        foreach (var wall in currentRoom.Walls)
        {
            var collision = CollisionManager.CheckCollision(ship.Colliders[0], wall, ship.Position, Vector2.Zero, ship.PreviousPosition, Vector2.Zero);
            if (collision is not null)
            {
                var response = CollisionManager.HandleSolidCollision(collision.Value);
                ship.Position += response;
            }
        }
        //_camera.Position = ship.Position.ToPoint() - _camera.Size / new Point(2);

        //exit room
        if (ship.Y < 0 && level[currentCell - levelWidth] is not null)
        {
            currentCell -= levelWidth;
            currentRoom = level[currentCell];
            ship.Y = currentRoom.Height;
        }
        if (ship.Y > currentRoom.Height && level[currentCell + levelWidth] is not null)
        {
            currentCell += levelWidth;
            currentRoom = level[currentCell];
            ship.Y = 0;
        }
        if (ship.X < 0 && level[currentCell - 1] is not null)
        {
            currentCell -= 1;
            currentRoom = level[currentCell];
            ship.X = currentRoom.Width;
        }
        if (ship.X > currentRoom.Width && level[currentCell + 1] is not null)
        {
            currentCell += 1;
            currentRoom = level[currentCell];
            ship.X = 0;
        }

        //shooting
        Vector2 fireVector = new(
            inputState.GetInput((int)InputSignal.FireHorizontal),
            inputState.GetInput((int)InputSignal.FireVertical)
        );
        if (fireVector.LengthSquared() > 0)
        {
            facing = MathHelper.VectorToAngle(fireVector);
            _juicyCM.SetSpriteAnimation(ship.Sprites[0], "ship" + JuicyContentManager.DirectionString(directionNames, facing));
            if (frameNumber >= nextShot)
            {
                // for (int i = -1; i < 2; i++)
                var i = 0;
                {
                    var shot = new GameObject(ship.Position)
                    {
                        Sprites = [_juicyCM.GenerateSprite("spritesheet", "shot" + JuicyContentManager.DirectionString(directionNames, facing))],
                        Colliders = [new ColliderNode(0, 0, 8, 8)],
                        Velocity = MathHelper.AngleToVector(facing + i * MathF.PI / 12, shotSpeed),
                    };
                    shots.Add(shot);

                    var muzzleFlash = new Particle(ship.Position + Vector2.Normalize(fireVector) * 10)
                    {
                        Sprites = [_juicyCM.GenerateSprite("spritesheet", "muzzle flash" + JuicyContentManager.DirectionString(directionNames, facing))],
                    };
                    particles.Add(muzzleFlash);
                }
                nextShot = frameNumber + shotDelay;
            }
        }

        for (int i = shots.Count - 1; i >= 0; i--)
        {
            var shot = shots[i];
            shot.Position += shot.Velocity;
            // if (shot.X < -8 || shot.Y < -8 || shot.X > _camera.Size.X + 8 || shot.Y > _camera.Size.Y + 8)
            // {
            //     shots.Remove(shot);
            // }
            foreach (var wall in currentRoom.Walls)
            {
                if (CollisionManager.CheckCollisionSimple(shot.Colliders[0], wall, shot.Position, Vector2.Zero))
                {
                    shots.Remove(shot);
                    particles.Add(new Particle(shot.Position)
                    {
                        Sprites = [_juicyCM.GenerateSprite("spritesheet", "shot pop")],
                    });
                }
            }
        }
    }

    private void UpdateEnemies(InputState inputState)
    {
        for (int i = currentRoom.Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = currentRoom.Enemies[i];
            var los = CheckLoS(enemy.Position, ship.Position, currentRoom.Walls);
            if (los)
            {
                enemy.target = ship.Position;
            }
            var targetVector = enemy.target - enemy.Position;
            var facing = MathHelper.VectorToAngle(targetVector);
            switch (enemy.state)
            {
                case EnemyState.Patrolling:
                    enemy.Update(null, frameNumber, inputState);
                    if (enemy.X % 8 == 0)
                    {
                        if (enemy.Y % 8 == 0)
                        {
                            enemy.Velocity = PickRandomVelocity(enemySpeed);
                        }
                        else
                        {
                            enemy.Velocity.X = 0;
                        }
                    }
                    else
                    {
                        enemy.Velocity.Y = 0;
                    }
                    if (los)
                    {
                        enemy.state = EnemyState.Chasing;
                    }
                    break;

                case EnemyState.Chasing:
                    if (targetVector.LengthSquared() < enemySpeed * enemySpeed)
                    {
                        enemy.state = EnemyState.Patrolling;
                    }
                    else if (los && (ship.Position - enemy.Position).LengthSquared() < 64 * 64)
                    {
                        enemy.state = EnemyState.Attacking;
                    }
                    else
                    {
                        targetVector.Normalize();
                        facing = MathHelper.Snap(facing, MathF.PI / 4);
                        enemy.Velocity = MathHelper.AngleToVector(facing, enemySpeed);
                        enemy.Update(null, frameNumber, inputState);
                        _juicyCM.SetSpriteAnimation(enemy.Sprites[0], "enemy" + JuicyContentManager.DirectionString(directionNames, facing));
                    }
                    break;

                case EnemyState.Attacking:
                    //enemy shooting
                    if (frameNumber >= enemy.NextShot)
                    {
                        var fireVector = MathHelper.AngleToVector(facing, bulletSpeed);
                        var bullet = new GameObject(enemy.Position) 
                        {
                            Sprites = [_juicyCM.GenerateSprite("spritesheet", "bullet")],
                            Colliders = [new ColliderNode(-3, -3, 6, 6)],
                            Velocity = fireVector,
                        };
                        bullet.Sprites[0].FrameRatio = 0.5f;
                        bullets.Add(bullet);
                        enemy.NextShot = frameNumber + Enemy.ShotDelay;
                        var muzzleFlash = new Particle(enemy.Position + Vector2.Normalize(fireVector) * 10)
                        {
                            Sprites = [_juicyCM.GenerateSprite("spritesheet", "muzzle flash" + JuicyContentManager.DirectionString(directionNames, facing))],
                        };
                        particles.Add(muzzleFlash);
                        enemy.state = EnemyState.Chasing;
                    }
                    break;
            }

            //enemy collision w walls
            foreach (var wall in currentRoom.Walls)
            {
                var collision = CollisionManager.CheckCollision(enemy.Colliders[0], wall, enemy.Position, Vector2.Zero, enemy.PreviousPosition, Vector2.Zero);
                if (collision is not null)
                {
                    var response = CollisionManager.HandleSolidCollision(collision.Value);
                    enemy.Position += response;
                }
            }

            //enemy collision w shots
            var dead = false;
            for (int j = shots.Count - 1; j >= 0; j--)
            {
                var shot = shots[j];
                if (CollisionManager.CheckCollisionSimple(shot.Colliders[0], enemy.Colliders[0], shot.Position, enemy.Position))
                {
                    shots.Remove(shot);
                    particles.Add(new Particle(shot.Position)
                    {
                        Sprites = [_juicyCM.GenerateSprite("spritesheet", "shot pop")],
                    });
                    for (int k = -1; k < 2; k++)
                    {
                        MakeSpark(shot.Position, MathHelper.VectorToAngle(shot.Velocity) + random.NextSingle() * MathF.PI / 2 - MathF.PI / 4);
                    }
                    enemy.Health -= 1;
                    if (enemy.Health <= 0)
                    {
                        currentRoom.Enemies.Remove(enemy);
                        pickups.Add(new GameObject(enemy.Position) {
                            Sprites = [_juicyCM.GenerateSprite("spritesheet", "pickup")],
                            Colliders = [new ColliderNode(-4, -4, 8, 8)],
                        });
                        Explode(enemy.Position);
                        dead = true;
                        break;
                    }
                }
            }
            if (dead) continue;

            //enemy collision w player
            if (PlayerVulnerable
            && CollisionManager.CheckCollisionSimple(ship.Colliders[0], enemy.Colliders[0], ship.Position, enemy.Position))
            {
                _asm.ScheduleAction(frameNumber + 1, () => currentRoom.Enemies.Remove(enemy));
                HitPlayer();
            }
        }
    }

    private void UpdatePickups(InputState inputState)
    {
        var fireVectorLengthSquared = new Vector2(inputState[(int)InputSignal.FireHorizontal], inputState[(int)InputSignal.FireVertical]).LengthSquared();
        for (int i = pickups.Count - 1; i >= 0; i--)
        {
            var pickup = pickups[i];
            pickup.Update(null, frameNumber, inputState);

            if (fireVectorLengthSquared == 0)
            {
                var vector = ship.Position - pickup.Position;
                if (vector.LengthSquared() > 0)
                    pickup.Velocity = vector / vector.LengthSquared() * pickupTime * 4;
                if (pickup.Velocity.LengthSquared() > vector.LengthSquared())
                {
                    pickup.Velocity = Vector2.Normalize(pickup.Velocity) * vector.Length();
                }
            }
            else
            {
                pickup.Velocity *= 0.8f;
            }
            pickup.Position += pickup.Velocity;

            if (CollisionManager.CheckCollisionSimple(ship.Colliders[0], pickup.Colliders[0], ship.Position, pickup.Position))
            {
                pickups.Remove(pickup);
                score += 100;
            }
        }

        if (fireVectorLengthSquared > 0)
            pickupTime = 0;
        else
            pickupTime++;
    }

    private void UpdateBullets()
    {
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            var bullet = bullets[i];
            bullet.Update(null, frameNumber, _inputManager.InputState);
            
            //wall collisions
            foreach (var wall in currentRoom.Walls)
            {
                if (CollisionManager.CheckCollisionSimple(bullet.Colliders[0], wall, bullet.Position, Vector2.Zero))
                {
                    bullets.Remove(bullet);
                    break;
                }
            }

            // if (bullet.X < -8 || bullet.Y < -8 || bullet.X > _camera.Size.X + 8 || bullet.Y > _camera.Size.Y + 8)
            // {
            //     bullets.Remove(bullet);
            //     continue;
            // }

            //bullet collision w player
            if (PlayerVulnerable
            && CollisionManager.CheckCollisionSimple(ship.Colliders[0], bullet.Colliders[0], ship.Position, bullet.Position))
            {
                _asm.ScheduleAction(frameNumber + 1, () => bullets.Remove(bullet));
                HitPlayer();
            }
        }
    }

    private void UpdateParticles()
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var particle = particles[i];
            particle.Update(null, frameNumber, _inputManager.InputState);
            if (particle.Sprites[0].AnimationOver)
            {
                particles.Remove(particle);
            }
        }
    }

    #endregion

    #region Misc methods

    public static Vector2 PickRandomVelocity(float speed)
    {
        var possibleVelocities = new Vector2[] {
            new (speed, 0),
            new (0, speed),
            new (-speed, 0),
            new (0, -speed),
        };
        return possibleVelocities[random.Next(possibleVelocities.Length)];
    }

    private void Explode(Vector2 position)
    {
        for (int k = 0; k < 6; k++)
        {
            var particle = new Particle(position + MathHelper.AngleToVector(k * MathF.PI / 3, 12))
            {
                Sprites = [_juicyCM.GenerateSprite("spritesheet", "explosion")],
                Velocity = new(0, -random.NextSingle() - 1),
            };
            particle.Sprites[0].FrameRatio = 0.5f;
            particle.Sprites[0].FrameIndex = random.Next(0, 2);
            particles.Add(particle);
        }
        for (int i = 0; i < 10; i++)
        {
            MakeSpark(position, random.NextSingle() * MathF.PI * 2);
        }
        var flash = new Particle(position)
        { 
            Sprites = [_juicyCM.GenerateSprite("spritesheet", "explosion flash")],
        };
        flash.Sprites[0].FrameRatio = 0.5f;
        particles.Add(flash);
    }

    private void MakeSpark(Vector2 position, float direction)
    {
        var dirString = JuicyContentManager.DirectionString(slopeNames, direction);
        particles.Add(new Particle(position)
        {
            Sprites = [_juicyCM.GenerateSprite("spritesheet", "spark " + dirString)],
            Velocity = MathHelper.AngleToVector(direction, 3),
        });
    }

    private void HitPlayer()
    {
        lives -= 1;
        freezeFrames = 30;
        iFramesEnd = frameNumber + 120;
        //schedule an explosion for next frame so that we can see the bullet and the ship during the freeze frame
        _asm.ScheduleAction(frameNumber + 1, () => {
            Explode(ship.Position);
            if (lives <= 0)
            {
                gameState = GameState.GameOver;
            }
        });
    }

    private static bool CheckLoS(Vector2 a, Vector2 b, List<ColliderNode> colliders)
    {
        foreach (var collider in colliders)
        {
            if (CollisionManager.CheckCollisionLine(collider, Vector2.Zero, a, b))
                return false;
        }
        return true;
    }

    protected void ToggleFullScreen()
    {
        _graphics.IsFullScreen = !_graphics.IsFullScreen;
        if (_graphics.IsFullScreen)
        {
            _graphics.PreferredBackBufferWidth = GraphicsDevice.Adapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsDevice.Adapter.CurrentDisplayMode.Height;
        }
        else
        {
            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Height;
        }
        _graphics.ApplyChanges();
        OnResizeWindow(null, null);
    }

    protected void OnResizeWindow(object sender, EventArgs e)
    {
        int xScale = Math.Max(1, Window.ClientBounds.Width / _camera.GameRect.Width);
        int yScale = Math.Max(1, Window.ClientBounds.Height / _camera.GameRect.Height);
        int scale = Math.Min(xScale, yScale);
        Point size = new(_camera.GameRect.Width * scale, _camera.GameRect.Height * scale);
        Point location = new((Window.ClientBounds.Width - size.X) / 2, (Window.ClientBounds.Height - size.Y) / 2);
        _camera.ViewRect = new Rectangle(location, size);
        _guiCamera.ViewRect = new Rectangle(location, size);
    }

    public static void DefaultControls(InputManager inputManager)
    {
        inputManager.SetBinding(InputMode.KeyboardOnly, (int)InputSignal.HorizontalMovement, new KeyInput(Keys.A, Keys.D));
        inputManager.SetBinding(InputMode.KeyboardOnly, (int)InputSignal.VerticalMovement, new KeyInput(Keys.W, Keys.S));
        inputManager.SetBinding(InputMode.KeyboardOnly, (int)InputSignal.FireHorizontal, new KeyInput(Keys.Left, Keys.Right));
        inputManager.SetBinding(InputMode.KeyboardOnly, (int)InputSignal.FireVertical, new KeyInput(Keys.Up, Keys.Down));
        inputManager.SetBinding(InputMode.KeyboardOnly, (int)InputSignal.Accept, new KeyInput(Keys.Enter));

        inputManager.SetBinding(InputMode.MouseAndKeyboard, (int)InputSignal.HorizontalMovement, new KeyInput(Keys.A, Keys.D));
        inputManager.SetBinding(InputMode.MouseAndKeyboard, (int)InputSignal.VerticalMovement, new KeyInput(Keys.W, Keys.S));
        inputManager.SetBinding(InputMode.MouseAndKeyboard, (int)InputSignal.FireHorizontal, new MouseAxisInput(MouseAxes.MouseX));
        inputManager.SetBinding(InputMode.MouseAndKeyboard, (int)InputSignal.FireVertical, new MouseAxisInput(MouseAxes.MouseY));
        inputManager.SetBinding(InputMode.MouseAndKeyboard, (int)InputSignal.Accept, new MouseButtonInput(MouseButtons.LeftButton));

        inputManager.SetBinding(InputMode.XBoxController, (int)InputSignal.HorizontalMovement, new GamePadAxisInput(GamePadAxes.LeftStickX));
        inputManager.SetBinding(InputMode.XBoxController, (int)InputSignal.VerticalMovement, new GamePadAxisInput(GamePadAxes.LeftStickY, true));
        inputManager.SetBinding(InputMode.XBoxController, (int)InputSignal.FireHorizontal, new GamePadAxisInput(GamePadAxes.RightStickX));
        inputManager.SetBinding(InputMode.XBoxController, (int)InputSignal.FireVertical, new GamePadAxisInput(GamePadAxes.RightStickY, true));
        inputManager.SetBinding(InputMode.XBoxController, (int)InputSignal.Accept, new GamePadButtonInput(Buttons.A));
    }
    
    #endregion

    #region Draw methods

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            rasterizerState: _rasterizerState,
            effect: _betterBlend,
            blendState: BlendState.NonPremultiplied
        );
        _spriteBatch.GraphicsDevice.ScissorRectangle = _camera.ViewRect;

        _camera.Draw(_whitePixel, _camera.GameRect, new Color(20, 24, 46));

        currentRoom.Draw(_camera);

        foreach (var enemy in currentRoom.Enemies)
        {
            enemy.Draw(null, _camera, Vector2.Zero);
            // DrawCollider(_camera, enemy.Colliders[0], Color.Red, enemy.Position);
            var color = CheckLoS(enemy.Position, ship.Position, currentRoom.Walls) ? Color.Red : Color.White;
            // DrawLine(_camera, enemy.Position, ship.Position, color);
        }

        foreach (var pickup in pickups)
        {
            pickup.Draw(null, _camera, Vector2.Zero);
        }
        
        if (gameState != GameState.GameOver)
        {
            foreach (var shot in shots)
            {
                shot.Draw(null, _camera, Vector2.Zero);
                // DrawCollider(_camera, shot.Colliders[0], Color.Blue, shot.Position);
            }
            if (freezeFrames > 0 || frameNumber > iFramesEnd || frameNumber % 20 < 10)
                ship.Draw(null, _camera, Vector2.Zero);
            // DrawCollider(_camera, ship.Colliders[0], Color.Blue, ship.Position);
        }

        // foreach (var wall in walls)
        // {
        //     DrawCollider(_camera, wall, Color.White, Vector2.Zero);
        // }

        foreach (var particle in particles)
        {
            particle.Draw(null, _camera, Vector2.Zero);
        }
        // foreach (var flash in explosionFlashes)
        // {
        //     // var perc = flash.Lifetime / 8f;
        //     // var radius = (int)(16 * perc);
        //     // var color = new Color(255, perc * 0.75f + 0.25f, perc);
        //     _camera.Draw(_circle16, new Rectangle(flash.Position.ToPoint() - new Point(16), new Point(32)), Color.White);
        // }
        
        foreach (var bullet in bullets)
        {
            bullet.Draw(null, _camera, Vector2.Zero);
            // _camera.Draw(_whitePixel, new Rectangle((bullet.Position + bullet.Colliders[0].Position).ToPoint(), bullet.Colliders[0].Dimensions.ToPoint()), Color.Red);
        }

        if (gameState == GameState.Title)
        {
            _guiCamera.DrawString(_juicyCM.Fonts["blocky sans"], "Bullet Hail", _guiCamera.GameRect.Center.ToVector2(), Color.White, TextHorizontal.CenterAligned, TextVertical.BottomAligned);
            _guiCamera.DrawString(_juicyCM.Fonts["mostly sans"], "Press A to start", _guiCamera.GameRect.Center.ToVector2(), Color.White, TextHorizontal.CenterAligned, TextVertical.TopAligned);
        }
        else if (gameState == GameState.GameOver)
        {
            _guiCamera.DrawString(_juicyCM.Fonts["blocky sans"], "Game Over", _guiCamera.GameRect.Center.ToVector2(), Color.White, TextHorizontal.CenterAligned, TextVertical.BottomAligned);
            _guiCamera.DrawString(_juicyCM.Fonts["mostly sans"], "Press A to restart", _guiCamera.GameRect.Center.ToVector2(), Color.White, TextHorizontal.CenterAligned, TextVertical.TopAligned);
        }

        _guiCamera.DrawString(_juicyCM.Fonts["blocky sans"], "Score: " + score, new Vector2(1), Color.White);
        _guiCamera.DrawString(_juicyCM.Fonts["blocky sans"], "Lives: " + lives, new Vector2(_guiCamera.GameRect.Width - 1, 1), Color.White, TextHorizontal.RightAligned);
        
        DrawMap(new (0, 164, 28, 28));

        //_spriteBatch.Draw(_juicyCM.Textures["spritesheet"], _camera.ViewRect, new Rectangle(0, 0, 160, 20), Color.Transparent);
        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawCollider(Camera camera, ColliderNode collider, Color color, Vector2 referencePosition)
    {
        camera.Draw(_whitePixel, new Rectangle((referencePosition + collider.Position - collider.Dimensions / 2).ToPoint(), collider.Dimensions.ToPoint()), color);
    }

    private void DrawLine(Camera camera, Vector2 point1, Vector2 point2, Color color, float thickness = 1f)
    {
        var distance = Vector2.Distance(point1, point2);
        var angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
        DrawLine(camera, point1, distance, angle, color, thickness);
    }

    private void DrawLine(Camera camera, Vector2 point, float length, float angle, Color color, float thickness = 1f)
    {
        var scale = new Vector2(length, thickness);
        camera.Draw(_whitePixel, new(point.ToPoint(), scale.ToPoint()), color, angle);
    }

    private void DrawMap(Rectangle area)
    {
        for (int x = 0; x < levelWidth; x++)
        {
            for (int y = 0; y < levelHeight; y++)
            {
                var index = x + y * levelWidth;
                if (level[index] is null)
                    continue;

                var roomArea = new Rectangle(
                    area.X + x * area.Width / levelWidth,
                    area.Y + y * area.Height / levelHeight,
                    area.Width / levelWidth,
                    area.Height / levelHeight
                );
                var color = Color.White;
                if (index == currentCell)
                    color = Color.Blue;
                _guiCamera.Draw(_whitePixel, roomArea, color);
            }
        }
    }

    #endregion
}
