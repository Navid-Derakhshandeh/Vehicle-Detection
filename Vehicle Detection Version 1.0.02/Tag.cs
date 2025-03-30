using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Face;
using Emgu.CV.CvEnum;
using System.IO;
using System.Threading;
using System.Diagnostics;
using AForge.Video;
using AForge.Vision;
using AForge.Imaging;
using System.Text.RegularExpressions;

namespace Vehicle_Detection
{
    public partial class Tag : Form
    {
        private VideoCapture OnlineCamCapture = null;
        private Image<Bgr, Byte> OnlineFrame = null;
        Mat Imageframe = new Mat();
        private bool VehicleDetectionEnabled = false;
        CascadeClassifier VehicleCascadeClassifier = new CascadeClassifier("cars.xml");
        bool EnableSave = false;
        static List<Image<Gray, Byte>> TrainedVehicle = new List<Image<Gray, byte>>();
        static List<int> VehicleTag = new List<int>();
        static EigenFaceRecognizer Detection;
        static List<string> VehiclesName = new List<string>();
        private static bool Trained = false;

        public Tag()
        {
            InitializeComponent();
        }

        private void btnCap_Click(object sender, EventArgs e)
        {
            OnlineCamCapture = new VideoCapture("http://IP-Address/mjpg/video.mjpg");
            OnlineCamCapture.ImageGrabbed += Frame3;
            OnlineCamCapture.Start();
        }
        private void Frame3(object sender, EventArgs e)
        {
            OnlineCamCapture.Retrieve(Imageframe, 0);
            OnlineFrame = Imageframe.ToImage<Bgr, Byte>().Resize(OnlineCapture.Width, OnlineCapture.Height, Inter.Cubic);

            if (VehicleDetectionEnabled)
            {
                Mat GrayImage = new Mat();
                CvInvoke.CvtColor(OnlineFrame, GrayImage, ColorConversion.Bgr2Gray);
                CvInvoke.EqualizeHist(GrayImage, GrayImage);

                Rectangle[] Vehicles = VehicleCascadeClassifier.DetectMultiScale(GrayImage, 1.1, 3, Size.Empty, Size.Empty);
                if (Vehicles.Length > 0)
                {
                    foreach (var vehicle in Vehicles)
                    {
                        CvInvoke.Rectangle(OnlineFrame, vehicle, new Bgr(Color.Red).MCvScalar, 2);
                        Image<Bgr, Byte> Resimage = OnlineFrame.Convert<Bgr, Byte>();
                        Resimage.ROI = vehicle;
                        VehicleImage.SizeMode = PictureBoxSizeMode.StretchImage;
                        VehicleImage.Image = Resimage.Bitmap;
                        if (EnableSave)
                        {
                            string Path = Directory.GetCurrentDirectory() + @"\Train";
                            if (!Directory.Exists(Path))
                                Directory.CreateDirectory(Path);

                            Task.Factory.StartNew(() => {
                                for (int i = 0; i < 10; i++)
                                { 
                                    Resimage.Resize(200, 200, Inter.Cubic).Save(Path + @"\" + txtName.Text + "_" + DateTime.Now.ToString("dd-mm-yyyy-hh-mm-ss") + ".jpg");
                                    Thread.Sleep(1000);
                                }
                            });
                        }
                        EnableSave = false;
                        if (btnAdd.InvokeRequired)
                        {
                            btnAdd.Invoke(new ThreadStart(delegate {
                                btnAdd.Enabled = true;
                            }));
                        }
                        if (Trained)
                        {
                            Image<Gray, Byte> GrayVehicleResult = Resimage.Convert<Gray, Byte>().Resize(200, 200, Inter.Cubic);
                            CvInvoke.EqualizeHist(GrayVehicleResult, GrayVehicleResult);
                            var Res = Detection.Predict(GrayVehicleResult);
                            Debug.WriteLine(Res.Label + ". " + Res.Distance);
                            if (Res.Label != -1 && Res.Distance < 2000)
                            {
                                CvInvoke.PutText(OnlineFrame, VehiclesName[Res.Label], new Point(vehicle.X - 2, vehicle.Y - 2),
                                    FontFace.HersheyComplex, 1.0, new Bgr(Color.Green).MCvScalar);
                                CvInvoke.Rectangle(OnlineFrame, vehicle, new Bgr(Color.Red).MCvScalar, 2);
                            }
                            else
                            {
                                CvInvoke.PutText(OnlineFrame, "UnClassified", new Point(vehicle.X - 2, vehicle.Y - 2),
                                    FontFace.HersheyComplex, 1.0, new Bgr(Color.Green).MCvScalar);
                                CvInvoke.Rectangle(OnlineFrame, vehicle, new Bgr(Color.Blue).MCvScalar, 2);
                            }
                        }
                    }
                }

            }
            OnlineCapture.Image = OnlineFrame.Bitmap;
        }

        private void btnDetect_Click(object sender, EventArgs e)
        {
            VehicleDetectionEnabled = true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            btnAdd.Enabled = true;
            btnAdd.Enabled = false;
            EnableSave = true;
        }

        private void btnTraining_Click(object sender, EventArgs e)
        {
            TrainImagesFrompath();
        }
        private static bool TrainImagesFrompath()
        {
            int VehicleCount = 0;
            double Sill = 2000;
            TrainedVehicle.Clear();
            VehicleTag.Clear();
            VehiclesName.Clear();
            try
            {
                string Path = Directory.GetCurrentDirectory() + @"\Train";
                string[] Files = Directory.GetFiles(Path, "*.jpg", SearchOption.AllDirectories);

                foreach (var File in Files)
                {
                    Image<Gray, byte> TrainedV = new Image<Gray, byte>(File).Resize(200, 200, Inter.Cubic);
                    CvInvoke.EqualizeHist(TrainedV, TrainedV);
                    TrainedVehicle.Add(TrainedV);
                    VehicleTag.Add(VehicleCount);
                    string name = File.Split('\\').Last().Split('_')[0];
                    VehiclesName.Add(name);
                    VehicleCount++;
                    Debug.WriteLine(VehicleCount + ". " + name);
                }

                if (TrainedVehicle.Count() > 0)
                {
                    Detection = new EigenFaceRecognizer(VehicleCount, Sill);
                    Detection.Train(TrainedVehicle.ToArray(), VehicleTag.ToArray());
                    Trained = true;
                    return true;
                }
                else
                {
                    Trained = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trained = false;
                MessageBox.Show("Some Problem Happened: " + ex.Message);
                return false;
            }

        }

        private void btnDetection_Click(object sender, EventArgs e)
        {
            VehicleDetectionEnabled = true;
        }
    }
}
