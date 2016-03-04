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
using DigitalRune.Game.Input;
using DigitalRune.Physics;
using KinectGame.GeneralComponents;
using KinectGame.GeneralComponents.RigidBodyRendered;
using KinectGame.AppCore;
using KinectGame.AppCore.MappingSkeleton;
using System.Timers;
using System.Text;


using DigitalRune.Animation;
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
using System.Collections;

namespace KinectGame.AppCore
{
    class Recognition:DrawableGameComponent
    {
        private readonly KinectWrapper _kinectWrapper;
        protected SpriteBatch SpriteBatch { get; private set; }
        protected SpriteFont SpriteFont { get; private set; }
        private readonly States st;
        private readonly IInputService _inputService;
        private readonly IUIService _uiService;
        private Poses[] poses;
        private Gestures[] gestures;
        private Poses pose_to_be_analysed;
        private Timer timer;
        private double pose_grade;
        private Color color;
        private JointType[,] segments;

        private ArrayList _video;
        private const int BufferSize = 32;
        private const int MinimumFrames = 6;
        private DynamicTimeWarping dtw;


        private StringBuilder recognised_pose;
        private List<double> valori_de_min;


        private KinectSkeleton[] _kinectSkeleton;

        public Dictionary<String, double> reference_angles = new Dictionary<String, double>();
  


        public Recognition(Game game) : base(game)
        {
            _kinectWrapper = Game.Components.OfType<KinectWrapper>().First();
            st = Game.Components.OfType<States>().First();
            dtw = Game.Components.OfType<DynamicTimeWarping>().First();

            _inputService = (IInputService)game.Services.GetService(typeof(IInputService));
            _uiService = (IUIService)game.Services.GetService(typeof(IUIService));


            pose_to_be_analysed = new Poses(Game);
            //_video = new ArrayList();
            

            _kinectSkeleton = new KinectSkeleton[1];
            segments = new JointType[2,10];
            valori_de_min = new List<double>();


            timer = new Timer(600);

            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            recognised_pose = new StringBuilder();
            recognised_pose.Append(" ");

            segments[0, 0] = JointType.WristRight; segments[0, 5] = JointType.HipRight; segments[0, 8] = JointType.ShoulderCenter;
            segments[1, 0] = JointType.ElbowRight; segments[1, 5] = JointType.KneeRight; segments[1, 8] = JointType.ShoulderLeft;
            segments[0, 1] = JointType.WristLeft; segments[0, 6] = JointType.KneeLeft; segments[0, 9] = JointType.ShoulderCenter;
            segments[1, 1] = JointType.ElbowLeft; segments[1, 6] = JointType.AnkleLeft; segments[1, 9] = JointType.ShoulderRight;
            segments[0, 2] = JointType.ElbowRight; segments[0, 7] = JointType.KneeRight;
            segments[1, 2] = JointType.ShoulderRight; segments[1, 7] = JointType.AnkleRight;
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

        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            SpriteFont = Game.Content.Load<SpriteFont>("SpriteFont1");
            LoadLearnedPoses();
           // LoadLearnedGestures();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // _kinectSkeleton[0] = _kinectWrapper.getSkeletons();
            // check_for_pose();
            check_for_pose_exp();
          
           // timer.Start();
        }

        public override void Draw(GameTime gameTime)
        {
            var position = new Vector2(150, 350);
            var position2 = new Vector2(150, 150);
         
            SpriteBatch.Begin();
            
            Rectangle titleSafeRectangle = GraphicsDevice.Viewport.TitleSafeArea;

            SpriteBatch.DrawString(SpriteFont,pose_grade.ToString(), position2, Color.Red, 0, new Vector2(10, 10), 3, SpriteEffects.None, 1);
            SpriteBatch.DrawString(SpriteFont, recognised_pose, position, Color.Red, 0, new Vector2(10, 10), 3, SpriteEffects.None, 1);
         
            SpriteBatch.End();

        //    check_for_Gesture();
       
           // check_for_pose();

            base.Draw(gameTime);
        }

