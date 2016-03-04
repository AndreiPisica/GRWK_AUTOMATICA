using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using DigitalRune.Game.Input;
using DigitalRune.Physics;
using KinectGame.GeneralComponents;
using KinectGame.GeneralComponents.RigidBodyRendered;
using KinectGame.AppCore;
using KinectGame.AppCore.MappingSkeleton;
using System.Timers;
using System.IO;

using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.States;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Game.UI.Rendering;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Game;

using KinectSkeleton = Microsoft.Kinect.Skeleton;
using Excel = Microsoft.Office.Interop.Excel;
using DigitalRune.Graphics;
using Microsoft.Kinect;
using Timer = System.Timers.Timer;

namespace KinectGame.AppCore
{
    class Learning : DrawableGameComponent
    {
      
        private readonly IInputService _inputService;
        protected SpriteBatch SpriteBatch { get; private set; }
        protected SpriteFont SpriteFont { get; private set; }
        protected KinectWrapper _kinectWrapper { get; private set; }
        protected Recognition _recognition { get; private set; }
        protected DynamicTimeWarping _dtw { get; private set; }

        private UIScreen _uiLearningScreen;
        private Window _LearningWindow;
        private readonly IUIService _uiService;
        protected readonly IGraphicsService _graphicsService;

      
        private Timer timer;
        private Timer timer2;
        protected Poses p1;
        public  KinectSkeleton kinect_skeletons { get; private set; }
        private TextBox textBox1;
        private DropDownButton dropDown;
        private Button savePose;
     
        private String _WarningMessage=String.Empty;
        private String _InformationMessage = String.Empty;

        public static bool pose_taken;

        private States st;
        Texture2D pixel;
        public Color[] Border;
        public Color _borderColor;
        private int i;
        private int _timp = 0;
        private Poses poses;
        private List<Poses> gesture;
        private JointType[,] segments = new JointType[2,10];


        public Learning(Game game) : base(game)
        {
            _kinectWrapper = Game.Components.OfType<KinectWrapper>().First();
            
            st = Game.Components.OfType<States>().First();

            _inputService = (IInputService)game.Services.GetService(typeof(IInputService));
            _uiService = (IUIService)game.Services.GetService(typeof(IUIService));
            poses = new Poses(game);
         


            segments[0, 0] = JointType.WristRight;      segments[0, 5] = JointType.HipRight;        segments[0, 8] = JointType.ShoulderCenter;
            segments[1, 0] = JointType.ElbowRight;      segments[1, 5] = JointType.KneeRight;       segments[1, 8] = JointType.ShoulderLeft;
            segments[0, 1] = JointType.WristLeft;       segments[0, 6] = JointType.KneeLeft;        segments[0, 9] = JointType.ShoulderCenter;
            segments[1, 1] = JointType.ElbowLeft;       segments[1, 6] = JointType.AnkleLeft;       segments[1, 9] = JointType.ShoulderRight;
            segments[0, 2] = JointType.ElbowRight;      segments[0, 7] = JointType.KneeRight;
            segments[1, 2] = JointType.ShoulderRight;   segments[1, 7] = JointType.AnkleRight;
            segments[0, 3] = JointType.ElbowLeft;       
            segments[1, 3] = JointType.ShoulderLeft;    
           // segments[0, 4] = JointType.Spine;           
           // segments[1, 4] = JointType.HipCenter;       
           // segments[0, 5] = JointType.HipCenter;       
           // segments[1, 5] = JointType.HipLeft;         
           // segments[0, 6] = JointType.HipCenter;
           // segments[1, 6] = JointType.HipRight;
            segments[0, 4] = JointType.HipLeft;
            segments[1, 4] = JointType.KneeLeft;


            //    _animationService = (IAnimationService)game.Services.GetService(typeof(IAnimationService));

            Border = new Color[2];
            timer = new Timer(1000);
            timer2 = new Timer(1000);
            timer2.AutoReset = false;
            timer.AutoReset = false;
            timer.Elapsed += Timer_Elapsed;
            timer2.Elapsed += Timer_Elapsed1;
            i = 2;
            kinect_skeletons = new KinectSkeleton();
            gesture = new List<Poses>();
            pose_taken = false;
        }

