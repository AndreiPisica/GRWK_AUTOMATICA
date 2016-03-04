using System;
using System.Collections.Generic;
using System.Linq;
using DigitalRune.Animation;
using DigitalRune.Animation.Easing;
using DigitalRune.Game.Input;
using DigitalRune.Game.States;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Game.UI.Rendering;
using DigitalRune.Graphics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Threading;
using Microsoft.Practices.ServiceLocation;
using DigitalRune.ServiceLocation;
using KinectGame.AppCore.MappingSkeleton;
using DigitalRune.Game;

namespace KinectGame.AppCore
{
     public class States : DrawableGameComponent
    {
        private TextBlock _loadingTextBlock;

        private StateMachine _stateMachine;
        private  UIScreen _uiScreen;
        private readonly IInputService _inputService;
        private readonly IUIService _uiService;
        private readonly IAnimationService _animationService;
        private UIScreen _screen;
        protected readonly IGraphicsService _graphicsService;


        public string currentGameState;
        public bool drawColorBox;
 
        public States(Game game)
         : base(game)
        {
          
            // Get the required services from the game's service provider.
            _inputService = (IInputService)game.Services.GetService(typeof(IInputService));
            _uiService = (IUIService)game.Services.GetService(typeof(IUIService));
            _animationService = (IAnimationService)game.Services.GetService(typeof(IAnimationService));
           
            currentGameState = "Loading";
           
            drawColorBox = true;
            CreateStateMachine();
            
        }

        protected override void LoadContent()
        {
          //  learning = Game.Components.OfType<Learning>().First();
          
            // Load a UI theme, which defines the appearance and default values of UI controls.
            var theme = Game.Content.Load<Theme>("UI Theme/BlendBlue/Theme");

            // Create a UI renderer, which uses the theme info to renderer UI controls.
            var renderer = new UIRenderer(Game, theme);
           
            // Create a UIScreen and add it to the UI service. The screen is the root of 
            // the tree of UI controls. Each screen can have its own renderer. 
            _screen = new UIScreen("Application", renderer)
            {
                // Make the screen transparent.
                Background = new Color(0, 0, 0, 0),
            };
            _uiScreen = new UIScreen("UIScreen", renderer)
            {
                Background = new Color(0, 0, 0, 0),            
            };

            // Add the screen to the UI service.
            _uiService.Screens.Add(_screen);
            _uiService.Screens.Add(_uiScreen);
           // learning.Visible = false;
            
            base.LoadContent();
        }


        public override void Draw(GameTime gameTime)
        {
            // Clear background.
            GraphicsDevice.Clear(new Color(50, 50, 50));

            // Draw the UI screen. 
            _screen.Draw(gameTime);
            _uiScreen.Draw(gameTime);
        }


        private void CreateStateMachine()
        {
            _stateMachine = new StateMachine();

            var loadingState = new State { Name = "Loading" };
            loadingState.Enter += OnEnterLoadingScreen;
            loadingState.Exit += OnExitLoadingScreen;

            // The "Menu" state represents the main menu. It provides buttons to start 
            // the game, show sub menus, and exit the game.
            var menuState = new State { Name = "Menu" };
            menuState.Enter += OnEnterMenuScreen;
            menuState.Exit += OnExitMenuScreen;

            var LearningState = new State { Name = "LearningState" };
            LearningState.Enter += OnEnterLearningScreen;
            LearningState.Update += OnUpdateLearningScreen;
            LearningState.Exit += OnExitLearningScreen;
           

            // The "Game" state is a placeholder for the actual game content.
            var gameState = new State { Name = "Game" };
            gameState.Enter += OnEnterGameScreen;
            gameState.Update += OnUpdateGameScreen;
            gameState.Exit += OnExitGameScreen;

            // Register the states in the state machine.
            _stateMachine.States.Add(loadingState);
            _stateMachine.States.Add(menuState);
            _stateMachine.States.Add(LearningState);
            _stateMachine.States.Add(gameState);

            _stateMachine.States.InitialState = loadingState;

            // ----- Next we can define the allowed transitions between states.

            // The "Loading" screen will transition to the "Start" screen once all assets
            // are loaded. The assets are loaded in the background. The background worker 
            // sets the flag _allAssetsLoaded when it has finished.
            // The transition should fire automatically. To achieve this we can set FireAlways 
            // to true and define a Guard. A Guard is a condition that needs to be fulfilled 
            // to enable the transition. This way the game component automatically switches 
            // from the "Loading" state to the "Start" state once the loading is complete.
            var loadToMenuTransition = new Transition
            {
                Name = "LoadingToMenu",
                TargetState = menuState,
                FireAlways = true,                // Always trigger the transition, if the guard allows it.
                Guard = () => _allAssetsLoaded,   // Enable the transition when _allAssetsLoaded is true.
            };
            loadingState.Transitions.Add(loadToMenuTransition);

            // The remaining transition need to be triggered manually.
            var menuToGameTransition = new Transition
            {
                Name = "MenuToGame",
                TargetState = gameState,
            };
            menuState.Transitions.Add(menuToGameTransition);
            var menuToLearningTransition = new Transition
            {
                Name = "MenuToLearning",
                TargetState = LearningState,
            };
            menuState.Transitions.Add(menuToLearningTransition);

            var gameToMenuTransition = new Transition
            {
                Name = "GameToMenu",
                TargetState = menuState,
            };
            gameState.Transitions.Add(gameToMenuTransition);

            var LearningToMenuTransition = new Transition
            {
                Name = "LearningToMenu",
                TargetState = menuState,
            };
            LearningState.Transitions.Add(LearningToMenuTransition);
        }

