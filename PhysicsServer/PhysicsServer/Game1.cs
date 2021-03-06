using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Henge3D.Physics;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;

namespace PhysicsServer {
  /// <summary>
  /// This is the main type for your game
  /// TODO FOR YOU
  /// 
  /// Change the ips to something else, not localhost
  /// 
  /// </summary>
  public class Game1 : Microsoft.Xna.Framework.Game {
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;

    bool ENABLE_PHYSICS = true;
    string MODEL = "wall-model-small";

    int blue = 0;
    int red = 0;

    bool resetBlue = false;
    bool resetRed = false;

    // Transform from physics coordinate space to world coordinate space
    Matrix Tpw = Matrix.CreateScale(.1f)
      * Matrix.CreateFromYawPitchRoll(
          MathHelper.ToRadians(-5),
          MathHelper.ToRadians(5),
          MathHelper.ToRadians(0))
      * Matrix.CreateTranslation(0, 0, 0);

    // Transform from world coordinate space to physics coordinate space
    Matrix Twp = Matrix.CreateTranslation(0, 0, 0)
      * Matrix.CreateFromYawPitchRoll(
          MathHelper.ToRadians(5),
          MathHelper.ToRadians(-5),
          MathHelper.ToRadians(0))
      * Matrix.CreateScale(10);
    PhysicsManager physics;

    Model model;
    Model sphere;
    Model cup_bottom;
    Model cup_top;

    Body modelBody;
    Body cupTopBody;
    Body cupBottomBody;

    List<List<Body>> balls = new List<List<Body>>();
    private object ballLock = new object();
    private string ballString = "";

    private TcpClient tcpClient;

    public Game1() {
      graphics = new GraphicsDeviceManager(this);
      Content.RootDirectory = "Content";
      if (ENABLE_PHYSICS) {
        physics = new PhysicsManager(this);
      }
    }

    /// <summary>
    /// Allows the game to perform any initialization it needs to before starting to run.
    /// This is where it can query for any required services and load any non-graphic
    /// related content.  Calling base.Initialize will enumerate through any components
    /// and initialize them as well.
    /// </summary>
    protected override void Initialize() {
      // TODO: Add your initialization logic here

      base.Initialize();
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent() {
      // Create a new SpriteBatch, which can be used to draw textures.
      spriteBatch = new SpriteBatch(GraphicsDevice);
      model = Content.Load<Model>(MODEL);
      sphere = Content.Load<Model>("sphere");
      cup_bottom = Content.Load<Model>("cup_bottom");
      cup_top = Content.Load<Model>("cup_top");

      modelBody = new Body(this, model, "model");
      cupTopBody = new Body(this, cup_top, "cup_top");
      cupBottomBody = new Body(this, cup_bottom, "cup_bottom");
      cupBottomBody.OnCollision += OnCollide;

      if (ENABLE_PHYSICS) {
        transformAndAdd(modelBody, physics, true);
        transformAndAdd(cupTopBody, physics);
        transformAndAdd(cupBottomBody, physics);
        physics.Gravity = new Vector3(0, -9.8f, 0);
      }
      balls.Add(new List<Body>());
      balls.Add(new List<Body>());
      new Thread(new ThreadStart(tcp1)).Start();
      new Thread(new ThreadStart(tcp2)).Start();
    }

    private void transformAndAdd(Body model, PhysicsManager physicsEngine, bool rotate = false) {
      Vector3 scale = new Vector3();
      Vector3 translation = new Vector3();
      Quaternion rotation = new Quaternion();
      Matrix trans = (rotate ? Matrix.CreateRotationZ(MathHelper.ToRadians(-90)) : Matrix.CreateRotationY(MathHelper.ToRadians(-90))) * Twp;
      // Convert the twp matrix into its individual parts
      trans.Decompose(out scale, out rotation, out translation);
      // Scale is same for x, y, and z, so just use x
      model.SetWorld(scale.X, translation, rotation);
      physics.Add(model);
    }

    private bool OnCollide(RigidBody b1, RigidBody b2) {
      Body body = b2 as Body;

      Console.WriteLine("CONTACT " + body.Tag() + " " + String.Equals(body.Tag(), "sphere"));
      if (String.Equals(body.Tag(), "sphere")) {
        Console.WriteLine(body.Player());
        if (body.Player() == 0) {
          blue++;
        } else if (body.Player() == 1) {
          red++;
        }
        physics.Remove(body);
      }
      return false;
    }

    /// <summary>
    /// UnloadContent will be called once per game and is the place to unload
    /// all content.
    /// </summary>
    protected override void UnloadContent() {
      // TODO: Unload any non ContentManager content here
    }

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime) {
      // Allows the game to exit
      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        this.Exit();
      Monitor.Enter(ballLock);
      ballString = serializeBallsToString();
      if (Keyboard.GetState().IsKeyDown(Keys.D1)) {
        resetBlue = true;
      }
      if (Keyboard.GetState().IsKeyDown(Keys.D2)) {
        resetRed = true;
      }
      Monitor.Exit(ballLock);


      base.Update(gameTime);
    }