        private void Timer_Elapsed1(object sender, ElapsedEventArgs e)
        {
            _timp += 1;
            if (_timp >= 8)
            {

               
                _timp = 0;
                //savePose.IsEnabled = false;
                timer2.Stop();
                i = 2;
                Learn_Gesture();
            }
            else
            {
                timer2.Start();
                if (i % 2 == 0)
                {
                    _borderColor = Border[0];
                }
                else
                {
                    _borderColor = Border[1];
                }
                i += 1;
                if (i >= 4)
                {
                    i = 2;
                }
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timp += 1;
            if(_timp >=8)
            {
                Start_Learning_Pose();
                _timp = 0;
                savePose.IsEnabled = true;
                timer.Stop();
                i = 2;
            }
            else
            {
                timer.Start();
                if (i % 2 == 0)
                {
                    _borderColor = Border[0];
                }
                else
                {
                    _borderColor = Border[1];
                }
                i += 1;
                if(i>=4)
                {
                    i = 2;
                }
            }
            
        }

        protected override void LoadContent()
        {
            _recognition = Game.Components.OfType<Recognition>().First();
            _dtw = Game.Components.OfType<DynamicTimeWarping>().First();
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            SpriteFont = Game.Content.Load<SpriteFont>("SpriteFont1");
            var theme = Game.Content.Load<Theme>("UI Theme/BlendBlue/Theme");
            var renderer = new UIRenderer(Game, theme);

            _uiLearningScreen = new UIScreen("_uiLearningScreen", renderer)
            {
                Background = new Color(0, 0, 0, 0),
            };

            _uiService.Screens.Add(_uiLearningScreen);

            _LearningWindow = new Window
            {
                Name = "Learning control",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                CanDrag = false,
                CanResize = false,
            };
            _LearningWindow.HideOnClose = true;
            _LearningWindow.Width = 350;
            _LearningWindow.Height = 250;
            _uiLearningScreen.Children.Add(_LearningWindow);
            _LearningWindow.Closed += _LearningWindow_Closed;


            var StackPanel = new StackPanel
            {
                Margin = new Vector4F(8),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };

            _LearningWindow.Content = StackPanel;


            var button1 = new Button
            {
                Name = "Learn a pose",
                Content = new TextBlock { Text = "Learn a new pose" },
                FocusWhenMouseOver = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Vector4F(50, 0, 0, 0),
           

            };
            button1.Click += Button1_Click;

            var learn_dynamic_gesture = new Button
            {
                Name = "Learn a new dynamic gesture",
                Content = new TextBlock { Text = "Learn a gesture" },
                FocusWhenMouseOver = true,
               // HorizontalAlignment = HorizontalAlignment.Left,
              //  VerticalAlignment = VerticalAlignment.Center,
                X=200,
                Y=-18,
             //   Margin = new Vector4F(15, 0, 15, 25),
            };
            learn_dynamic_gesture.Click += Learn_dynamic_gesture_Click;

            textBox1 = new TextBox
            {
                Name = "Name of pose",
                FocusWhenMouseOver = true,
              //  HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 140,
                Margin = new Vector4F(0, 30, 0, 0),
                X=70,
                Y=-21,
            };


         savePose = new Button
            {
                Name = "SavePose",
                Content = new TextBlock { Text = "Save Pose/Gesture" },
                X = 220,
                Y = -21,
                Margin = new Vector4F(0, 2, 2, 2),
            };
            savePose.Click += SavePose_Click;

            var refreshPoses = new Button
            {
                Name = "RefreshPoses",
                Content = new TextBlock { Text = "Refresh" },
                X = 55,
                Y = 10,
            };
            refreshPoses.Click += RefreshPoses_Click;

            var adaptselectedPose = new Button
            {
                Name = "AdaptPose",
                Content = new TextBlock { Text = "Adapt pose" },
                X = 145,
                Y = -10,
            };
            adaptselectedPose.Click += AdaptselectedPose_Click;



             dropDown = new DropDownButton
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Vector4F(4),
                MaxDropDownHeight = 250,
            };
            
            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            xlApp = new Excel.Application();

            if (!System.IO.Directory.GetFiles("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent", "poses.xls", System.IO.SearchOption.AllDirectories).Any())
            {
               
            }
            else
            {
                xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\poses.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                foreach (Excel.Worksheet xlWork in xlWorkBook.Worksheets)
                {
                    dropDown.Items.Add(xlWork.Name.ToString());
                }
                dropDown.SelectedIndex = 0;

                xlApp.Quit();
                releaseObject(xlWorkBook);
            }

            //  Excel.Sheets worksheets = xlWorkBook.Worksheets;
            releaseObject(xlApp);
            
          




            StackPanel.Children.Add(button1);
            StackPanel.Children.Add(learn_dynamic_gesture);
            StackPanel.Children.Add(textBox1);
            StackPanel.Children.Add(savePose);
            StackPanel.Children.Add(dropDown);
            StackPanel.Children.Add(refreshPoses);
            StackPanel.Children.Add(adaptselectedPose);

            _LearningWindow.IsVisible = false;

            button1.Focus();
            savePose.IsEnabled = false;


            Border[0] = Color.Red;
            Border[1] = Color.Green;

            pixel = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            pixel.SetData(new[] { Color.White });
           

            base.LoadContent();

        }