        #region ----- Loading State -----
        private volatile bool _allAssetsLoaded;
        private void OnEnterLoadingScreen(object sender, StateEventArgs eventArgs)
        {
           
            // Show the text "Loading..." centered on the screen.
            _loadingTextBlock = new TextBlock
            {
                Name = "LoadingTextBlock",    // Control names are optional - but very helpful for debugging!
                Text = "Loading...",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _screen.Children.Add(_loadingTextBlock);
            currentGameState = "Loading";
            // Start loading assets in the background.
            Parallel.StartBackground(LoadAssets);
        }
        private void LoadAssets()
        {
            // To simulate a loading process we simply wait for 2 seconds.
            Thread.Sleep(TimeSpan.FromSeconds(2));
            _allAssetsLoaded = true;
        }
        private void OnExitLoadingScreen(object sender, StateEventArgs eventArgs)
        {
            // Clean up.
            _screen.Children.Remove(_loadingTextBlock);
            _loadingTextBlock = null;
        }
        #endregion
        #region ----- Menu State -----
        private Window _menuWindow;
        private AnimationController _menuExitAnimationController;

        private void OnEnterMenuScreen(object sender, StateEventArgs eventArgs)
        {
            // Show a main menu consisting of several buttons.

            _menuWindow = new Window();
            _menuWindow.HorizontalAlignment = HorizontalAlignment.Stretch;
            _menuWindow.VerticalAlignment = VerticalAlignment.Stretch;
            
      

            // The content of the Window is a vertical StackPanel containing several buttons.
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Vector4F(50, 0, 0,500)
               
            };
           
            _menuWindow.Content = stackPanel;
            _menuWindow.Name = "Meniu";
           


            _uiScreen.Children.Add(_menuWindow);




            // The "Start" button starts the "Game" state.
            var startButton = new Button
            {
                Name = "StartButton",
                Content = new TextBlock { Text = "Start" },
                FocusWhenMouseOver = true,
            };
            startButton.Click += OnStartButtonClicked;

            // The buttons "Sub menu 1" and "Sub menu 2" show a dummy sub-menu.
            var subMenu1Button = new Button
            {
                Name = "Learn Gestures",
                Content = new TextBlock { Text = "Learn Gestures" },
                FocusWhenMouseOver = true,
            };
            subMenu1Button.Click += OnLearningButtonClicked;

            var subMenu2Button = new Button
            {
                Name = "SubMenu2Button",
                Content = new TextBlock { Text = "Sub-menu 2" },
                FocusWhenMouseOver = true,
            };
         //   subMenu2Button.Click += OnSubMenuButtonClicked;

            // The "Exit" button closes the application.
            var exitButton = new Button
            {
                Name = "ExitButton",
                Content = new TextBlock { Text = "Exit" },
                FocusWhenMouseOver = true,
            };
            exitButton.Click += OnExitButtonClicked;

            stackPanel.Children.Add(startButton);
            stackPanel.Children.Add(subMenu1Button);
            stackPanel.Children.Add(subMenu2Button);
            stackPanel.Children.Add(exitButton);

            // By default, the first button should be selected.
            startButton.Focus();

            // Slide the buttons in from the left (off screen) to make things more dynamic.
            AnimateFrom(stackPanel.Children, 0, new Vector2F(-300, 0));
            currentGameState = "Menu";
            // The first time initialization of the GUI can take a short time. If we reset the elapsed 
            // time of the XNA game timer, the animation will start a lot smoother. 
            // (This works only if the XNA game uses a variable time step.)
            Game.ResetElapsedTime();
        
        }

