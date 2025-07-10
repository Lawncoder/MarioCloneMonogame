using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Mario.Components;
using Mario.Enemy;
using Mario.Helpers;
using Mario.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;
using nkast.Aether.Physics2D.Collision;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using Color = Microsoft.Xna.Framework.Color;
using Path = System.IO.Path;
using Vector2 = nkast.Aether.Physics2D.Common.Vector2;
using World = Arch.Core.World;



namespace Mario;

public class Game1 : Game
{
    const int SCREEN_WIDTH = 320;
    const int SCREEN_HEIGHT = 180;
    private const int TILE_WIDTH = 16;
    
    
    private CommandBuffer _commandBuffer;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    public static World EntityWorld { get;private set; }
    public static nkast.Aether.Physics2D.Dynamics.World PhysicsWorld { get;private set; }

    private SpriteFont _default;
    
    private Transform _camera;

    private Color _scoreColor = Color.Red;
    private Color _targetColor = Color.Red;
    
    public static ContentManager SContent { get;private set; }

    private List<Sprite> SpriteBuffer;
    
    private List<AnimatedSprite> AnimatedSpriteBuffer;
    
    public static KeyboardInfo KeyboardState { get; private set; }

    private int _score = 0;
    
    private CollisionLayers Ground =  CollisionLayers.Ground;
    private CollisionLayers Player =  CollisionLayers.Player;
    private CollisionLayers Enemy = CollisionLayers.Enemy;
    private CollisionLayers Block = CollisionLayers.Block;
    private CollisionLayers Wall = CollisionLayers.Wall;
    private CollisionLayers Boundary = CollisionLayers.Boundary;
    
    private Body ground;
    private Sprite mario;
    private Sprite goomba;
    private Sprite questionBlock;
    private Sprite shell;
    private Sprite koopaTroopa;
    private Body collisionBoundary;

    RenderTarget2D renderTarget;
    public const float TILE_SIZE = 1f;
    private Tilemap _map;
    private SpriteFont _defaultX5;
    
    private MouseInfo _mouseInfo = new MouseInfo();
    float _timePassedSinceLastPhysicsUpdate = 0;
    
