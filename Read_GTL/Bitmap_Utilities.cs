using System;
using System.IO;
using gtlDisplay;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GTL_Editor
{
    class Bitmap_Utilities
    {
        //Methods
        public static Image Zoom(Image inputImage)        
        {
            Bitmap gBitmap = new Bitmap(inputImage.Width * 3, inputImage.Height * 3);
            Graphics g = Graphics.FromImage((Image)gBitmap);
            g.PageScale = 3.0f;
            g.PageUnit = GraphicsUnit.Pixel;
            g.DrawImage(inputImage, new Rectangle(0, 0, inputImage.Width, inputImage.Height), 0, 0, inputImage.Width, inputImage.Height, GraphicsUnit.Pixel);
            g.Dispose();
            return (Image) gBitmap;
        }
    }
}
