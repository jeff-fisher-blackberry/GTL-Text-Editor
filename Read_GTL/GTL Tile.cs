using System;
using System.Text;
using System.Collections.Generic;

namespace GTL_Editor
{
    class GTL_Tile
    {
        public struct smalltile                                            
        {
            //Properties
            public byte cRed;
            public byte cGreen;
            public byte cBlue;
            public byte c0x;
        }
        public smalltile SmallTile = new smalltile();

        public Int32 FullTileSize;
        public Int32 HalfTileSize;

        public struct bitmapinfoheader                                     
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPels;
            public int biYPels;
            public int biClrUsed;
            public int biClrImportant;
        }
        public bitmapinfoheader BITMAPINFOHEADER = new bitmapinfoheader();

        public Int32 maskRed;
        public Int32 maskGreen;
        public Int32 maskBlue;
    }
}