    private const float JUMP_BUFFER = 0.1f;
    private float _timePassedSinceLastJumpButtonPressed = JUMP_BUFFER;
    public static float MaxCameraX = 160;
    private bool _jumpButtonReleasedSinceLastFalling = true;
    private List<BaseSystem<World, float>> _systems;
    public static CommandBuffer CommandBuffer { get; private set;  }
    public bool ResolveCollisions(Contact contact)
    {
           
       
        if (CollisionAssistant.CategoryInCategories(Player, contact.FixtureB.CollisionCategories) &&
            CollisionAssistant.CategoryInCategories(Block, contact.FixtureA.CollisionCategories))
        {
            //Player collided with a block, that's it. Will have a query handle all game logic
            var blockHitComponent = new HitComponent();
            var normal = Vector2.One;
            contact.GetWorldManifold(out normal, out blockHitComponent.Points);
            blockHitComponent.CollisionLayer = contact.FixtureB.CollisionCategories;
            blockHitComponent.CollisionNormal = normal;
            
        

            var query = new QueryDescription().WithAll<QuestionBlockComponent, Fixture>();
            EntityWorld.Query(query, (Entity entity, ref Fixture fixture) =>
            {
                if (fixture == contact.FixtureA)
                {
                    
                    if (!EntityWorld.Has<HitComponent>(entity))
                    {
                        EntityWorld.Add(entity, blockHitComponent);
                        

                    }
                }
            });

        }
        else
        {
            var query = new QueryDescription().WithAll<Fixture>();
            Entity entityA = Entity.Null;
            Entity entityB = Entity.Null;
            EntityWorld.Query(in query, (Entity entity, ref Fixture fixture) =>
            {
                if (fixture == contact.FixtureA)
                {
                    
                    entityA = entity;
                    
                }
                if (fixture == contact.FixtureB)
                {
                    entityB = entity;
                }
            });
            
            var hitComponent = new HitComponent();
            var normal = Vector2.One;
            contact.GetWorldManifold(out normal, out hitComponent.Points);
            hitComponent.CollisionLayer = contact.FixtureB.CollisionCategories;
            hitComponent.CollisionNormal = normal;
            hitComponent.Position = contact.FixtureB.Body.Position;
            hitComponent.Fixture = contact.FixtureB;
            hitComponent.Body = contact.FixtureB.Body;
            hitComponent.Entity = entityB;
                    
                    
                    
            _commandBuffer.Add(entityA, hitComponent);
            
            hitComponent = new HitComponent();
            var normal2 = Vector2.One;
            contact.GetWorldManifold(out normal2, out hitComponent.Points);
            hitComponent.CollisionLayer = contact.FixtureA.CollisionCategories;
            hitComponent.CollisionNormal = normal2;
            hitComponent.Position = contact.FixtureA.Body.Position;
            hitComponent.Fixture = contact.FixtureA;
            hitComponent.Body = contact.FixtureA.Body;
            hitComponent.Entity = entityA;
                    
                    
                    
            _commandBuffer.Add(entityB, hitComponent);
        }
        
        
        
        return true;
    }
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        PhysicsWorld = new nkast.Aether.Physics2D.Dynamics.World();
        EntityWorld = World.Create();
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        _camera = new Transform();
        _camera.Position = new Vector2(0, 0);
        _camera.Scale = new Vector2(1,1);
        _camera.Rotation = 0f;
        PhysicsWorld.Gravity = PhysicsWorld.Gravity * -5; 
        KeyboardState = new  KeyboardInfo();
        SpriteBuffer = new List<Sprite>();
        AnimatedSpriteBuffer = new List<AnimatedSprite>();
        SContent = Content;
        _commandBuffer = new CommandBuffer();
        _systems = new  List<BaseSystem<World, float>>();
        CommandBuffer = _commandBuffer;
        _systems.Add(new DamageSystem(EntityWorld));
        _systems.Add(new KoopaTroopaShellSystem(EntityWorld));
        _systems.Add(new EnemyPatrolSystem(EntityWorld));
        
       
        