    private void tcp1() {
      tcpRun("127.0.0.1", 9000);
    }

    private void tcp2() {
      tcpRun("127.0.0.1", 9001);
    }

    private void tcpRun(string ip, int port) {
      IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
      TcpListener listener = new TcpListener(ipEndPoint);
      listener.Start();

      TcpClient tcpClient = listener.AcceptTcpClient();
      Console.WriteLine("Connection to port " + port);
      StreamReader reader = new StreamReader(tcpClient.GetStream());
      StreamWriter writer = new StreamWriter(tcpClient.GetStream());

      while (true) {
        string data = reader.ReadLine();
        if (!data.Equals("")) {
          Console.WriteLine("Adding " + data);
          addBall(data);
        }

        Monitor.Enter(ballLock);
        writer.WriteLine((resetBlue ? 1 : 0) + "*"  + (resetRed ? 1 : 0) + "*" + blue + "*" + red + "*" + ballString);
        resetBlue = false;
        resetRed = false;
        writer.Flush();
        Monitor.Exit(ballLock);
      }
    }

    private string serializeBallsToString() {
      StringBuilder builder = new StringBuilder();
      for (int ind = 0; ind < balls.Count; ind++) {
        for (int i = 0; i < balls[ind].Count; i++) {
          builder.Append(balls[ind][i].Matrix(i != 0));
          if (i < balls[ind].Count - 1) {
            builder.Append("|");
          }
        }
        if (ind < balls.Count - 1) {
          builder.Append("*");
        }
      }
      return builder.ToString();
    }

    private void addBall(string data) {
      String[] parts = data.Split(',');
      int ind = int.Parse(parts[0]);
      Vector3 translation = new Vector3(float.Parse(parts[1]), float.Parse(parts[2]) + .2f, float.Parse(parts[3]));
      float mag = float.Parse(parts[4]);
      float xAngle = float.Parse(parts[5]) + (float)(Math.PI / 9);
      float yAngle = float.Parse(parts[6]);

      Body body = new Body(this, sphere, "sphere", ind);
      body.SetWorld(translation);

      float xVel = mag * (float)(Math.Sin(yAngle) * Math.Sin(xAngle - Math.PI / 2));
      float yVel = -mag * (float)Math.Cos(xAngle - Math.PI / 2);
      float zVel = -mag * (float)(Math.Cos(yAngle) * Math.Sin(xAngle - Math.PI / 2));

      body.SetVelocity(new Vector3(xVel, yVel, zVel), Vector3.Zero);
      for (int i = 0; i < body.Skin.Count; i++) {
        body.Skin.SetMaterial(body.Skin[i], new Material(.8f, .5f));
      }
      physics.Add(body);
      balls[ind].Add(body);

      if (balls[ind].Count > 50) {
        Body toRemove = balls[ind][0];
        balls[ind].RemoveAt(0);
        physics.Remove(toRemove);
      }
    }

    protected override void Draw(GameTime gameTime) {
      GraphicsDevice.Clear(Color.CornflowerBlue);
      base.Draw(gameTime);
    }
  }
}
