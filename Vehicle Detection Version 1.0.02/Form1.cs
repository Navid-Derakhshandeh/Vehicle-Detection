using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Vision.Motion;
using AForge.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Vehicle_Detection
{
    public partial class Form1 : Form
    {
        MJPEGStream Video1;
        MJPEGStream Video2;
        MJPEGStream Video3;
        MotionDetector motion;

        public Form1()
        {
            InitializeComponent();
            Video1 = new MJPEGStream(""); //http://IP-Address/mjpg/video.mjpg
            Video1.NewFrame += Frame1;
            Video2 = new MJPEGStream("");  //http://IP-Address/mjpg/video.mjpg
            Video3 = new MJPEGStream("");
            
            motion = new MotionDetector(new TwoFramesDifferenceDetector(), new GridMotionAreaProcessing());
        }
        Double c;
        private void Frame1(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bmp = (Bitmap)eventArgs.Frame.Clone();
            pictureBox1.Image = bmp;
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            timer1.Start();
            videoSourcePlayer1.VideoSource = Video2;
            videoSourcePlayer1.Start();
            Video1.Start();
            Video3.Start();
            Video3.NewFrame += Frame2;
        }
        static readonly CascadeClassifier cascadeClassifier = new CascadeClassifier("cars.xml");
        private void Frame2(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmp2 = (Bitmap)eventArgs.Frame.Clone();
            Image<Bgr, byte> grayImage = new Image<Bgr, byte>(bitmp2);
            Rectangle[] rectangles = cascadeClassifier.DetectMultiScale(grayImage, 1.2, 1);
            foreach (Rectangle rectangle in rectangles)
            {
                using (Graphics graphics = Graphics.FromImage(bitmp2))
                {
                    using (Pen pen = new Pen(Color.Red, 1))
                    {
                        graphics.DrawRectangle(pen, rectangle);
                    }
                }
            }
            pictureBox2.Image = bitmp2;
        }

        private void Disconnect_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            Video1.Stop();
            Video3.Stop();
            videoSourcePlayer1.Stop();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
            videoSourcePlayer1.Stop();
            Video1.Stop();
            Video3.Stop();
        }

        private void videoSourcePlayer1_NewFrame(object sender, ref Bitmap image)
        {
            c = motion.ProcessFrame(image);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            textBox1.Text = c.ToString();
            listBox1.Items.Add($"Number of Cars Hvae Counted:  +  {c.ToString()}");
        }

        private void tagingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tag frm = new Tag();
            frm.Show();
        }
    }
}
