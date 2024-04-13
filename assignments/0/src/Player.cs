using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

class Player
{
    // constants
    const float MaxSpeed = 1000f;
    const float Acceleration = 10f;
    const float RoadFriction = 5f;
    const float MinTurnRate = 0.01f; // 0.57 degrees
    const float MaxTurnRate = 0.03f; // 1,7 degrees
    const float MaxSpeedTurnFactor = 200f; // 1,7 degrees
    const float JumpVelocity = 1500f;
    const float Gravity = 3500f; // custom world gravity for the jump
    const float BaseScale = 1.0f;
    const float ScaleFactor = 0.008f;
    const float MaxScale = 1.05f;

    // state 
    private float speed = 0f;
    private bool isAccelerating = false;
    private float angle = 0;
    private float jumpHeight = 0f;
    private float jumpTime = 0;
    private bool isJumping = false;
    private float scale = 1;

    public Model Model { get; set; }
    public Vector3 Pos { get; set; } = Vector3.Zero;
    public Matrix World;

    public void Initialize()
    {
        UpdateWorldMatrix();
    }

    public void Load(ContentManager Content, string modelsFolder)
    {
        Model = Content.Load<Model>(modelsFolder + "scene/car");
    }

    public void Draw(Matrix View, Matrix Projection)
    {
        Model.Draw(World, View, Projection);
    }

    public void Update(GameTime gameTime)
    {
        KeyboardState State = Keyboard.GetState();
        HandleInput(State);
        ApplyPhysics(gameTime);
        UpdateWorldMatrix();
    }

    private void HandleInput(KeyboardState keyState)
    {
        if (keyState.IsKeyDown(Keys.W))
            Accelerate();
        else isAccelerating = false;
        if (keyState.IsKeyDown(Keys.S))
            Decelerate();
        if (keyState.IsKeyDown(Keys.D))
            Turn(1);
        if (keyState.IsKeyDown(Keys.A))
            Turn(-1);
        if (keyState.IsKeyDown(Keys.Space) && !isJumping)
            StartJump();
    }

    private void Accelerate()
    {
        // the signs are inverted coz of the model
        speed = Math.Max(-MaxSpeed, speed - Acceleration);
        // play scaling animation when accelerating
        scale = Math.Min(MaxScale, scale + ScaleFactor);
        isAccelerating = true;
    }

    private void Decelerate() => speed = Math.Min(MaxSpeed, speed + Acceleration);

    private float GetTurnRatio() =>
        // a fun and straightforward arcade-mode turning algo
        // depends on some constants defined by us through experimentation but, most importantly, on the currentSpeed
        // with the given values, the max turn ration (when currentSpeed is at its max) is 8.5deg
        MinTurnRate + (MaxTurnRate - MinTurnRate) * (speed / MaxSpeedTurnFactor);

    private void Turn(int dir) => angle += speed != 0 ? dir * GetTurnRatio() : 0;

    private void StartJump()
    {
        isJumping = true;
        jumpHeight = 0;
        jumpTime = 0;
    }

    private void ApplyPhysics(GameTime gameTime)
    {
        float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (isJumping) UpdateJump(elapsedTime);
        else ApplyRoadFriction();

        Pos += new Vector3((float)Math.Sin(angle) * speed, jumpHeight, (float)Math.Cos(angle) * speed) * elapsedTime;
    }

    private void UpdateJump(float elapsedTime)
    {
        jumpTime += elapsedTime;
        jumpHeight = JumpVelocity * jumpTime - 0.5f * Gravity * jumpTime * jumpTime;
        float newPosY = Pos.Y + jumpHeight * elapsedTime;

        if (newPosY < 0)
        {
            // land the jump
            Pos += new Vector3(0, -Pos.Y, 0);
            jumpHeight = 0;
            isJumping = false;
        }
    }

    private void ApplyRoadFriction()
    {
        if (!isAccelerating && scale > BaseScale)
            scale = Math.Max(BaseScale, scale - ScaleFactor);
        // it is not moving, so obviously the road is not applying any friction force
        if (speed == 0) return;
        if (speed > 0) speed = Math.Max(0, speed - RoadFriction);
        if (speed < 0) speed = Math.Min(0, speed + RoadFriction);
    }

    private void UpdateWorldMatrix()
    {
        World = Matrix.CreateScale(1, 1, scale) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(Pos);
    }
}