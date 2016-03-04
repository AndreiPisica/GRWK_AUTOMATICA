using System.Linq;
using DigitalRune.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DigitalRune.Game.Input;
using DigitalRune.Geometry;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using Microsoft.Xna.Framework.Input;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace KinectGame.GeneralComponents.RigidBodyRendered
{
    public class RigidBodyRenderer : DrawableGameComponent
    {
        private readonly RasterizerState _wireFrameState = new RasterizerState
        {
            FillMode = FillMode.WireFrame,
        };

        private BasicEffect _basicEffect;


        public Simulation Simulation { get; private set; }


        // Determines whether wire frame or solid fill mode should be used.
        public bool DrawWireFrame { get; private set; }


        public RigidBodyRenderer(Game game, Simulation simulation)
          : base(game)
        {
            Simulation = simulation;

            DrawWireFrame = true;
        }


        protected override void LoadContent()
        {
            // Create a basic effect.
            _basicEffect = new BasicEffect(GraphicsDevice);
            _basicEffect.EnableDefaultLighting();
            _basicEffect.PreferPerPixelLighting = true;
            _basicEffect.SpecularColor = new Vector3(0.2f, 0.1f, 0.1f);
            _basicEffect.SpecularPower = 20f;

            base.LoadContent();
        }


        public override void Update(GameTime gameTime)
        {
            foreach (RigidBody body in Simulation.RigidBodies)
            {
                // We use a DrawableShape to render each rigid body.
                // The DrawableShape is stored in body.UserData.
                DrawableShape drawable = body.UserData as DrawableShape;
                if (drawable == null
                    && body.UserData == null)  // If UserData is not available, then the RigidBodyRenderer does not render this body.
                {
                    // The DrawableShape does not yet exist.
                    // --> Search for another body with the same shape and scale.
                    foreach (RigidBody other in Simulation.RigidBodies)
                    {
                        if (other.Shape == body.Shape && other.Scale == body.Scale && other.UserData is DrawableShape)
                        {
                            // There is another rigid body with the same shape and size.
                            // We can share the DrawableShape between the rigid bodies.
                            drawable = (DrawableShape)other.UserData;
                            break;
                        }
                    }

                    // Create a new DrawableShape if we haven't found any.
                    if (drawable == null)
                    {
                        drawable = new DrawableShape();
                        drawable.LoadContent(GraphicsDevice, body);
                    }

                    body.UserData = drawable;
                }
            }

            base.Update(gameTime);
        }


        public override void Draw(GameTime gameTime)
        {
            if (Enabled)
            {
                // 'Camera' is the game component that controls the view.
                Camera camera = Game.Components.OfType<Camera>().First();
                
                _basicEffect.View = camera.View;
                _basicEffect.Projection = camera.Projection;

                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.BlendState = BlendState.Opaque;

                // Draw the rigid bodies of all registered simulations.
                foreach (RigidBody body in Simulation.RigidBodies)
                {
                    DrawableShape drawable = body.UserData as DrawableShape;
                    if (drawable != null)
                    {
                        Color color = Color.Gray;

                        // Switch wireframe rendering and backside culling.
                        if (DrawWireFrame)
                            GraphicsDevice.RasterizerState = _wireFrameState;
                        else
                            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

                        drawable.Draw(GraphicsDevice, _basicEffect, body.Pose, color);
                    }
                }

                // Restore original settings.
                GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            }

            base.Draw(gameTime);
        }
    }
}