        private void check_for_Gesture()
        {
            int l = 0;
            while (l < 18)
            {
                _kinectSkeleton[0] = _kinectWrapper.getSkeletons();
                l = 0;
                foreach (Joint jnt in _kinectSkeleton[0].Joints)
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
            pose_to_be_analysed = new Poses(Game);

            if (l >= 18)
            {
                double x, y, z;
                int a;
                var center = new Vector3D((_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.X + _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.X) / 2, (_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.Y + _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.Y) / 2, (_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.Z + _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.Z) / 2);
                double shoulderDist =
                    Math.Sqrt(Math.Pow((_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.X - _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.X), 2) +
                              Math.Pow((_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.Y - _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.Y), 2) +
                              Math.Pow((_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.Z - _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.Z), 2));

                #region calcul_segmente 
                for (int i = 0; i < 9; i++)
                {

                    pose_to_be_analysed.dictionar_pozitii[i][0] = (_kinectSkeleton[0].Joints[segments[0, i]].Position.X - center.X) / shoulderDist - (_kinectSkeleton[0].Joints[segments[1, i]].Position.X - center.X) / shoulderDist;
                    pose_to_be_analysed.dictionar_pozitii[i][1] = (_kinectSkeleton[0].Joints[segments[0, i]].Position.Y - center.Y) / shoulderDist - (_kinectSkeleton[0].Joints[segments[1, i]].Position.Y - center.Y) / shoulderDist;
                    pose_to_be_analysed.dictionar_pozitii[i][2] = (_kinectSkeleton[0].Joints[segments[0, i]].Position.Z - center.Z) / shoulderDist - (_kinectSkeleton[0].Joints[segments[1, i]].Position.Z - center.Z) / shoulderDist;
                }
                #endregion
                //sent the pose to dtw to be added to array

            }
        }

        private void check_for_pose()
        {
            recognised_pose.Clear();

            timer.Stop();
          
            _kinectSkeleton[0] = _kinectWrapper.getSkeletons();

            if (_kinectSkeleton[0] != null && _kinectSkeleton[0].TrackingState == SkeletonTrackingState.Tracked)
            {

                for (int i = 0; i < 14; i++)
                {
                    pose_to_be_analysed.dictionar_pozitii[i][0] = _kinectSkeleton[0].Joints[segments[0, i]].Position.X - _kinectSkeleton[0].Joints[segments[1, i]].Position.X;
                    pose_to_be_analysed.dictionar_pozitii[i][1] = _kinectSkeleton[0].Joints[segments[0, i]].Position.Y - _kinectSkeleton[0].Joints[segments[1, i]].Position.Y;
                    pose_to_be_analysed.dictionar_pozitii[i][2] = _kinectSkeleton[0].Joints[segments[0, i]].Position.Z - _kinectSkeleton[0].Joints[segments[1, i]].Position.Z;
                }
                pose_to_be_analysed.y_ax_angle();
                pose_to_be_analysed.az_proj_angle();
                pose_to_be_analysed.sum_angle();



                for (int i = 0; i < poses.Count<Poses>(); i++)
                {
                    poses[i].set_min_value(Math.Abs(poses[i].get_sum_angle() - pose_to_be_analysed.get_sum_angle()));
                    valori_de_min.Add(poses[i].get_min_value());
                    if (i == poses.Count<Poses>() - 1)
                    {
                        valori_de_min.Add(valori_de_min.Min());
                    }
                }

                for (int i = 0; i < poses.Count<Poses>(); i++)
                {
                    if (poses[i].get_min_value() == valori_de_min.Last())
                    {
                        recognised_pose.Append(poses[i].nume_pozitie);
                        break;
                    }
                }
                valori_de_min.Clear();
                



            }
            timer.Start();
        }

        private void check_for_pose_exp()
        {
            timer.Stop();

            recognised_pose.Clear();
            _kinectSkeleton[0] = _kinectWrapper.getSkeletons();

            if (_kinectSkeleton[0] != null && _kinectSkeleton[0].TrackingState == SkeletonTrackingState.Tracked)
            {
                var center = new Vector3D((_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.X + _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.X) / 2, (_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.Y + _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.Y) / 2, (_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.Z + _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.Z) / 2);
                double shoulderDist =
                    Math.Sqrt(Math.Pow((_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.X - _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.X), 2) +
                              Math.Pow((_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.Y - _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.Y), 2) +
                              Math.Pow((_kinectSkeleton[0].Joints[JointType.ShoulderLeft].Position.Z - _kinectSkeleton[0].Joints[JointType.ShoulderRight].Position.Z), 2));

                for (int i = 0; i < 10; i++)
                {
                    pose_to_be_analysed.dictionar_pozitii[i][0] = ((_kinectSkeleton[0].Joints[segments[0, i]].Position.X - center.X) / shoulderDist) - ((_kinectSkeleton[0].Joints[segments[1, i]].Position.X - center.X) / shoulderDist);
                    pose_to_be_analysed.dictionar_pozitii[i][1] = ((_kinectSkeleton[0].Joints[segments[0, i]].Position.Y - center.Y) / shoulderDist) - ((_kinectSkeleton[0].Joints[segments[1, i]].Position.Y - center.Y) / shoulderDist);
                    pose_to_be_analysed.dictionar_pozitii[i][2] = ((_kinectSkeleton[0].Joints[segments[0, i]].Position.Z - center.Z) / shoulderDist) - ((_kinectSkeleton[0].Joints[segments[1, i]].Position.Z - center.Z) / shoulderDist);
                }
                
                //     pose_to_be_analysed.apply_correction(_kinectSkeleton[0]);
               // pose_grade = pose_to_be_analysed.apply_correction(_kinectSkeleton[0]);

                for(int i=0;i<poses.Count<Poses>();i++)
                {
                    poses[i].set_min_value(pose_to_be_analysed);
                    valori_de_min.Add(poses[i].get_min_value());
                    if (i == poses.Count<Poses>() - 1)
                    {
                        valori_de_min.Add(valori_de_min.Min());
                    }
                }


                for (int i = 0; i < poses.Count<Poses>(); i++)
                {
                    if (poses[i].get_min_value() == valori_de_min.Last())
                    {
                        recognised_pose.Append(poses[i].nume_pozitie);
                        pose_grade = poses[i].get_min_value();
                        break;
                    }
                }
                valori_de_min.Clear();
            }
            timer.Start();
            
            }

        public void LoadLearnedPoses()
        {
            Microsoft.Office.Interop.Excel.Application xlApp;
            xlApp = new Microsoft.Office.Interop.Excel.Application();
            int nr_pozitii;
            Excel.Workbook xlWorkBook;
            if (xlApp == null)
            {
                //afisare eroare pe ecran
                return;
            }
            if (!System.IO.Directory.GetFiles("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent", "poses.xls", System.IO.SearchOption.AllDirectories).Any())
            {
                return;
            }
            else
            {
                xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\poses.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                nr_pozitii = xlWorkBook.Worksheets.Count;
                poses = new Poses[nr_pozitii];
                for(int i = 0; i < nr_pozitii; i++)
                {
                    poses[i] = new Poses(Game);
                }

                int j = 0;
                 foreach (Excel.Worksheet xlWork in xlWorkBook.Worksheets)
                { 
                    poses[j].nume_pozitie = xlWork.Name.ToString();
                    for(int i=1;i<11;i++)
                    {
                        for(int c=2;c<5;c++)
                        {    
                            poses[j].dictionar_pozitii[i-1][c - 2] = (float)Convert.ToDouble(xlWork.Cells[i, c].Value);   
                        }

                    }
                    j += 1;
                }

                 for(int i=0;i<nr_pozitii;i++)
                {
                    poses[i].y_ax_angle();
                    poses[i].az_proj_angle();
                    poses[i].sum_angle();
                }

                xlApp.Quit();
                releaseObject(xlApp);
                releaseObject(xlWorkBook);    
            }
            
        }

        private void LoadLearnedGestures()
        {
            Microsoft.Office.Interop.Excel.Application xlApp;
            xlApp = new Microsoft.Office.Interop.Excel.Application();
            int nr_gesturi;
            Excel.Workbook xlWorkBook;
            if (xlApp == null)
            {
                //afisare eroare pe ecran
                return;
            }
            if (!System.IO.Directory.GetFiles("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent", "gestures.xls", System.IO.SearchOption.AllDirectories).Any())
            {
                return;
            }
            else
            {
                xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\gestures.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                nr_gesturi = xlWorkBook.Worksheets.Count;
                gestures = new Gestures[nr_gesturi];
                for (int i = 0; i < nr_gesturi; i++)
                {
                    gestures[i] = new Gestures();
                }

                int j = 0;
                foreach (Excel.Worksheet xlWork in xlWorkBook.Worksheets)
                {
                    gestures[j].nume_gest = xlWork.Name.ToString();

                    int number_of_rows = xlWork.Rows.Count;
                    int number_of_poses = number_of_rows / 14;
                    poses = new Poses[number_of_poses];

                    for (int i = 0; i < number_of_poses; i++)
                    {
                        poses[i] = new Poses(Game);
                    }

                    int offset = 0;
                    for (int k = 0; k < number_of_poses; k++)
                    {
                        for (int i = 1; i < 10; i++)
                        {
                            offset += 1;
                            for (int c = 2; c < 5; c++)
                            {
                                poses[k].dictionar_pozitii[i - 1][c - 2] = (float)Convert.ToDouble(xlWork.Cells[offset, c].Value);
                            }

                        }
                        gestures[j].add_pose_to_gesture(poses[k]);
                     }
                    j += 1;
                    offset = 0;


                }


            }



            }

        private bool TestRange(double numberToCheck, double bottom, double top)
        {
            return (numberToCheck >= bottom && numberToCheck <= top);
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

        public void UnloadPoses()
        {
            for(int i=0;i<poses.Length;i++)
            {
                poses[i].Dispose();
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (_inputService.IsPressed(Keys.R, false))
            {
                timer.Start(); ///TODO SCOATE R
            }
            if (_inputService.IsPressed(Keys.D, false))
            {
                dtw.StartRecognition(); ///TODO SCOATE D
            }
            if (_inputService.IsPressed(Keys.T, false))
            {
                timer.Stop();
            }
            base.Update(gameTime);
        }

    }
}