        private void Learn_dynamic_gesture_Click(object sender, EventArgs e)
        {
            timer2.Start();
            textBox1.IsEnabled = true;
            savePose.IsEnabled = true;
        }

        private void AdaptselectedPose_Click(object sender, EventArgs e)
        {
            if(dropDown.Items.Count>0)
            {
                timer2.Start();
                textBox1.IsEnabled = false;
                savePose.IsEnabled = false;
            }
        }

        private void RefreshPoses_Click(object sender, EventArgs e)
        {
            dropDown.Items.RemoveRange(0, dropDown.Items.Count);

            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
        
            xlApp = new Excel.Application();
            xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\poses.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            foreach (Excel.Worksheet xlWork in xlWorkBook.Worksheets)
            {
                dropDown.Items.Add(xlWork.Name.ToString());
            }
            dropDown.SelectedIndex = 0;

            xlApp.Quit();
            releaseObject(xlWorkBook);
            releaseObject(xlApp);
        }

        private void _LearningWindow_Closed(object sender, EventArgs e)
        {
            _LearningWindow.IsVisible = false;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            timer.Start();
            textBox1.IsEnabled = true;
            savePose.IsEnabled = true;
        }

        private void SavePose_Click(object sender, EventArgs e)
        {
            if ((textBox1.Text != String.Empty) && (textBox1.Text != "Please name the pose!"))
            {
                
                textBox1.IsEnabled = false;
                Save_Pose(textBox1.Text);
                savePose.IsEnabled = false;
                textBox1.Text = String.Empty;
            }
            else
            {
                textBox1.Text = ("Please name the pose!");
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (st.currentGameState == "Learning")
            {
               // _LearningWindow.IsVisible = true;
                GraphicsDevice.Clear(new Color(50, 50, 50));
                _uiLearningScreen.Draw(gameTime);

                var position = new Vector2(400, 550);
                var position2 = new Vector2(350, 260);

                SpriteBatch.Begin();
                // Create any rectangle you want. Here we'll use the TitleSafeArea for fun.
                Rectangle titleSafeRectangle = GraphicsDevice.Viewport.TitleSafeArea;
                // Call our method (also defined in this blog-post)
                DrawBorder(5, _borderColor);
                SpriteBatch.DrawString(SpriteFont, _WarningMessage, position, Color.Red, 0, new Vector2(10, 10), 3, SpriteEffects.None, 1);
                SpriteBatch.DrawString(SpriteFont, _InformationMessage, position2, Color.LightGreen, 0, new Vector2(10, 10), 1.5f, SpriteEffects.None, 1);


                SpriteBatch.End();
            }
            else
            {
                _LearningWindow.IsVisible = false;
                return;
            }
        }

        private void DrawBorder( int thicknessOfBorder, Color borderColor)
        {
            // Draw top line
            SpriteBatch.Draw(pixel, new Rectangle(110, 72, 113, thicknessOfBorder), borderColor);

            // Draw left line
            SpriteBatch.Draw(pixel, new Rectangle(110, 72, thicknessOfBorder,30), borderColor);

            // Draw right line
            SpriteBatch.Draw(pixel, new Rectangle((110 + 113 - thicknessOfBorder),
                                            72,
                                            thicknessOfBorder,
                                            30), borderColor);
            // Draw bottom line//
            SpriteBatch.Draw(pixel, new Rectangle(110,
                                            72 +30- thicknessOfBorder,
                                            113,
                                            thicknessOfBorder), borderColor);
        }

        private void Learn_Gesture()
        {
            int p;
            int nr_frames = 0;
            
            for (p = 0; p < 32; p++)
            {
                if(_kinectWrapper.IsTrackedA==false)
                {
                    _WarningMessage = "There is no skeleton tracked!";
                    break;
                }
                int l = 0;
                while (l < 18)
                {
                    _borderColor = Color.Red;
                    kinect_skeletons = _kinectWrapper.getSkeletons();
                    l = 0;
                    foreach (Joint jnt in kinect_skeletons.Joints)
                    {
                        if (jnt.Position.X != 0 && jnt.Position.Y != 0 && jnt.Position.Z != 0)
                        {
                            l++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                poses = new Poses(Game);

                if (l >= 18)
                {
                    _borderColor = Color.Yellow;

                    var center = new Vector3D((kinect_skeletons.Joints[JointType.ShoulderLeft].Position.X + kinect_skeletons.Joints[JointType.ShoulderRight].Position.X) / 2, (kinect_skeletons.Joints[JointType.ShoulderLeft].Position.Y + kinect_skeletons.Joints[JointType.ShoulderRight].Position.Y) / 2, (kinect_skeletons.Joints[JointType.ShoulderLeft].Position.Z + kinect_skeletons.Joints[JointType.ShoulderRight].Position.Z) / 2);
                    double shoulderDist =
                        Math.Sqrt(Math.Pow((kinect_skeletons.Joints[JointType.ShoulderLeft].Position.X - kinect_skeletons.Joints[JointType.ShoulderRight].Position.X), 2) +
                                  Math.Pow((kinect_skeletons.Joints[JointType.ShoulderLeft].Position.Y - kinect_skeletons.Joints[JointType.ShoulderRight].Position.Y), 2) +
                                  Math.Pow((kinect_skeletons.Joints[JointType.ShoulderLeft].Position.Z - kinect_skeletons.Joints[JointType.ShoulderRight].Position.Z), 2));
                    #region calcul_segmente 
                    for (int i = 0; i < 10; i++)
                    {

                        poses.dictionar_pozitii[i][0] = (kinect_skeletons.Joints[segments[0, i]].Position.X - center.X) / shoulderDist - (kinect_skeletons.Joints[segments[1, i]].Position.X - center.X) / shoulderDist;
                        poses.dictionar_pozitii[i][1] = (kinect_skeletons.Joints[segments[0, i]].Position.Y - center.Y) / shoulderDist - (kinect_skeletons.Joints[segments[1, i]].Position.Y - center.Y) / shoulderDist;
                        poses.dictionar_pozitii[i][2] = (kinect_skeletons.Joints[segments[0, i]].Position.Z - center.Z) / shoulderDist - (kinect_skeletons.Joints[segments[1, i]].Position.Z - center.Z) / shoulderDist;
                    }
                    #endregion


                }
                _WarningMessage = "Frame number : " + p.ToString();
                gesture.Add(poses);
                Thread.Sleep(200);
            }

        eticheta1:
            if (_kinectWrapper.get_command() == "nocommand")
            {
                _InformationMessage = "Say DELETE if you don't want to keep the recorded gesture,\nsay SAVE if you want to save gesture!";
                goto eticheta1;
            }
            else if (_kinectWrapper.get_command() == "D")
            {
                timer2.Start();
                goto eticheta2;
            }
            else if (_kinectWrapper.get_command() == "S")
            {
                
                save_gesture(gesture);
                goto eticheta2;
            }
        eticheta2:
            _kinectWrapper.set_command("nocommand");
            _InformationMessage = String.Empty;


        }

        private void save_gesture(List<Poses> gesture)
        {
            if (System.IO.Directory.GetFiles("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent", "gestures.xls", System.IO.SearchOption.AllDirectories).Any())
            {
                Excel.Application xlApp;
                xlApp = new Microsoft.Office.Interop.Excel.Application();
                Excel.Workbook xlWorkBook;
                Excel.Worksheet xlWorkSheet;
                object misValue = System.Reflection.Missing.Value;
                xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\gestures.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                xlApp.DisplayAlerts = false;

                for (int i = xlWorkBook.Worksheets.Count; i > 0; i--)
                {
                    xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets[i];

                    if (xlWorkSheet.Name == "Temporary")
                    {
                        xlWorkSheet.Delete();
                    }
                    releaseObject(xlWorkSheet);
                }

                xlWorkBook.SaveAs("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\gestures.xls", Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);

                xlWorkBook.Close(true, Type.Missing, Type.Missing);
                xlApp.DisplayAlerts = true;
                xlApp.Quit();

                releaseObject(xlWorkBook);
                releaseObject(xlApp);
            }

            #region save_Gesture
            if (!System.IO.Directory.GetFiles("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent", "gestures.xls", System.IO.SearchOption.AllDirectories).Any())
            {
                Excel.Application xlApp;
                xlApp = new Microsoft.Office.Interop.Excel.Application();
                Excel.Workbook xlWorkBook;
                Excel.Worksheet xlWorkSheet;
                object misValue = System.Reflection.Missing.Value;
                xlWorkBook = xlApp.Workbooks.Add(misValue);
                xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
                xlWorkSheet.Name = "Temporary";

                
                int offset = 0;
                for (int k = 0; k < gesture.Count; k++)
                {
                    for (int i = 1; i < 11; i++)
                    {
                        offset += 1;
                        xlWorkSheet.Cells[offset, 1] = i;
                        for (int j = 2; j <= 5; j++)
                        {
                            switch (j)
                            {
                                case 2:
                                    xlWorkSheet.Cells[offset, j] = gesture[k].dictionar_pozitii[i - 1][0];
                                    break;
                                case 3:
                                    xlWorkSheet.Cells[offset, j] = gesture[k].dictionar_pozitii[i - 1][1];
                                    break;
                                case 4:
                                    xlWorkSheet.Cells[offset, j] = gesture[k].dictionar_pozitii[i - 1][2];
                                    break;
                            }
                        }

                    }
                }
                
                xlWorkSheet.Cells[offset+1,1] = "END";
                xlWorkBook.SaveAs("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\gestures.xls", Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
                xlWorkBook.Close(true, misValue, misValue);
                xlApp.Quit();

                releaseObject(xlWorkSheet);
                releaseObject(xlWorkBook);
                releaseObject(xlApp);
            }
            else
            {
                Excel.Application xlApp;
                Excel.Workbook xlWorkBook;
                Excel.Worksheet xlWorkSheet;
                xlApp = new Excel.Application();
                xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\gestures.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                //    xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
                // Excel.Sheets worksheets = xlWorkBook.Worksheets;

                var xlSheets = xlWorkBook.Sheets as Excel.Sheets;

                //  var xlNewSheet = (Excel.Worksheet)worksheets.Add(worksheets[1], Type.Missing, Type.Missing, Type.Missing);

                var xlNewSheet = (Excel.Worksheet)xlSheets.Add(xlSheets[1], Type.Missing, Type.Missing, Type.Missing);
                xlNewSheet.Name = "Temporary";
                xlApp.DisplayAlerts = false;
                
                #region --add new pose
                int offset = 0;
                for (int k = 0; k < gesture.Count; k++)
                {
                    for (int i = 1; i < 11; i++)
                    {
                        offset += 1;
                        xlNewSheet.Cells[offset, 1] = i;
                        for (int j = 2; j <= 5; j++)
                        {
                            switch (j)
                            {
                                case 2:
                                    xlNewSheet.Cells[offset, j] = gesture[k].dictionar_pozitii[i - 1][0];
                                    break;
                                case 3:
                                    xlNewSheet.Cells[offset, j] = gesture[k].dictionar_pozitii[i - 1][1];
                                    break;
                                case 4:
                                    xlNewSheet.Cells[offset, j] = gesture[k].dictionar_pozitii[i - 1][2];
                                    break;
                            }
                        }

                    }
                }


                #endregion
                xlNewSheet.Cells[offset+1, 1] = "END";
                // xlNewSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
                //xlNewSheet.Select();

                xlWorkBook.Save();
                xlWorkBook.Close(Type.Missing, Type.Missing, Type.Missing);
                xlApp.DisplayAlerts = true;
                xlApp.Quit();

                releaseObject(xlNewSheet);
                //    releaseObject(worksheets);
                releaseObject(xlWorkBook);
                releaseObject(xlApp);
            }
            #endregion

        }

        private void Start_Learning_Pose()
        {
            int l = 0;
            while (l < 18)
            {
                _borderColor = Color.Red;
                if(_kinectWrapper.IsTrackedA==false)
                {
                    _WarningMessage = "There is no tracked skeleton for taking this pose,please try again!";
                    break;
                }
                kinect_skeletons = _kinectWrapper.getSkeletons();
                l = 0;
                foreach (Joint jnt in kinect_skeletons.Joints)
                {
                    if (jnt.Position.X != 0 && jnt.Position.Y != 0 && jnt.Position.Z != 0)
                    {
                        l++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (l >= 18)
            {
                _borderColor = Color.Yellow;
                var center = new Vector3D((kinect_skeletons.Joints[JointType.ShoulderLeft].Position.X + kinect_skeletons.Joints[JointType.ShoulderRight].Position.X)/2,(kinect_skeletons.Joints[JointType.ShoulderLeft].Position.Y + kinect_skeletons.Joints[JointType.ShoulderRight].Position.Y) / 2,(kinect_skeletons.Joints[JointType.ShoulderLeft].Position.Z+ kinect_skeletons.Joints[JointType.ShoulderRight].Position.Z)/2);
                double shoulderDist =
                    Math.Sqrt(Math.Pow((kinect_skeletons.Joints[JointType.ShoulderLeft].Position.X - kinect_skeletons.Joints[JointType.ShoulderRight].Position.X), 2) +
                              Math.Pow((kinect_skeletons.Joints[JointType.ShoulderLeft].Position.Y - kinect_skeletons.Joints[JointType.ShoulderRight].Position.Y), 2) +
                              Math.Pow((kinect_skeletons.Joints[JointType.ShoulderLeft].Position.Z - kinect_skeletons.Joints[JointType.ShoulderRight].Position.Z), 2));
                #region calcul_segmente 
                for (int i = 0; i < 10; i++)
                {
                    
                    poses.dictionar_pozitii[i][0] = (kinect_skeletons.Joints[segments[0, i]].Position.X - center.X)/shoulderDist - (kinect_skeletons.Joints[segments[1, i]].Position.X-center.X)/shoulderDist;
                    poses.dictionar_pozitii[i][1] = (kinect_skeletons.Joints[segments[0, i]].Position.Y -center.Y)/shoulderDist - (kinect_skeletons.Joints[segments[1, i]].Position.Y-center.Y)/shoulderDist;
                    poses.dictionar_pozitii[i][2] = (kinect_skeletons.Joints[segments[0, i]].Position.Z-center.Z)/shoulderDist - (kinect_skeletons.Joints[segments[1, i]].Position.Z-center.Z)/shoulderDist;
                }
                #endregion


                if (System.IO.Directory.GetFiles("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent", "poses.xls", System.IO.SearchOption.AllDirectories).Any())
                {
                    Excel.Application xlApp;
                    xlApp = new Microsoft.Office.Interop.Excel.Application();
                    Excel.Workbook xlWorkBook;
                    Excel.Worksheet xlWorkSheet;
                    object misValue = System.Reflection.Missing.Value;


                    xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\poses.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);

                    xlApp.DisplayAlerts = false;

                    for (int i = xlWorkBook.Worksheets.Count; i > 0; i--)
                    {
                        xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets[i];

                        if (xlWorkSheet.Name == "Temporary")
                        {
                            xlWorkSheet.Delete();
                        }
                        releaseObject(xlWorkSheet);
                    }



                    xlWorkBook.SaveAs("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\poses.xls", Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);

                    xlWorkBook.Close(true, Type.Missing, Type.Missing);
                    xlApp.DisplayAlerts = true;
                    xlApp.Quit();

                    releaseObject(xlWorkBook);
                    releaseObject(xlApp);

                }



                #region save_posture

                if (!System.IO.Directory.GetFiles("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent", "poses.xls", System.IO.SearchOption.AllDirectories).Any())
                {

                    Excel.Application xlApp;
                    xlApp = new Microsoft.Office.Interop.Excel.Application();
                    Excel.Workbook xlWorkBook;
                    Excel.Worksheet xlWorkSheet;
                    object misValue = System.Reflection.Missing.Value;
                    xlWorkBook = xlApp.Workbooks.Add(misValue);
                    xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

                    xlWorkSheet.Name = "Temporary";
                    int i = 0;
                    for (i = 1; i < 11; i++)
                    {
                        xlWorkSheet.Cells[i, 1] = i;
                        for (int j = 2; j <= 5; j++)
                        {
                            switch (j)
                            {
                                case 2:
                                    xlWorkSheet.Cells[i, j] = poses.dictionar_pozitii[i-1][0];
                                    break;
                                case 3:
                                    xlWorkSheet.Cells[i, j] = poses.dictionar_pozitii[i-1][1];
                                    break;
                                case 4:
                                    xlWorkSheet.Cells[i, j] = poses.dictionar_pozitii[i-1][2];
                                    break;
                            }
                        }

                    }

                    xlWorkBook.SaveAs("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\poses.xls", Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
                    xlWorkBook.Close(true, misValue, misValue);
                    xlApp.Quit();

                    releaseObject(xlWorkSheet);
                    releaseObject(xlWorkBook);
                    releaseObject(xlApp);
                }
                else
                {
                    Excel.Application xlApp;
                    Excel.Workbook xlWorkBook;
                    Excel.Worksheet xlWorkSheet;
                    xlApp = new Excel.Application();
                    xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\poses.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                    //    xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
                    // Excel.Sheets worksheets = xlWorkBook.Worksheets;

                    var xlSheets = xlWorkBook.Sheets as Excel.Sheets;



                    //  var xlNewSheet = (Excel.Worksheet)worksheets.Add(worksheets[1], Type.Missing, Type.Missing, Type.Missing);

                    var xlNewSheet = (Excel.Worksheet)xlSheets.Add(xlSheets[1], Type.Missing, Type.Missing, Type.Missing);
                    xlNewSheet.Name = "Temporary";
                    xlApp.DisplayAlerts = false;
                    int i = 0;
                    #region --add new pose

                    for (i = 1; i < 11; i++)
                    {
                        
                        xlNewSheet.Cells[i, 1] = i;
                        for (int j = 2; j <= 5; j++)
                        {
                            switch (j)
                            {
                                case 2:
                                    xlNewSheet.Cells[i, j] = poses.dictionar_pozitii[i-1][0];
                                    break;
                                case 3:
                                    xlNewSheet.Cells[i, j] = poses.dictionar_pozitii[i-1][1];
                                    break;
                                case 4:
                                    xlNewSheet.Cells[i, j] = poses.dictionar_pozitii[i-1][2];
                                    break;
                            }
                        }

                    }


                    #endregion

                    // xlNewSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
                    //xlNewSheet.Select();

                    xlWorkBook.Save();
                    xlWorkBook.Close(Type.Missing, Type.Missing, Type.Missing);
                    xlApp.DisplayAlerts = true;
                    xlApp.Quit();

                    releaseObject(xlNewSheet);
                    //    releaseObject(worksheets);
                    releaseObject(xlWorkBook);
                    releaseObject(xlApp);



                }


            }



                #endregion
                

            }
        
         public void Save_Pose(String nume_pozitie)
    {
            #region rename_from_temporary_to_given_name_For_pose
            int xrel;
        if (!System.IO.Directory.GetFiles("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent", "poses.xls", System.IO.SearchOption.AllDirectories).Any())
        {
            _WarningMessage = "There is no file with a pose written to be learned!";
        }
        else
        {
            Excel.Application xlApp;
            xlApp = new Microsoft.Office.Interop.Excel.Application();
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            object misValue = System.Reflection.Missing.Value;

            xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\poses.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);

            foreach (Excel.Worksheet xlWork in xlWorkBook.Worksheets)
            {
                if (xlWork.Name == textBox1.Text)
                {
                    _WarningMessage = "The pose you are trying to learn is already learned!";

                    xlApp.Quit();
                    releaseObject(xlWorkBook);
                    releaseObject(xlApp);
                    return;
                }
            }
            xlApp.DisplayAlerts = false;
            for (int i = xlWorkBook.Worksheets.Count; i > 0; i--)
            {
                xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets[i];

                if (xlWorkSheet.Name == "Temporary")
                {
                    xlWorkSheet.Name = textBox1.Text;
                }
                releaseObject(xlWorkSheet);
            }
            xlWorkBook.SaveAs("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\poses.xls", Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
            xlWorkBook.Close(true, Type.Missing, Type.Missing);
            xlApp.DisplayAlerts = true;
            xlApp.Quit();

            releaseObject(xlWorkBook);
                releaseObject(xlApp);
                _recognition.UnloadPoses();
                _recognition.LoadLearnedPoses();
        }
            #endregion

            #region rename_from_temporary_to_given_name_For_gesture
            if (!System.IO.Directory.GetFiles("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent", "gestures.xls", System.IO.SearchOption.AllDirectories).Any())
            {
                _WarningMessage = "There is no file with a gesture written to be learned!";
            }
            else
            {
                Excel.Application xlApp;
                xlApp = new Microsoft.Office.Interop.Excel.Application();
                Excel.Workbook xlWorkBook;
                Excel.Worksheet xlWorkSheet;
                object misValue = System.Reflection.Missing.Value;

                xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\gestures.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);

                foreach (Excel.Worksheet xlWork in xlWorkBook.Worksheets)
                {
                    if (xlWork.Name == textBox1.Text)
                    {
                        _WarningMessage = "The gesture you are trying to learn is already learned!";

                        xlApp.Quit();
                        releaseObject(xlWorkBook);
                        releaseObject(xlApp);
                        return;
                    }
                }
                xlApp.DisplayAlerts = false;
                for (int i = xlWorkBook.Worksheets.Count; i > 0; i--)
                {
                    xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets[i];

                    if (xlWorkSheet.Name == "Temporary")
                    {
                        xlWorkSheet.Name = textBox1.Text;
                    }
                    releaseObject(xlWorkSheet);
                }
                xlWorkBook.SaveAs("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\gestures.xls", Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
                xlWorkBook.Close(true, Type.Missing, Type.Missing);
                xlApp.DisplayAlerts = true;
                xlApp.Quit();

                releaseObject(xlWorkBook);
                releaseObject(xlApp);
                _dtw.Load_Gestures();
            }
            #endregion




        }

        private void releaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch (Exception ex)
            {
                obj = null;
               //eroare
            }
            finally
            {
                GC.Collect();
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (_inputService.IsPressed(MouseButtons.Right, false))
            {

                _WarningMessage = String.Empty;
                _LearningWindow.IsVisible = true;
            }
         
           if(_inputService.IsPressed(Keys.Enter,false))
            {
                _WarningMessage = String.Empty;
            }
            
           base.Update(gameTime);
        }

    }
}