        private void OnStartButtonClicked(object sender, EventArgs eventArgs)
        {
            // Animate all buttons within the StackPanel to opacity 0 and offset (-300, 0).
            var stackPanel = (StackPanel)_menuWindow.Content;
            _menuExitAnimationController = AnimateTo(stackPanel.Children, 0, new Vector2F(-300, 0));

            // When the last animation finishes, trigger the "MenuToGame" transition.
            _menuExitAnimationController.Completed +=
              (s, e) => _stateMachine.States.ActiveState.Transitions["MenuToGame"].Fire();

            // Disable all buttons. The user should not be able to click a button while 
            // the fade-out animation is playing.
            DisableMenuItems();
        }
        private void OnLearningButtonClicked(object sender,EventArgs eventArgs)
        {
            var stackPanel = (StackPanel)_menuWindow.Content;
            _menuExitAnimationController = AnimateTo(stackPanel.Children, 0, new Vector2F(-300, 0));
            _menuExitAnimationController.Completed +=
              (s, e) => _stateMachine.States.ActiveState.Transitions["MenuToLearning"].Fire();
            DisableMenuItems();
        }
        private void OnExitButtonClicked(object sender, EventArgs eventArgs)
        {
            // Animate all buttons within the StackPanel to opacity 0 and offset (-300, 0).
            var stackPanel = (StackPanel)_menuWindow.Content;
            _menuExitAnimationController = AnimateTo(stackPanel.Children, 0, new Vector2F(-300, 0));

            // When the last animation finishes, exit the game.
            _menuExitAnimationController.Completed += (s, e) =>
            {
            
                Game.Exit();

            };

            // Disable all buttons. The user should not be able to click a button while 
            // the fade-out animation is playing.
            DisableMenuItems();
        }
        private void DisableMenuItems()
        {
            var stackPanel = (StackPanel)_menuWindow.Content;
            foreach (var button in stackPanel.Children)
                button.IsEnabled = false;
        }
        private void OnExitMenuScreen(object sender, StateEventArgs eventArgs)
        {
            // Clean up.
            _menuExitAnimationController.Stop();
            _menuExitAnimationController.Recycle();

            _uiScreen.Children.Remove(_menuWindow);
            _menuWindow = null;
        }

        #endregion
        #region ----- Learning State -----


        private void OnEnterLearningScreen(object sender, StateEventArgs eventArgs)
        {
            currentGameState = "Learning";
        
        }


        private void OnUpdateLearningScreen(object sender, StateEventArgs eventArgs)
        {
 
            if(_inputService.IsPressed(Keys.Escape,false))
            {
                _inputService.IsKeyboardHandled = true;
                ExitLearningScreen();
            }
            if (_inputService.IsPressed(Keys.X, false))
            {
                drawColorBox = !drawColorBox;
            }
            
        }


        private void ExitLearningScreen()
        {
            _stateMachine.States.ActiveState.Transitions["LearningToMenu"].Fire();

        }



        private void OnExitLearningScreen(object sender, StateEventArgs eventArgs)
        {
            // Clean up.
            /*_subMenuExitAnimationController.Stop();
            _subMenuExitAnimationController.Recycle();
            _subMenuExitAnimationIsPlaying = false;

            _screen.Children.Remove(_LearningWindow);
            _LearningWindow = null;*/
        }
        #endregion
        #region ----- Game State -----

        private TextBlock _gameTextBlock;

