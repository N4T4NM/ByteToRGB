using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FileEncoder
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void EncodeButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            if (dialog.ShowDialog() == DialogResult.OK)
                Encode(new FileInfo(dialog.FileName));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = "Encoded files|*.png";
            if (dialog.ShowDialog() == DialogResult.OK)
                Decode(new FileInfo(dialog.FileName));
        }

        private void Decode(FileInfo file)
        {
            Bitmap image = Bitmap.FromFile(file.FullName, false) as Bitmap;

            List<byte> header = new List<byte>();
            List<byte> body = new List<byte>();

            string filename = "";

            bool finished = false;
            for(int x=0; x < image.Width; x++)
            {
                if (finished)
                    break;
                for(int y=0; y < image.Height; y++)
                {
                    Color cByte = image.GetPixel(x, y);
                    if (string.IsNullOrEmpty(filename))
                    {
                        if(cByte.A == 240)
                        {
                            if(cByte.R != 255)
                                header.Add(cByte.R);
                            if (cByte.G != 255)
                                header.Add(cByte.G);
                            if (cByte.B != 255)
                                header.Add(cByte.B);
                            filename = Encoding.UTF8.GetString(header.ToArray());
                            Debug.WriteLine(filename);
                            continue;
                        }
                        header.Add(cByte.R);
                        header.Add(cByte.G);
                        header.Add(cByte.B);
                    } else
                    {
                        if(cByte.A == 1)
                        {
                            if (cByte.R != 255)
                                body.Add(cByte.R);
                            if (cByte.G != 255)
                                body.Add(cByte.G);
                            if (cByte.B != 255)
                                body.Add(cByte.B);
                            finished = true;
                            break;
                        }
                        body.Add(cByte.R);
                        body.Add(cByte.G);
                        body.Add(cByte.B);
                    }
                }
            }

            image.Dispose();
            string ext = filename.Split('.')[filename.Split('.').Length - 1];
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = $"Decoded File|*.{ext}";
            if (dialog.ShowDialog() == DialogResult.OK)
                File.WriteAllBytes(dialog.FileName, body.ToArray());
        }

        private void SetByteFinish(List<Color> bytes, List<byte> rgb)
        {
            if (rgb.Count > 0)
            {
                int r = 255, g = 255, b = 255;

                try { r = rgb[0]; g = rgb[1]; b = rgb[2]; } catch (Exception ex) { }
                bytes.Add(Color.FromArgb(240, r, g, b));
            }
            if(bytes[bytes.Count-1].A != 240)
                bytes.Add(Color.FromArgb(240, 255, 255, 255));
        }
        private void SetFileFinish(List<Color> bytes, List<byte> rgb)
        {
            if (rgb.Count > 0)
            {
                int r = 255, g = 255, b = 255;

                try { r = rgb[0]; g = rgb[1]; b = rgb[2]; } catch (Exception ex) { }
                bytes.Add(Color.FromArgb(1, r, g, b));
            }
            if (bytes[bytes.Count - 1].A != 1)
                bytes.Add(Color.FromArgb(1, 255, 255, 255));
        }

        private void Encode(FileInfo file)
        {
            byte[] fileName = Encoding.UTF8.GetBytes(file.Name);
            byte[] fileData = File.ReadAllBytes(file.FullName);
            List<Color> encodedBytes = new List<Color>(fileData.Length);
            
            List<byte> rgb = new List<byte>();
            
            for(int i=0; i < fileName.Length;i++)
            {
                rgb.Add(fileName[i]);
                if(rgb.Count >= 3)
                {
                    encodedBytes.Add(Color.FromArgb(255, rgb[0], rgb[1], rgb[2]));
                    rgb.Clear();
                }
            }
            SetByteFinish(encodedBytes, rgb);
            for(int i=0; i < fileData.Length; i++)
            {
                rgb.Add(fileData[i]);
                if (rgb.Count >= 3)
                {
                    encodedBytes.Add(Color.FromArgb(255, rgb[0], rgb[1], rgb[2]));
                    rgb.Clear();
                }
            }
            SetFileFinish(encodedBytes, rgb);

            int total = Convert.ToInt32(Math.Sqrt(Convert.ToDouble(encodedBytes.Count)));

            Bitmap bmp = new Bitmap(total, total+1, PixelFormat.Format32bppArgb);
            bmp.MakeTransparent();

            int location = 0;
            for(int x=0; x < bmp.Width;x++)
            {
                if (location >= encodedBytes.Count)
                    break;
                for(int y=0; y < bmp.Height;y++)
                {
                    if (location >= encodedBytes.Count)
                        break;
                    bmp.SetPixel(x, y, encodedBytes[location]);
                    location++;
                }
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Encoded File|*.png";
            if (dialog.ShowDialog() == DialogResult.OK)
                bmp.Save(dialog.FileName, ImageFormat.Png);

            bmp.Dispose();
        }
    }
}
