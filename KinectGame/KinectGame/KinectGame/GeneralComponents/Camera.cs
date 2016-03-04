using DigitalRune.Game.Input;
using DigitalRune.Geometry;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace KinectGame.GeneralComponents
{
    public class Camera : DrawableGameComponent
    {
        private readonly IInputService _inputService;

        // Some constants for motion control.
        private const float LinearVelocityMagnitude = 5f;
        private const float AngularVelocityMagnitude = 0.1f;
        private const int MouseOrigin = 300;
        private const float ThumbStickFactor = 15;

        // Position and Orientation of camera.
        private Vector3F _position;
        private float _yaw;
        private float _pitch;


        // The pose (position and orientation) of the camera.
        public Pose Pose { get; private set; }

        // The view matrix.
        public Matrix View { get; private set; }

        // The projection matrix.
        public Matrix Projection { get; private set; }


        public Camera(Game game)
          : base(game)
        {
            _inputService = (IInputService)game.Services.GetService(typeof(IInputService));

            ResetPose();
        }


        private void ResetPose()
        {
            _position = new Vector3F(0, 1.0f, 1);
            _yaw = 0;
            _pitch = 0;
        }


        protected override void LoadContent()
        {
            // Create the projection matrix.
            Projection = Matrix.CreatePerspectiveFieldOfView(
              MathHelper.ToRadians(45),
              GraphicsDevice.Viewport.AspectRatio,
              0.1f,
              1000f);

            base.LoadContent();
        }


        public override void Update(GameTime gameTime)
        {
            if (!Game.IsActive)
            {
                // No mouse-centering when another windows application is in the foreground.
                _inputService.EnableMouseCentering = false;
                return;
            }

        //    _inputService.EnableMouseCentering = true;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Compute new orientation from mouse movement and gamepad.
            GamePadState gamePadState = _inputService.GetGamePadState(PlayerIndex.One);
            Vector2F mousePositionDelta = _inputService.MousePositionDelta;

            float deltaYaw = -mousePositionDelta.X - gamePadState.ThumbSticks.Right.X * ThumbStickFactor;
           // _yaw += deltaYaw * deltaTime * AngularVelocityMagnitude;
            float deltaPitch = -mousePositionDelta.Y + gamePadState.ThumbSticks.Right.Y * ThumbStickFactor;
            //   _pitch += deltaPitch * deltaTime * AngularVelocityMagnitude;
            _yaw = 0;
            _pitch = 0;
            
            // Limit the pitch angle to +/- 90°.
            _pitch = MathHelper.Clamp(_pitch, -ConstantsF.PiOver2, ConstantsF.PiOver2);

            // Reset camera position if <Home> or <Right Stick> is pressed.
            if (_inputService.IsPressed(Keys.Home, false) || _inputService.IsPressed(Buttons.RightStick, false, PlayerIndex.One))
            {
                ResetPose();
            }

            // Compute new orientation of the camera.
            QuaternionF orientation = QuaternionF.CreateRotationY(_yaw) * QuaternionF.CreateRotationX(_pitch);

            // Create velocity from <W>, <A>, <S>, <D> and <R>, <F> keys. 
            // <R> or DPad up is used to move up ("rise"). 
            // <F> or DPad down is used to move down ("fall").
            Vector3F velocity = Vector3F.Zero;
            if (Keyboard.GetState().IsKeyDown(Keys.W))
                velocity.Z--;
            if (Keyboard.GetState().IsKeyDown(Keys.S))
                velocity.Z++;
            if (Keyboard.GetState().IsKeyDown(Keys.A))
                velocity.X--;
            if (Keyboard.GetState().IsKeyDown(Keys.D))
                velocity.X++;
            if (Keyboard.GetState().IsKeyDown(Keys.R) || gamePadState.DPad.Up == ButtonState.Pressed)
                velocity.Y++;
            if (Keyboard.GetState().IsKeyDown(Keys.F) || gamePadState.DPad.Down == ButtonState.Pressed)
                velocity.Y--;

            // Add velocity from gamepad sticks.
            velocity.X += gamePadState.ThumbSticks.Left.X;
            velocity.Z -= gamePadState.ThumbSticks.Left.Y;
            

            // Rotate the velocity vector from view space to world space.
            velocity = orientation.Rotate(velocity);

            // Multiply the velocity by time to get the translation for this frame.
            Vector3F translation = velocity * LinearVelocityMagnitude * deltaTime;

            _position += translation;

            Pose = new Pose(_position, orientation);
            

            // The view matrix is the inverse of the camera pose.
            View = Pose.Inverse;
            _position.X = 0;
            _position.Y = 40;
            _position.Z = 120;
            

            // Reset the mouse position. (The mouse is hidden and should not leave the window in 
            // windowed mode.)
          //  Mouse.SetPosition(MouseOrigin, MouseOrigin);

            base.Update(gameTime);
        }
    }
}
