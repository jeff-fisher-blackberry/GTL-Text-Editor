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
    class GTL_File
    {
        //Structures
        public struct gtltile                                                                    
        {
            public Bitmap FullTile;
            public Bitmap HalfTile;
            public Color SmallTile;
        }
        
        //Properties
        public string Signature                                                                  
        {
            get
            {
                return signature;
            }
        }
        public string Version                                                                    
        {
            get
            {
                return version;
            }
        }
        public UInt32 TileCount                                                                  
        {
            get
            {
                return tilecount;
            }
        }
        public gtltile[] Tile                                                                    
        {
            get
            {
                return tile;
            }
        }

        //Local Variables
        char[] chSignature = new char[4];
        byte[] bVersion = new byte[2];
        string signature;
        string version;
        UInt32 tilecount;
        gtltile[] tile;

        //Methods
        public void WriteFile(string filename, Panel gtlTilePanel)                               
        {
            int totalTiles = gtlTilePanel.Controls.Count;
            byte[] fullTile;
            byte[] halfTile;
            Bitmap bmpfullTile;
            Bitmap bmphalfTile;

            Encoding ascii = Encoding.ASCII;

            BinaryWriter binWriter = new BinaryWriter(File.Open(filename, FileMode.Create));
            binWriter.Write(ascii.GetBytes("G"));
            binWriter.Write(ascii.GetBytes("T"));
            binWriter.Write(ascii.GetBytes("L"));
            binWriter.Write(ascii.GetBytes("B"));
            binWriter.Write(Convert.ToSByte(0));
            binWriter.Write(Convert.ToSByte(3));
            binWriter.Write(totalTiles);

            for (int i = 0; i < totalTiles; i++)
            {

                binWriter.Write(((PictureBox)((gtlTileDisplay)gtlTilePanel.Controls[i]).Controls["smallTile"]).BackColor.R);
                binWriter.Write(((PictureBox)((gtlTileDisplay)gtlTilePanel.Controls[i]).Controls["smallTile"]).BackColor.G);
                binWriter.Write(((PictureBox)((gtlTileDisplay)gtlTilePanel.Controls[i]).Controls["smallTile"]).BackColor.B);
                binWriter.Write(Convert.ToSByte(0));

                bmpfullTile = (Bitmap)((PictureBox)((gtlTileDisplay)gtlTilePanel.Controls[i]).Controls["fullTile"]).Image.Clone();
                bmphalfTile = (Bitmap)((PictureBox)((gtlTileDisplay)gtlTilePanel.Controls[i]).Controls["halfTile"]).Image.Clone();

                fullTile = BmptoGtl(bmpfullTile);

                binWriter.Write(fullTile.Length + 52);
                binWriter.Write(40);
                binWriter.Write(bmpfullTile.Width);
                binWriter.Write(bmpfullTile.Height);
                binWriter.Write(Convert.ToInt16(1));
                binWriter.Write(Convert.ToInt16(16));
                binWriter.Write(3);
                binWriter.Write(fullTile.Length);
                binWriter.Write(0);
                binWriter.Write(0);
                binWriter.Write(0);
                binWriter.Write(0);
                binWriter.Write(63488);
                binWriter.Write(2016);
                binWriter.Write(31);
                for (int j = 0; j < fullTile.Length; j++)
                {
                    binWriter.Write(fullTile[j]);
                }

                halfTile = BmptoGtl(bmphalfTile);

                binWriter.Write(halfTile.Length + 52);
                binWriter.Write(40);
                binWriter.Write(bmphalfTile.Width);
                binWriter.Write(bmphalfTile.Height);
                binWriter.Write(Convert.ToInt16(1));
                binWriter.Write(Convert.ToInt16(16));
                binWriter.Write(3);
                binWriter.Write(halfTile.Length);
                binWriter.Write(0);
                binWriter.Write(0);
                binWriter.Write(0);
                binWriter.Write(0);
                binWriter.Write(63488);
                binWriter.Write(2016);
                binWriter.Write(31);
                for (int j = 0; j < halfTile.Length; j++)
                {
                    binWriter.Write(halfTile[j]);
                }
            }
            binWriter.Close();
        }
        public void ReadFile(string filename)                                                    
        {
            BinaryReader binReader = new BinaryReader(File.Open(filename, FileMode.Open));
            //Read file signature (GTLB)
            for (int i = 0; i <=3; i++)
            {
                chSignature[i] = binReader.ReadChar();
            }
            signature = chSignature[0].ToString() + chSignature[1].ToString() + chSignature[2].ToString() + chSignature[3].ToString();

            //Read file version
            for (int i = 0; i <= 1; i++)
            {
                bVersion[i] = binReader.ReadByte();
            }
            version = bVersion[0].ToString() + bVersion[1].ToString();

            //Read tile count
            tilecount = binReader.ReadUInt32();

            gtltile[] gtlTILE = new gtltile[tilecount];

            GTL_Tile workerTile = new GTL_Tile();

            for (int j = 0; j < tilecount; j++)
            {
                workerTile.SmallTile.cRed = binReader.ReadByte();
                workerTile.SmallTile.cGreen = binReader.ReadByte();
                workerTile.SmallTile.cBlue = binReader.ReadByte();
                workerTile.SmallTile.c0x = binReader.ReadByte();

                gtlTILE[j].SmallTile = Color.FromArgb(workerTile.SmallTile.cRed, workerTile.SmallTile.cGreen, workerTile.SmallTile.cBlue);

                workerTile.FullTileSize = binReader.ReadInt32();

                workerTile.BITMAPINFOHEADER.biSize = binReader.ReadInt32();
                workerTile.BITMAPINFOHEADER.biWidth = binReader.ReadInt32();
                workerTile.BITMAPINFOHEADER.biHeight = binReader.ReadInt32();
                workerTile.BITMAPINFOHEADER.biPlanes = binReader.ReadInt16();
                workerTile.BITMAPINFOHEADER.biBitCount = binReader.ReadInt16();
                workerTile.BITMAPINFOHEADER.biCompression = binReader.ReadInt32();
                workerTile.BITMAPINFOHEADER.biSizeImage = binReader.ReadInt32();
                workerTile.BITMAPINFOHEADER.biXPels = binReader.ReadInt32();
                workerTile.BITMAPINFOHEADER.biYPels = binReader.ReadInt32();
                workerTile.BITMAPINFOHEADER.biClrUsed = binReader.ReadInt32();
                workerTile.BITMAPINFOHEADER.biClrImportant = binReader.ReadInt32();

                if (workerTile.BITMAPINFOHEADER.biBitCount == 16)
                {
                    workerTile.maskRed = binReader.ReadInt32();
                    workerTile.maskGreen = binReader.ReadInt32();
                    workerTile.maskBlue = binReader.ReadInt32();

                    byte[] fullbmpBytes = new byte[workerTile.BITMAPINFOHEADER.biSizeImage];
                    for (int i = 0; i < workerTile.BITMAPINFOHEADER.biSizeImage; i++)
                    {
                        fullbmpBytes[i] = binReader.ReadByte();
                    }
                    gtlTILE[j].FullTile = GtlToBmp(fullbmpBytes, workerTile.BITMAPINFOHEADER.biSizeImage, workerTile.BITMAPINFOHEADER.biWidth, workerTile.BITMAPINFOHEADER.biHeight);

                    workerTile.HalfTileSize = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biSize = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biWidth = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biHeight = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biPlanes = binReader.ReadInt16();
                    workerTile.BITMAPINFOHEADER.biBitCount = binReader.ReadInt16();
                    workerTile.BITMAPINFOHEADER.biCompression = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biSizeImage = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biXPels = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biYPels = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biClrUsed = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biClrImportant = binReader.ReadInt32();

                    workerTile.maskRed = binReader.ReadInt32();
                    workerTile.maskGreen = binReader.ReadInt32();
                    workerTile.maskBlue = binReader.ReadInt32();

                    byte[] halfbmpBytes = new byte[workerTile.BITMAPINFOHEADER.biSizeImage];
                    for (int i = 0; i < workerTile.BITMAPINFOHEADER.biSizeImage; i++)
                    {
                        halfbmpBytes[i] = binReader.ReadByte();
                    }
                    gtlTILE[j].HalfTile = GtlToBmp(halfbmpBytes, workerTile.BITMAPINFOHEADER.biSizeImage, workerTile.BITMAPINFOHEADER.biWidth, workerTile.BITMAPINFOHEADER.biHeight);
                }
                if (workerTile.BITMAPINFOHEADER.biBitCount == 24)
                {
                    byte[] fullbmpBytes = new byte[workerTile.BITMAPINFOHEADER.biSizeImage];
                    for (int i = 0; i < workerTile.BITMAPINFOHEADER.biSizeImage; i++)
                    {
                        fullbmpBytes[i] = binReader.ReadByte();
                    }
                    gtlTILE[j].FullTile = GtlToBmp24(fullbmpBytes, workerTile.BITMAPINFOHEADER.biSizeImage, workerTile.BITMAPINFOHEADER.biWidth, workerTile.BITMAPINFOHEADER.biHeight);

                    workerTile.HalfTileSize = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biSize = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biWidth = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biHeight = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biPlanes = binReader.ReadInt16();
                    workerTile.BITMAPINFOHEADER.biBitCount = binReader.ReadInt16();
                    workerTile.BITMAPINFOHEADER.biCompression = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biSizeImage = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biXPels = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biYPels = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biClrUsed = binReader.ReadInt32();
                    workerTile.BITMAPINFOHEADER.biClrImportant = binReader.ReadInt32();

                    byte[] halfbmpBytes = new byte[workerTile.BITMAPINFOHEADER.biSizeImage];
                    for (int i = 0; i < workerTile.BITMAPINFOHEADER.biSizeImage; i++)
                    {
                        halfbmpBytes[i] = binReader.ReadByte();
                    }
                    gtlTILE[j].HalfTile = GtlToBmp24(halfbmpBytes, workerTile.BITMAPINFOHEADER.biSizeImage, workerTile.BITMAPINFOHEADER.biWidth, workerTile.BITMAPINFOHEADER.biHeight);
                }
            }

            binReader.Close();
          
            tile = gtlTILE;
        }
        private unsafe Bitmap GtlToBmp(byte[] bmpBytes, Int32 byteCount, int width, int height)  
        {
            Bitmap flippedBmp;
            Image workerImage;

            Bitmap nBmp = new Bitmap(width,height,PixelFormat.Format16bppRgb565);
            BitmapData bData = nBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format16bppRgb565);
            
            Marshal.Copy(bmpBytes, 0, bData.Scan0, byteCount);
            
            nBmp.UnlockBits(bData);
            
            workerImage = nBmp;
            workerImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
            flippedBmp = (Bitmap)workerImage;
            return flippedBmp;
        }
        private unsafe Bitmap GtlToBmp24(byte[] bmpBytes, Int32 byteCount, int width, int height)
        {
            Bitmap flippedBmp;
            Image workerImage;

            Bitmap nBmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bData = nBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            Marshal.Copy(bmpBytes, 0, bData.Scan0, byteCount);

            nBmp.UnlockBits(bData);

            workerImage = nBmp;
            workerImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
            flippedBmp = (Bitmap)workerImage;
            return flippedBmp;
        }
        private unsafe byte[] BmptoGtl(Bitmap nBmp)                                              
        {
            Bitmap flippedBmp;
            Image workerImage;

            workerImage = nBmp;
            workerImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
            flippedBmp = (Bitmap)workerImage;

            BitmapData bData = flippedBmp.LockBits(new Rectangle(new Point(), flippedBmp.Size),ImageLockMode.ReadOnly,PixelFormat.Format16bppRgb565);
            int byteCount = bData.Stride * flippedBmp.Height;
            byte[] bmpBytes = new byte[byteCount];

            Marshal.Copy(bData.Scan0, bmpBytes, 0, byteCount);

            flippedBmp.UnlockBits(bData);
             
            return bmpBytes;
        }
    }
}
