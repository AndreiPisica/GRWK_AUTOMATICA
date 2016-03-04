using DigitalRune.Game.Input;
using KinectGame.AppCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;

namespace KinectGame.GeneralComponents
{
    public class Help : DrawableGameComponent
    {
        // The help text that is drawn when F1 is pressed.
        private const string Text = "Help Text\n"
          + "----------------------------------------------------\n"
          + "Keyboard/Mouse:\n"
          + "  Press <F1> to display this help text.\n"
          + "  Press <Esc> to return to menu.\n"
            +"Press <X> to enable/disable ColorStream.\n"
          + "  Press <Home> to reset camera position.\n"
          + "\n";

        private readonly IInputService _inputService;
        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;

        private States st;

        public Help(Game game)
          : base(game)
        {
            _inputService = (IInputService)game.Services.GetService(typeof(IInputService));
         
        }


        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _spriteFont = Game.Content.Load<SpriteFont>("SpriteFont1");

            base.LoadContent();
        }


        public override void Draw(GameTime gameTime)
        {

            if (_inputService.IsDown(Keys.F1) || _inputService.IsDown(Buttons.Start, PlayerIndex.One))
            {
                // F1 is pressed:

                // Clear screen.
                GraphicsDevice.Clear(Color.White);

                // Draw help text.
                float left = MathHelper.Max(GraphicsDevice.Viewport.TitleSafeArea.Left, 20);
                float top = MathHelper.Max(GraphicsDevice.Viewport.TitleSafeArea.Top, 20);
                Vector2 position = new Vector2(left, top);
                _spriteBatch.Begin();
                _spriteBatch.DrawString(_spriteFont, Text, position, Color.Black);
                _spriteBatch.End();
            }
            else 
            {
                // F1 is not pressed:

                // Draw a help hint at the bottom of the screen.
                float left = MathHelper.Max(GraphicsDevice.Viewport.TitleSafeArea.Left, 20);
                float bottom = MathHelper.Min(GraphicsDevice.Viewport.TitleSafeArea.Bottom - 20, GraphicsDevice.Viewport.Height - 30);
                Vector2 position = new Vector2(left, bottom);
                const string text = "Press <F1> or <Start> to display Help";
                _spriteBatch.Begin();
                _spriteBatch.DrawString(_spriteFont, text, position, Color.White);
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}