        PhysicsWorld.ContactManager.BeginContact += new BeginContactDelegate(ResolveCollisions);

    }


 
    
    protected override void Initialize()
    {
        renderTarget = new RenderTarget2D(GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT);
    
        
        
        base.Initialize();


        CreateQuestionBlock(new QuestionBlockComponent()
        {
            QuestionBlockComponentType = QuestionBlockComponent.QuestionBlockComponentTypes.Score,
            Score = 100
        }, new Vector2(24, 5));
        CreateQuestionBlock(new QuestionBlockComponent()
        {
            QuestionBlockComponentType = QuestionBlockComponent.QuestionBlockComponentTypes.Score,
            Score = 100
        }, new Vector2(25, 5));
        CreateQuestionBlock(new QuestionBlockComponent()
        {
            QuestionBlockComponentType = QuestionBlockComponent.QuestionBlockComponentTypes.Score,
            Score = 100
        }, new Vector2(26, 5));

        #region Initialize Mario and Camera

        PlatformerCharacter marioData = new PlatformerCharacter();
        marioData.Acceleration = 30f;
        marioData.MaxSpeed = 5f;
        marioData.State = PlatformerCharacter.States.Falling;
        marioData.AutoCalculateForce(PhysicsWorld.Gravity.Y, 4.5f);
       
        
        var marioBody = PhysicsWorld.CreateBody(new Vector2(2, 10), 0f, BodyType.Dynamic);
        marioBody.FixedRotation = true;
        var marioCollider = marioBody.CreateRectangle(1f, 1f, 1, Vector2.Zero);
        marioCollider.Tag = "Mario";
        marioCollider.Friction = 0f;
        marioCollider.Restitution = 0f;
        marioCollider.CollidesWith = CollisionAssistant.CategoryFromLayers(Enemy, Ground, Block, Boundary, Wall);
        marioCollider.CollisionCategories = CollisionAssistant.CategoryFromLayers(Player);

   
        Transform marioTransform = new Transform();
        marioTransform.Position = new Vector2(0, 0);
        marioTransform.Rotation = 0f;
        marioTransform.Scale = new Vector2(16/348f,16/348f);
        EntityWorld.Create(marioBody, marioCollider, "mario",marioTransform, new SpriteTypes(SpriteTypes.SpriteTypesEnum.Mario), marioData);
        
       

       
        
        
        var cameraBoundryBody = PhysicsWorld.CreateBody(new Vector2(0, 15f), 0f, BodyType.Static);
        var cameraBoundryFixture = cameraBoundryBody.CreateRectangle(0.5f,40f, 1f, Vector2.Zero);
        cameraBoundryFixture.Tag = "CameraBoundry";
        cameraBoundryFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(Player);
        cameraBoundryFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Boundary);
        collisionBoundary = cameraBoundryBody;

        EntityWorld.Create(cameraBoundryFixture, cameraBoundryBody, "cameraBoundry", new Transform());

        #endregion
        
        
       
        CreateKoopa(new Vector2(12,10));
        CreateGoomba(new Vector2(13,10));
        CreateHurtbox(new Vector2(22, 11), 3, 2);

        foreach (var s in _systems)
        {
            s.Initialize();
        }

    }

    private void CreateKoopa(Vector2 position)
    {
        var koopaTroopaBody = PhysicsWorld.CreateBody(position, 0f, BodyType.Dynamic);
        koopaTroopaBody.FixedRotation = true;
        var koopaTroopaFixture =  koopaTroopaBody.CreateRectangle(1f, 1f, 1f, Vector2.Zero);
        koopaTroopaFixture.Tag = "Koopa";
        koopaTroopaFixture.Restitution = 0f;
        koopaTroopaFixture.Friction = 0f;
        koopaTroopaFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(Ground, Block, Enemy, Player);
        koopaTroopaFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Enemy);
        var koopaPatrol = new Patrol();
        var koopaTransform = new Transform()
        {
            Scale = new  Vector2(TILE_WIDTH/196f,TILE_WIDTH/290f),
        };
        var koopaKoopa = new KoopaTroopaComponent();
        EntityWorld.Create(koopaKoopa, koopaPatrol, koopaTransform, koopaTroopaBody, koopaTroopaFixture, "Koopa", new SpriteTypes(SpriteTypes.SpriteTypesEnum.Koopa));
    }

    private void CreateQuestionBlock(QuestionBlockComponent blockComponent, Vector2 position)
    {
       
       
        var blockBody = PhysicsWorld.CreateBody(position + Vector2.One*.5f, 0f, BodyType.Static);
        var blockFixture = blockBody.CreateRectangle(TILE_SIZE, TILE_SIZE, 1f, Vector2.Zero);
        blockFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Block);
        
        blockFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(Player, Enemy);
        blockFixture.Tag = "Block";
        
        
        EntityWorld.Create("QBlock", blockComponent, blockBody, blockFixture, new SpriteTypes(SpriteTypes.SpriteTypesEnum.Question), new Transform()
        {
            Scale= new  Vector2(16/200f, 16/191f)
        });

    }
    private void CreateGoomba(Vector2 position)
    {
        var goombaBody = PhysicsWorld.CreateBody(position, 0f, BodyType.Dynamic);
        goombaBody.FixedRotation = true;
        var goombaFixture = goombaBody.CreateRectangle(1, 1, 1, Vector2.Zero);
        goombaFixture.Tag = "Goomba";
        goombaFixture.Restitution = 0.1f;
        goombaFixture.Friction = 0f;
        goombaFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(Player, Ground, Block, Enemy);
        goombaFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Enemy);
        var goombaPatrol = new Patrol();
        var goombaTransform = new Transform();
        goombaTransform.Scale = new Vector2(TILE_WIDTH/728f,TILE_WIDTH/660f);
        EntityWorld.Create(goombaPatrol, goombaBody, "goomba",goombaTransform, goombaFixture, new SpriteTypes(SpriteTypes.SpriteTypesEnum.Goomba));
    }



    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _default = Content.Load<SpriteFont>("Default");
        _defaultX5 = Content.Load<SpriteFont>("DefaultX5");
        mario = Sprite.FromFile("mario");
        goomba = Sprite.FromFile("goomba");
        questionBlock = Sprite.FromFile("questionBlock");
        koopaTroopa =  Sprite.FromFile("koopaTroopa");
        shell =  Sprite.FromFile("shell");
        _map = Tilemap.FromFile(Content,"map.json", true);
        CreateCollisionLayer("map.json");

    }

    private List<Span> spanData;
    public void CreateCollisionLayer(string jsonFilename)
    {
        bool[][] collisionTile;
        string filePath = Path.Combine(Content.RootDirectory, jsonFilename);
        using (var stream = TitleContainer.OpenStream(filePath))
        {
           
            using (JsonDocument doc = JsonDocument.Parse(stream))
            {
                collisionTile = new bool[doc.RootElement.GetProperty("mapWidth").GetInt32()][];
                for (var i = 0; i < doc.RootElement.GetProperty("mapWidth").GetInt32(); i++)
                {
                    collisionTile[i] = new bool[doc.RootElement.GetProperty("mapHeight").GetInt32()];
                }
                foreach (var tile in doc.RootElement.GetProperty("layers")[0].GetProperty("tiles").EnumerateArray())
                {
                    collisionTile[tile.GetProperty("x").GetInt32()][tile.GetProperty("y").GetInt32()] = true;

                }
            }
        }

        List<Span> spans = new List<Span>();
        spanData = spans;

        for (int i = 0; i < collisionTile[0].Length; i++)
        {
            Span currentSpan = null;
            for (int j = 0; j < collisionTile.Length; j++)
            {
                if (collisionTile[j][i])
                {
                    if (currentSpan == null)
                    {
                        currentSpan = new Span
                        {
                            x_start = j,
                            y = i,
                            length = 1
                        };
                    }
                    else
                    {
                        currentSpan.length++;
                    }
                }
                else
                {
                    if (currentSpan != null)
                    {
                        spans.Add(currentSpan);
                        
                    }
                    currentSpan = null;
                    
                }
            }

            if (currentSpan != null)
            {
                spans.Add(currentSpan);
            }
        }
        List<Rectangle> rectangles = new List<Rectangle>();
        Dictionary<Vector2, List<Span>> spanMerges = new Dictionary<Vector2, List<Span>>();
        foreach (var span in spans)
        {
           
            Vector2 key = new Vector2(span.length, span.x_start);
            if (!spanMerges.ContainsKey(key))
            {
                spanMerges.Add(key, new List<Span>());
            }
            spanMerges[key].Add(span);
        }
        
        foreach (var list in spanMerges.Values)
        {
            
            int minY = list[0].y;
            int maxY = list[0].y;
            foreach (var span in list)
            {
                minY = Math.Min(minY, span.y);
                maxY = Math.Max(maxY, span.y);
                
            }
     
            Rectangle newRect = new Rectangle();
            newRect.X = list[0].x_start;
            newRect.Y = minY;
            newRect.Width = list[0].length;
            newRect.Height = maxY - minY + 1;
            
            CreateGround(new Vector2(newRect.X, newRect.Y), newRect.Width, newRect.Height);
        }
    }

    private class Span
    {
        public int x_start;
        public int length;
        public int y;
        public override string ToString()
        {
            return $"X: {x_start}, Y: {y}, Length: {length}";
        }
    }

    public void CreateGround(Vector2 position, int width, int height)
    {
        var fixture = PhysicsWorld.CreateBody(position).CreateRectangle(width, height, 1, new Vector2(width/2f, height/2f));
        fixture.CollidesWith = Category.All;
        fixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Ground);
        fixture.Tag = "Ground";
        fixture.Body.Tag = "Ground";
        EntityWorld.Create(fixture, fixture.Body, "ground");
    }

    public void CreateHurtbox(Vector2 position, int width, int height)
    {
        var fixture = PhysicsWorld.CreateBody(position).CreateRectangle(width, height, 1, new Vector2(width/2f, height/2f));
        fixture.CollidesWith = Category.All;
        fixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(CollisionLayers.Killer, Ground);
        fixture.Tag = "Ground";
        fixture.Body.Tag = "Ground";
        EntityWorld.Create(fixture, fixture.Body, "ground");
    }



    private bool _goombaSpawnFlag = false;
    protected override void Update(GameTime gameTime)
    {
        Console.WriteLine(mario.Position);
       
        foreach (var s in _systems)
        {
            s.BeforeUpdate(gameTime.ElapsedGameTime.Milliseconds/1000f);
        }

        if (MaxCameraX / 16f > 15 && !_goombaSpawnFlag)
        {
            _goombaSpawnFlag = true;
            CreateGoomba(new Vector2(27,9));
        }
        
        
        
        KeyboardState.Update();
        _mouseInfo.Update();
        //Escaping the Program
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit(); 
        
        foreach (var s in _systems)
        {
            s.Update(gameTime.ElapsedGameTime.Milliseconds/1000f);
        }
        
        
        _scoreColor = Color.Lerp(_scoreColor, _targetColor, 0.3f);
        
        
        
        
        //Fixed Update Code
        _timePassedSinceLastPhysicsUpdate += gameTime.ElapsedGameTime.Milliseconds;
        _timePassedSinceLastJumpButtonPressed += gameTime.ElapsedGameTime.Milliseconds;
        
       if (_timePassedSinceLastPhysicsUpdate >= 20f)
       {
           
           PhysicsUpdate(gameTime);
       }

       if (KeyboardState.WasKeyJustPressedThisFrame(Keys.Space))
       {
           _timePassedSinceLastJumpButtonPressed = 0f;
       }
        
       
       
            
       
       
       
       
      
     
       
       //Handling Question Boxes
       var query = new QueryDescription().WithAll<HitComponent, QuestionBlockComponent>();
       EntityWorld.Query(query, (Entity entity, ref HitComponent block, ref QuestionBlockComponent blockComponent) =>
       {
           if (block.CollisionNormal.Equals(GameDevMath.Up))
           {
               _commandBuffer.Destroy(entity);
               PhysicsWorld.Remove(EntityWorld.Get<Body>(entity));
               _score += blockComponent.Score;
               _targetColor = Color.Lerp(Color.Red, Color.Green, blockComponent.Score/100f);
              
           }
           else
           {
               EntityWorld.Remove<HitComponent>(entity);   
           }
                                
       });
       
       
      
       
       
       
       
       collisionBoundary.Position = new Vector2(-_camera.Position.X/TILE_WIDTH, collisionBoundary.Position.Y);
      
       

      
      


       
      
       
        base.Update(gameTime);
        
        
        foreach (var s in _systems)
        {
            s.AfterUpdate(gameTime.ElapsedGameTime.Milliseconds/1000f);
        }
        _commandBuffer.Playback(EntityWorld);
    }


    private bool _flipMario = false;
    protected void PhysicsUpdate(GameTime gameTime)
    {
        
        
        //Moving Mario
        var marioQuery = new QueryDescription().WithAll<PlatformerCharacter, Body>();
        EntityWorld.Query(in marioQuery, (Entity entity, ref Transform transform, ref PlatformerCharacter platformerCharacter, ref Body body) =>
        {

            MaxCameraX = (float)Math.Max(MaxCameraX, GameDevMath.Physics2ScreenVector(body.Position).X);
            _camera.Position.X = -MaxCameraX + SCREEN_WIDTH * .5f/_camera.Scale.X;
            
            var grounded = false;
            PhysicsWorld.RayCast((fixture, point, normal, fraction) =>
            {
               
                if ((string)fixture.Body.Tag == "Ground")
                {
                    grounded = true;
                    return fraction;
                }

                return 1f;
            }, body.Position + new Vector2(0.5f, 0), body.Position + new Vector2(0,0.55f));
            if (!grounded)
            {
                PhysicsWorld.RayCast((fixture, point, normal, fraction) =>
                {
               
                    if ((string)fixture.Body.Tag == "Ground")
                    {
                        grounded = true;
                        return fraction;
                    }

                    return 1f;
                }, body.Position - new Vector2(0.5f, 0), body.Position + new Vector2(0,0.55f));
            }
            
            var direction = KeyboardState.IsKeyDown(Keys.Right)?1f:0f - (KeyboardState.IsKeyDown(Keys.Left)?1f:0f);
            _flipMario = direction switch
            {
                > 0 => false,
                < 0 => true,
                _ => _flipMario
            };

            bool jump = _timePassedSinceLastJumpButtonPressed < JUMP_BUFFER*1000f;
           
            //Applies Gravity. Change this for dashes, floating states etc.
            var velocityY=body.LinearVelocity.Y;
          
            if (!grounded)
            {
                velocityY += platformerCharacter.Gravity;
              
            }
            
            
            var velocityX = body.LinearVelocity.X;
            switch (platformerCharacter.State)
            {
                case PlatformerCharacter.States.Falling:
                    if (grounded)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Grounded;
                    }
                    if (direction != 0f)
                    {
                        velocityX += direction * platformerCharacter.Acceleration * 0.02f * 0.2f;
                       
                    }
                    else
                    {
                        if (velocityX * velocityX > 0.1f)
                        {
                            velocityX += Math.Sign(-velocityX) * platformerCharacter.Acceleration * 0.02f * 0.2f;
                        }
                        else
                        {
                            velocityX *= 0f;
                        }
                    }

                  
                   
                    velocityX = Math.Clamp(velocityX, -platformerCharacter.MaxSpeed, platformerCharacter.MaxSpeed);
                    body.LinearVelocity= new Vector2(velocityX, body.LinearVelocity.Y);
                    if (EntityWorld.Has<HitComponent>(entity))
                    {
                        HitComponent hitComponent = EntityWorld.Get<HitComponent>(entity);
                        if (hitComponent.CollisionNormal.Y > 0.8 && CollisionAssistant.CategoryInCategories(CollisionLayers.Enemy, hitComponent.CollisionLayer))
                        {
                            
                            body.LinearVelocity = new Vector2(body.LinearVelocity.X, (float)-Math.Sqrt(3*PhysicsWorld.Gravity.Y));
                            
                        }
                        _commandBuffer.Remove<HitComponent>(entity);
                    }
                    
                    break;
                
                case PlatformerCharacter.States.Grounded:
                    
                    if (direction != 0f)
                    {
                        velocityX += direction * platformerCharacter.Acceleration * 0.02f;
                       
                    }
                    else
                    {
                        if (velocityX * velocityX > 0.1f)
                        {
                            velocityX += Math.Sign(-velocityX) * platformerCharacter.Acceleration * 0.02f;
                        }
                        else
                        {
                            velocityX *= 0f;
                        }
                    }
                   
                    velocityX = Math.Clamp(velocityX, -platformerCharacter.MaxSpeed, platformerCharacter.MaxSpeed);
                    body.LinearVelocity= new Vector2(velocityX, body.LinearVelocity.Y);

                    if (jump && _jumpButtonReleasedSinceLastFalling)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Jumping;
                        body.LinearVelocity = new  Vector2(body.LinearVelocity.X, -platformerCharacter.JumpForce);
                        _jumpButtonReleasedSinceLastFalling = false;
                    }
                    
                    if (body.LinearVelocity.Y > 0.001 && !grounded)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Falling;
                    }

                   
                    break;

                case PlatformerCharacter.States.Jumping:
                    if (body.LinearVelocity.Y > 0.001 && !grounded)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Falling;
                    }

                    if (grounded)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Grounded;
                    }

                    if (direction != 0f)
                    {
                        velocityX += direction * platformerCharacter.Acceleration * 0.02f * 0.2f;

                    }
                    else
                    {
                        if (velocityX * velocityX > 0.1f)
                        {
                            velocityX += Math.Sign(-velocityX) * platformerCharacter.Acceleration * 0.02f * 0.2f;
                        }
                        else
                        {
                            velocityX *= 0f;
                        }
                        
                    }

                    if (KeyboardState.IsKeyUp(Keys.Space))
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Falling;
                        velocityY *= 0.5f;
                        
                    }
                   
                    velocityX = Math.Clamp(velocityX, -platformerCharacter.MaxSpeed, platformerCharacter.MaxSpeed);
                    body.LinearVelocity= new Vector2(velocityX, velocityY);
                    break;
            }
            if (KeyboardState.IsKeyUp(Keys.Space))
            {
                _jumpButtonReleasedSinceLastFalling = true;
            }
            
         
           
   
        });

        PhysicsWorld.Step(0.02f);
        
        var query = new QueryDescription().WithAll<Body, Transform, Fixture>();
        EntityWorld.Query(in query, (Entity Entity, ref Body body, ref Transform transform, ref Fixture fixture) =>
        {
            transform.Rotation = body.Rotation;
            transform.Position = body.Position;



        });

        
        //Here is ensuring we don't get 2 physics updates while another is activating
        _timePassedSinceLastPhysicsUpdate = 0f;
    }
    protected override void Draw(GameTime gameTime)
    {
       
        GraphicsDevice.SetRenderTarget(renderTarget);
        GraphicsDevice.Clear(Color.White);
        

        _spriteBatch.Begin(transformMatrix: _camera.ToMatrix(), samplerState:  SamplerState.PointClamp);
        //Draw Tilemap
        _spriteBatch.DrawCircle(Microsoft.Xna.Framework.Vector2.Zero, 5f, 20, Color.Wheat);
       _map.Draw(_spriteBatch);
       
        
       

        var query = new QueryDescription().WithAll<SpriteTypes>();
        EntityWorld.Query(in query, (Entity entity, ref SpriteTypes spriteType, ref Transform transform) =>
        {
            var sprite =  spriteType.SpriteType switch
            {
                SpriteTypes.SpriteTypesEnum.Mario => mario,
                SpriteTypes.SpriteTypesEnum.Koopa => koopaTroopa,
                SpriteTypes.SpriteTypesEnum.Goomba =>  goomba,
                SpriteTypes.SpriteTypesEnum.Shell =>  shell,
                SpriteTypes.SpriteTypesEnum.Question =>   questionBlock
            };
           
            
            sprite.Position = GameDevMath.Physics2ScreenVector(transform.Position);
            sprite.Scale = new Microsoft.Xna.Framework.Vector2(transform.Scale.X, transform.Scale.Y);
            sprite.Rotation = transform.Rotation;
            sprite.CenterOrigin();
            if (spriteType.SpriteType == SpriteTypes.SpriteTypesEnum.Mario)
            {
                sprite.Effects = _flipMario ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            }
            sprite.Draw(spriteBatch: _spriteBatch);
            
        });
        
        query = new QueryDescription().WithAll<Fixture>();
        EntityWorld.Query(in query, (Entity entity, ref Fixture fixture, ref Body body) =>
        {
            var shape = (PolygonShape)fixture.Shape;
            for (int i = 0; i < shape.Vertices.Count; i++)
            {
                bool killer = CollisionAssistant.CategoryInCategories(CollisionLayers.Killer, fixture.CollisionCategories);
                _spriteBatch.DrawLine(GameDevMath.Physics2ScreenVector(body.Position + shape.Vertices[i]), GameDevMath.Physics2ScreenVector(body.Position + shape.Vertices[(i+1)%shape.Vertices.Count]), killer ? Color.Red : Color.Aquamarine);
            }
        });
        
        
        
        _spriteBatch.End();
        
        
        
        
        GraphicsDevice.SetRenderTarget(null);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        var region = new TextureRegion(renderTarget, 0, 0, renderTarget.Width, renderTarget.Height);
        region.Draw(_spriteBatch, Microsoft.Xna.Framework.Vector2.Zero, Color.White, 0f, Microsoft.Xna.Framework.Vector2.Zero, 6f, SpriteEffects.None, 1f);
        _spriteBatch.End();
        //UI
        _spriteBatch.Begin();
        
        _spriteBatch.DrawString(_defaultX5, _score.ToString(), new Microsoft.Xna.Framework.Vector2(0, 0), _scoreColor);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    public enum Type
    {
        Player, Goomba, Koopa, Tile, QuestionBlock
    }
    struct Hitbox(FixedArray4<Vector2> vertices,Vector2 position,string name)
    {
        public readonly FixedArray4<Vector2> Vertices = vertices;
        public readonly Vector2 Position = position;
        public readonly string Name = name;
    }
    
}