        /// <summary>
        /// Called when "Game" state is entered.
        /// </summary>
        private void OnEnterGameScreen(object sender, StateEventArgs eventArgs)
        {
            // Show a dummy text.
            _gameTextBlock = new TextBlock
            {
                Text = "Game is running. (Press Back button to return to menu.)",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            _uiScreen.Children.Add(_gameTextBlock);
            currentGameState = "Game";
            
        }

        private void OnUpdateGameScreen(object sender, StateEventArgs eventArgs)
        {
            // Exit the "Game" state if Back button or Escape key is pressed.
            
            if (_inputService.IsPressed(Buttons.Back, false, LogicalPlayerIndex.One)
                || _inputService.IsPressed(Keys.Escape, false))
            {
                _inputService.IsKeyboardHandled = true;
                _stateMachine.States.ActiveState.Transitions["GameToMenu"].Fire();
            }

            if(_inputService.IsPressed(Keys.X,false))
            {
                drawColorBox = !drawColorBox;
            }
           
        }


        private void OnExitGameScreen(object sender, StateEventArgs eventArgs)
        {
            // Clean up.
            _uiScreen.Children.Remove(_gameTextBlock);
            _gameTextBlock = null;
            
        }
        #endregion


        #region ----- Animation Helpers -----
        private void AnimateFrom(IList<UIControl> controls, float opacity, Vector2F offset)
        {
            TimeSpan duration = TimeSpan.FromSeconds(0.8);

            // First, let's define the animation that is going to be applied to a control.
            // Animate the "Opacity" from the specified value to its current value.
            var opacityAnimation = new SingleFromToByAnimation
            {
                TargetProperty = "Opacity",
                From = opacity,
                Duration = duration,
                EasingFunction = new CubicEase { Mode = EasingMode.EaseOut },
            };

            // Animate the "RenderTranslation" property from the specified offset to its
            // its current value, which is usually (0, 0).
            var offsetAnimation = new Vector2FFromToByAnimation
            {
                TargetProperty = "RenderTranslation",
                From = offset,
                Duration = duration,
                EasingFunction = new CubicEase { Mode = EasingMode.EaseOut },
            };

            // Group the opacity and offset animation together using a TimelineGroup.
            var timelineGroup = new TimelineGroup();
            timelineGroup.Add(opacityAnimation);
            timelineGroup.Add(offsetAnimation);

            // Run the animation on each control using a negative delay to give the first controls
            // a slight head start.
            var numberOfControls = controls.Count;
            for (int i = 0; i < controls.Count; i++)
            {
                var clip = new TimelineClip(timelineGroup)
                {
                    Delay = TimeSpan.FromSeconds(-0.04 * (numberOfControls - i)),
                    FillBehavior = FillBehavior.Stop,   // Stop and remove the animation when it is done.
                };
                var animationController = _animationService.StartAnimation(clip, controls[i]);

                animationController.UpdateAndApply();

                // Enable "auto-recycling" to ensure that the animation resources are recycled once
                // the animation stops or the target objects are garbage collected.
                animationController.AutoRecycle();
            }
        }
        private AnimationController AnimateTo(IList<UIControl> controls, float opacity, Vector2F offset)
        {
            TimeSpan duration = TimeSpan.FromSeconds(0.6f);

            // First, let's define the animation that is going to be applied to a control.
            // Animate the "Opacity" from its current value to the specified value.
            var opacityAnimation = new SingleFromToByAnimation
            {
                TargetProperty = "Opacity",
                To = opacity,
                Duration = duration,
                EasingFunction = new CubicEase { Mode = EasingMode.EaseIn },
            };

            // Animate the "RenderTranslation" property from its current value, which is 
            // usually (0, 0), to the specified value.
            var offsetAnimation = new Vector2FFromToByAnimation
            {
                TargetProperty = "RenderTranslation",
                To = offset,
                Duration = duration,
                EasingFunction = new CubicEase { Mode = EasingMode.EaseIn },
            };

            // Group the opacity and offset animation together using a TimelineGroup.
            var timelineGroup = new TimelineGroup();
            timelineGroup.Add(opacityAnimation);
            timelineGroup.Add(offsetAnimation);

            // Now we duplicate this animation by creating new TimelineClips that wrap the TimelineGroup.
            // A TimelineClip is assigned to a target by setting the TargetObject property.
            var storyboard = new TimelineGroup();

            for (int i = 0; i < controls.Count; i++)
            {
                var clip = new TimelineClip(timelineGroup)
                {
                    TargetObject = controls[i].Name,  // Assign the clip to the i-th control.
                    Delay = TimeSpan.FromSeconds(0.04f * i),
                    FillBehavior = FillBehavior.Hold, // Hold the last value of the animation when it            
                };                                  // because we don't want to opacity and offset to
                                                    // jump back to their original value.
                storyboard.Add(clip);
            }
            var animationController = _animationService.StartAnimation(storyboard, controls.Cast<IAnimatableObject>());
            animationController.UpdateAndApply();
            return animationController;
        }
        #endregion
     


        public override void Update(GameTime gameTime)
        {
            _stateMachine.Update(gameTime.ElapsedGameTime);
        }

     
    }
}
