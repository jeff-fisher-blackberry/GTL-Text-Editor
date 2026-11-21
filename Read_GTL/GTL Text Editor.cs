using JWC;
using System;
using System.IO;
using gtlDisplay;
using System.Data;
using System.Text;
using System.Drawing;
using Microsoft.Win32;
using System.Threading;
using System.Collections;
using System.Drawing.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Imaging;
using positionDisplayControl;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace GTL_Editor 
{
    public partial class Form1 : Form                                                                 
    {
        // tileDisplay and positionDisplay extracted to separate files

        public class PositionDataDto
        {
            public string TxtText { get; set; } = string.Empty;
            public string TxtIncrement { get; set; } = string.Empty;
            public bool BolIncrementVisible { get; set; }
            public string FontName { get; set; } = "Microsoft Sans Serif";
            public float FontSize { get; set; } = 8.25f;
            public int FontStyle { get; set; }
            public int ColorArgb { get; set; }
            public int Rotation { get; set; }
            public int PointX { get; set; }
            public int PointY { get; set; }
            public string TxtTile { get; set; } = string.Empty;
        }

        private void UpdateWorkingTileGraphics(Image image)
        {
            gWorkingTile?.Dispose();
            gWorkingTile = image != null ? Graphics.FromImage(image) : null;
        }

        private void UpdateRotateTextGraphics(Image image)
        {
            gRotateText?.Dispose();
            gRotateText = image != null ? Graphics.FromImage(image) : null;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            gWorkingTile?.Dispose();
            gRotateText?.Dispose();
            base.OnFormClosed(e);
        }
        [Serializable]
        private struct positionData                                                                    
        {
            public string txtText;
            public string txtIncrement;
            public bool bolIncrementVisible;
            public Font txtFont;
            public Color txtColor;
            public int txtRotation;
            public Point txtPoint;
            public string txtTile;
        }
        //Enumerations
        enum currentworkingtile {FullTile, HalfTile, SmallTile};
        //Local Variables
        GTL_File gtlFile = new GTL_File();
        Bitmap_Utilities bUtilities = new Bitmap_Utilities();
        tileDisplay TileDisplay;
        positionDisplay PositionDisplay;
        int panelHeight;
        bool nonNumberEntered = false;
        private Boolean ColorSelectSmallTile = true;
        int currentWorkingTile;
        Graphics gWorkingTile;
        Graphics gRotateText;
        Image RotateImage;
        string openedFile;
        int panel1Height;
        private Panel panel1Viewport;
        string currentfilename;
        protected MruStripMenuInline mruMenu;
        static string mruRegKey = "SOFTWARE\\GTL Editor";
        Cursor eyedropperCursor = new Cursor(@"Eyedropper.cur");

        // Dragging state tracking variables
        private bool isDraggingText = false;
        private Point dragStartMouseLoc;
        private Point dragStartTextLoc;

        private void EnableDoubleBuffering(Control control)
        {
            if (control == null) return;
            typeof(Control).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, control, new object[] { true });
        }

        private Bitmap GetModernGlyph(string glyph, int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using (Font font = new Font("Segoe MDL2 Assets", size * 0.55f, FontStyle.Regular, GraphicsUnit.Pixel))
                using (Brush brush = new SolidBrush(color))
                {
                    SizeF glyphSize = g.MeasureString(glyph, font);
                    g.DrawString(glyph, font, brush, (size - glyphSize.Width) / 2, (size - glyphSize.Height) / 2);
                }
            }
            return bmp;
        }

        private Bitmap GetContextPasteGoGlyph(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float baseFontSize = size * 0.58f;
                using (Font fBase = new Font("Segoe MDL2 Assets", baseFontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                using (Brush brushBlack = new SolidBrush(Color.Black))
                {
                    g.DrawString("\uE77F", fBase, brushBlack, size * 0.02f, size * 0.02f);
                }

                float badgeD = size * 0.44f;
                float bx = size * 0.52f;
                float by = size * 0.50f;

                using (Brush brushWhite = new SolidBrush(Color.White))
                {
                    g.FillEllipse(brushWhite, bx - 1.5f, by - 1.5f, badgeD + 3.0f, badgeD + 3.0f);
                }

                using (Brush goBrush = new SolidBrush(Color.FromArgb(16, 124, 65)))
                {
                    g.FillEllipse(goBrush, bx, by, badgeD, badgeD);
                }

                float arrowSize = badgeD * 0.65f;
                using (Font fArrow = new Font("Segoe MDL2 Assets", arrowSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush brushWhite = new SolidBrush(Color.White))
                {
                    SizeF szArr = g.MeasureString("\uE72A", fArrow);
                    float ax = bx + (badgeD - szArr.Width) / 2.0f;
                    float ay = by + (badgeD - szArr.Height) / 2.0f;
                    g.DrawString("\uE72A", fArrow, brushWhite, ax, ay);
                }
            }
            return bmp;
        }

        private Bitmap GetContextCopyGlyph(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float fontSize = size * 0.70f;
                using (Font font = new Font("Segoe MDL2 Assets", fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                using (Brush brush = new SolidBrush(Color.Black))
                {
                    SizeF sz = g.MeasureString("\uE8C8", font);
                    g.DrawString("\uE8C8", font, brush, (size - sz.Width) / 2.0f, (size - sz.Height) / 2.0f);
                }
            }
            return bmp;
        }

        private Bitmap GetArrowRangeGlyph(int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float scale = size / 48.0f;
                float penWidth = Math.Max(1.4f, 2.8f * scale);
                using (Pen pen = new Pen(color, penWidth))
                using (Brush brush = new SolidBrush(color))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    float tickMargin = 8.0f * scale;
                    float tickHeight = 24.0f * scale;
                    float midY = size / 2.0f;
                    float tTop = midY - tickHeight / 2.0f;
                    float tBottom = midY + tickHeight / 2.0f;

                    // Vertical boundary ticks on left and right
                    g.DrawLine(pen, tickMargin, tTop, tickMargin, tBottom);
                    g.DrawLine(pen, size - tickMargin, tTop, size - tickMargin, tBottom);

                    // Horizontal span line between ticks
                    g.DrawLine(pen, tickMargin + 2.0f * scale, midY, size - tickMargin - 2.0f * scale, midY);

                    // Arrowheads
                    float arrLen = 8.5f * scale;
                    float arrHalf = 5.0f * scale;

                    PointF[] ptsLeft = new PointF[]
                    {
                        new PointF(tickMargin + 1.5f * scale, midY),
                        new PointF(tickMargin + 1.5f * scale + arrLen, midY - arrHalf),
                        new PointF(tickMargin + 1.5f * scale + arrLen, midY + arrHalf)
                    };
                    g.FillPolygon(brush, ptsLeft);

                    PointF[] ptsRight = new PointF[]
                    {
                        new PointF(size - tickMargin - 1.5f * scale, midY),
                        new PointF(size - tickMargin - 1.5f * scale - arrLen, midY - arrHalf),
                        new PointF(size - tickMargin - 1.5f * scale - arrLen, midY + arrHalf)
                    };
                    g.FillPolygon(brush, ptsRight);
                }
            }
            return bmp;
        }

        private Bitmap GetHeightGlyph(int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float scale = size / 48.0f;
                float penWidth = Math.Max(1.4f, 2.8f * scale);
                using (Pen pen = new Pen(color, penWidth))
                using (Brush brush = new SolidBrush(color))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    float tickMargin = 8.0f * scale;
                    float tickWidth = 24.0f * scale;
                    float midX = size / 2.0f;
                    float tLeft = midX - tickWidth / 2.0f;
                    float tRight = midX + tickWidth / 2.0f;

                    // Horizontal boundary ticks on top and bottom
                    g.DrawLine(pen, tLeft, tickMargin, tRight, tickMargin);
                    g.DrawLine(pen, tLeft, size - tickMargin, tRight, size - tickMargin);

                    // Vertical span line between ticks
                    g.DrawLine(pen, midX, tickMargin + 2.0f * scale, midX, size - tickMargin - 2.0f * scale);

                    // Arrowheads
                    float arrLen = 8.5f * scale;
                    float arrHalf = 5.0f * scale;

                    PointF[] ptsTop = new PointF[]
                    {
                        new PointF(midX, tickMargin + 1.5f * scale),
                        new PointF(midX - arrHalf, tickMargin + 1.5f * scale + arrLen),
                        new PointF(midX + arrHalf, tickMargin + 1.5f * scale + arrLen)
                    };
                    g.FillPolygon(brush, ptsTop);

                    PointF[] ptsBottom = new PointF[]
                    {
                        new PointF(midX, size - tickMargin - 1.5f * scale),
                        new PointF(midX - arrHalf, size - tickMargin - 1.5f * scale - arrLen),
                        new PointF(midX + arrHalf, size - tickMargin - 1.5f * scale - arrLen)
                    };
                    g.FillPolygon(brush, ptsBottom);
                }
            }
            return bmp;
        }

        private Bitmap GetMaterialRotateRightGlyph(int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                float scale = size / 24.0f;
                float S(float v) => v * scale;

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.FillMode = FillMode.Winding;
                    path.StartFigure();
                    path.AddLine(S(15.55f), S(5.55f), S(11.0f), S(1.0f));
                    path.AddLine(S(11.0f), S(1.0f), S(11.0f), S(4.07f));
                    path.AddBezier(S(11.0f), S(4.07f), S(7.06f), S(4.56f), S(4.0f), S(7.92f), S(4.0f), S(12.0f));
                    path.AddBezier(S(4.0f), S(12.0f), S(4.0f), S(16.08f), S(7.06f), S(19.44f), S(11.0f), S(19.93f));
                    path.AddLine(S(11.0f), S(19.93f), S(11.0f), S(17.91f));
                    path.AddBezier(S(11.0f), S(17.91f), S(8.16f), S(17.43f), S(6.0f), S(14.97f), S(6.0f), S(12.0f));
                    path.AddBezier(S(6.0f), S(12.0f), S(6.0f), S(9.03f), S(8.16f), S(6.57f), S(11.0f), S(6.09f));
                    path.AddLine(S(11.0f), S(6.09f), S(11.0f), S(10.0f));
                    path.AddLine(S(11.0f), S(10.0f), S(15.55f), S(5.55f));
                    path.CloseFigure();

                    using (Brush brush = new SolidBrush(color))
                    {
                        g.FillPath(brush, path);
                    }
                }
            }
            return bmp;
        }

        private Bitmap GetMaterialRotateLeftGlyph(int size, Color color)
        {
            Bitmap bmp = GetMaterialRotateRightGlyph(size, color);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
            return bmp;
        }

        private static GraphicsPath ParseSvgPath(string pathData, float targetSize, float srcViewBox = 24.0f)
        {
            GraphicsPath path = new GraphicsPath();
            path.FillMode = FillMode.Winding;

            float scale = targetSize / srcViewBox;
            List<string> tokens = new List<string>();
            int i = 0;
            while (i < pathData.Length)
            {
                char c = pathData[i];
                if (char.IsWhiteSpace(c) || c == ',')
                {
                    i++;
                    continue;
                }
                if (char.IsLetter(c))
                {
                    tokens.Add(c.ToString());
                    i++;
                    continue;
                }
                int start = i;
                if (pathData[i] == '-' || pathData[i] == '+') i++;
                bool hasDot = false;
                while (i < pathData.Length && (char.IsDigit(pathData[i]) || (!hasDot && pathData[i] == '.')))
                {
                    if (pathData[i] == '.') hasDot = true;
                    i++;
                }
                tokens.Add(pathData.Substring(start, i - start));
            }

            float curX = 0, curY = 0;
            float startFigX = 0, startFigY = 0;
            float lastControlX = 0, lastControlY = 0;
            char lastCmd = ' ';
            int tIdx = 0;

            float S(float v) => v * scale;

            while (tIdx < tokens.Count)
            {
                string token = tokens[tIdx];
                char cmd = token[0];
                if (char.IsLetter(cmd))
                {
                    tIdx++;
                }
                else
                {
                    cmd = (lastCmd == 'M') ? 'L' : (lastCmd == 'm') ? 'l' : lastCmd;
                }

                switch (cmd)
                {
                    case 'M':
                    case 'm':
                        {
                            float x = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            float y = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            curX = (cmd == 'm') ? curX + x : x;
                            curY = (cmd == 'm') ? curY + y : y;
                            startFigX = curX;
                            startFigY = curY;
                            lastControlX = curX;
                            lastControlY = curY;
                            path.StartFigure();
                            break;
                        }
                    case 'L':
                    case 'l':
                        {
                            float x = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            float y = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            float nextX = (cmd == 'l') ? curX + x : x;
                            float nextY = (cmd == 'l') ? curY + y : y;
                            path.AddLine(S(curX), S(curY), S(nextX), S(nextY));
                            curX = nextX;
                            curY = nextY;
                            lastControlX = curX;
                            lastControlY = curY;
                            break;
                        }
                    case 'H':
                    case 'h':
                        {
                            float x = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            float nextX = (cmd == 'h') ? curX + x : x;
                            path.AddLine(S(curX), S(curY), S(nextX), S(curY));
                            curX = nextX;
                            lastControlX = curX;
                            break;
                        }
                    case 'V':
                    case 'v':
                        {
                            float y = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            float nextY = (cmd == 'v') ? curY + y : y;
                            path.AddLine(S(curX), S(curY), S(curX), S(nextY));
                            curY = nextY;
                            lastControlY = curY;
                            break;
                        }
                    case 'Q':
                    case 'q':
                        {
                            float x1 = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            float y1 = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            float x = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            float y = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);

                            float cX = (cmd == 'q') ? curX + x1 : x1;
                            float cY = (cmd == 'q') ? curY + y1 : y1;
                            float endX = (cmd == 'q') ? curX + x : x;
                            float endY = (cmd == 'q') ? curY + y : y;

                            float cp1x = curX + (2.0f / 3.0f) * (cX - curX);
                            float cp1y = curY + (2.0f / 3.0f) * (cY - curY);
                            float cp2x = endX + (2.0f / 3.0f) * (cX - endX);
                            float cp2y = endY + (2.0f / 3.0f) * (cY - endY);

                            path.AddBezier(S(curX), S(curY), S(cp1x), S(cp1y), S(cp2x), S(cp2y), S(endX), S(endY));

                            lastControlX = cX;
                            lastControlY = cY;
                            curX = endX;
                            curY = endY;
                            break;
                        }
                    case 'T':
                    case 't':
                        {
                            float cX = 2 * curX - lastControlX;
                            float cY = 2 * curY - lastControlY;

                            float x = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            float y = float.Parse(tokens[tIdx++], System.Globalization.CultureInfo.InvariantCulture);
                            float endX = (cmd == 't') ? curX + x : x;
                            float endY = (cmd == 't') ? curY + y : y;

                            float cp1x = curX + (2.0f / 3.0f) * (cX - curX);
                            float cp1y = curY + (2.0f / 3.0f) * (cY - curY);
                            float cp2x = endX + (2.0f / 3.0f) * (cX - endX);
                            float cp2y = endY + (2.0f / 3.0f) * (cY - endY);

                            path.AddBezier(S(curX), S(curY), S(cp1x), S(cp1y), S(cp2x), S(cp2y), S(endX), S(endY));

                            lastControlX = cX;
                            lastControlY = cY;
                            curX = endX;
                            curY = endY;
                            break;
                        }
                    case 'Z':
                    case 'z':
                        {
                            path.CloseFigure();
                            curX = startFigX;
                            curY = startFigY;
                            lastControlX = curX;
                            lastControlY = curY;
                            break;
                        }
                }
                lastCmd = cmd;
            }

            return path;
        }

        private Bitmap GetFontDownloadGlyph(int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                string svgPathData = "M6.4,18H8.5l1.1-3.05h4.8L15.5,18h2.1L13.05,6h-2.1L6.4,18Zm3.8-4.8l1.75-4.95h0.1L13.8,13.2H10.2ZM4,22q-0.82,0-1.41-0.59T2,20V4Q2,3.17 2.59,2.59T4,2H20q0.83,0 1.41,0.59T22,4V20q0,0.82-0.59,1.41T20,22H4Zm0-2H20V4H4V20ZM4,4V20V4Z";

                using (GraphicsPath path = ParseSvgPath(svgPathData, size, 24.0f))
                using (Brush brush = new SolidBrush(color))
                {
                    g.FillPath(brush, path);
                }
            }
            return bmp;
        }

        private Bitmap GetFormatColorTextGlyph(int size, Color textColor, Color barColor)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float scale = size / 48.0f;
                float fontPixelSize = 27.0f * scale;

                using (Font fontA = new Font("Segoe UI", fontPixelSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush brushText = new SolidBrush(textColor))
                using (Brush brushBar = new SolidBrush(barColor))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
                {
                    g.DrawString("A", fontA, brushText, size / 2.0f, 4.0f * scale, sf);

                    float barX = 9.0f * scale;
                    float barY = 37.0f * scale;
                    float barW = 30.0f * scale;
                    float barH = 5.0f * scale;
                    g.FillRectangle(brushBar, barX, barY, barW, barH);
                }
            }
            return bmp;
        }

        private Bitmap GetTextIncreaseGlyph(int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float scale = size / 48.0f;
                float fontPixelSize = 25.0f * scale;

                using (Font fontA = new Font("Segoe UI", fontPixelSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush brush = new SolidBrush(color))
                using (Pen pen = new Pen(color, Math.Max(1.8f, 2.6f * scale)))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    g.DrawString("A", fontA, brush, 4.0f * scale, 8.0f * scale);

                    float arrowX = 36.0f * scale;
                    float arrowBottom = 34.0f * scale;
                    float arrowTop = 16.0f * scale;
                    g.DrawLine(pen, arrowX, arrowBottom, arrowX, arrowTop);

                    float headLen = 6.5f * scale;
                    float headHalf = 5.0f * scale;
                    PointF[] arrowPts = new PointF[]
                    {
                        new PointF(arrowX, arrowTop - 2.0f * scale),
                        new PointF(arrowX - headHalf, arrowTop + headLen - 2.0f * scale),
                        new PointF(arrowX + headHalf, arrowTop + headLen - 2.0f * scale)
                    };
                    g.FillPolygon(brush, arrowPts);
                }
            }
            return bmp;
        }

        private Bitmap GetTextDecreaseGlyph(int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float scale = size / 48.0f;
                float fontPixelSize = 25.0f * scale;

                using (Font fontA = new Font("Segoe UI", fontPixelSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush brush = new SolidBrush(color))
                using (Pen pen = new Pen(color, Math.Max(1.8f, 2.6f * scale)))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    g.DrawString("A", fontA, brush, 4.0f * scale, 8.0f * scale);

                    float arrowX = 36.0f * scale;
                    float arrowTop = 16.0f * scale;
                    float arrowBottom = 34.0f * scale;
                    g.DrawLine(pen, arrowX, arrowBottom, arrowX, arrowTop);

                    float headLen = 6.5f * scale;
                    float headHalf = 5.0f * scale;
                    PointF[] arrowPts = new PointF[]
                    {
                        new PointF(arrowX, arrowBottom + 2.0f * scale),
                        new PointF(arrowX - headHalf, arrowBottom - headLen + 2.0f * scale),
                        new PointF(arrowX + headHalf, arrowBottom - headLen + 2.0f * scale)
                    };
                    g.FillPolygon(brush, arrowPts);
                }
            }
            return bmp;
        }

        private Bitmap GetFormatBoldGlyph(int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float scale = size / 48.0f;
                float fontPixelSize = 31.0f * scale;

                using (Font fontB = new Font("Segoe UI", fontPixelSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush brush = new SolidBrush(color))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("B", fontB, brush, new RectangleF(0, 0, size, size), sf);
                }
            }
            return bmp;
        }

        private Bitmap GetFormatItalicGlyph(int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float scale = size / 48.0f;
                float fontPixelSize = 31.0f * scale;

                using (Font fontI = new Font("Georgia", fontPixelSize, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Pixel))
                using (Brush brush = new SolidBrush(color))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("I", fontI, brush, new RectangleF(0, 0, size, size), sf);
                }
            }
            return bmp;
        }

        private Bitmap GetLowDensityTileGlyph(int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float scale = size / 48.0f;
                float margin = 8.0f * scale;
                float x0 = margin;
                float y0 = margin;
                float x1 = size - margin;
                float y1 = size - margin;
                float stroke = Math.Max(2.0f, 3.0f * scale);

                // Bottom and Right solid L-bracket
                using (Pen solidPen = new Pen(color, stroke))
                {
                    solidPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    solidPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    solidPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    g.DrawLines(solidPen, new PointF[]
                    {
                        new PointF(x0, y1),
                        new PointF(x1, y1),
                        new PointF(x1, y0)
                    });
                }

                // Top and Left segmented / dashed border
                using (Pen dashPen = new Pen(color, stroke))
                {
                    dashPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    dashPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    dashPen.DashCap = System.Drawing.Drawing2D.DashCap.Round;
                    dashPen.DashPattern = new float[] { 2.4f, 2.2f };
                    dashPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    g.DrawLines(dashPen, new PointF[]
                    {
                        new PointF(x1, y0),
                        new PointF(x0, y0),
                        new PointF(x0, y1)
                    });
                }

                // Inner sub-square in the upper-left quadrant (half-size tile representation)
                float innerMargin = stroke * 1.3f;
                float innerX = x0 + innerMargin;
                float innerY = y0 + innerMargin;
                float innerW = (x1 - x0) * 0.44f;
                float innerH = (y1 - y0) * 0.44f;

                using (Brush subBrush = new SolidBrush(Color.FromArgb(40, color)))
                {
                    g.FillRectangle(subBrush, innerX, innerY, innerW, innerH);
                }

                using (Pen subPen = new Pen(color, Math.Max(1.4f, stroke * 0.70f)))
                {
                    subPen.DashCap = System.Drawing.Drawing2D.DashCap.Round;
                    subPen.DashPattern = new float[] { 1.8f, 1.8f };
                    g.DrawRectangle(subPen, innerX, innerY, innerW, innerH);
                }
            }
            return bmp;
        }

        private void HigherContrastGroupBox_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            if (box == null) return;

            Color borderColor = Color.FromArgb(140, 140, 140);
            using (Pen pen = new Pen(borderColor, 1.0f))
            {
                Size textSize = TextRenderer.MeasureText(e.Graphics, box.Text, box.Font);
                int top = textSize.Height / 2;
                int textOffset = 8;
                int textPadding = 3;

                if (string.IsNullOrEmpty(box.Text))
                {
                    e.Graphics.DrawRectangle(pen, 0, top, box.Width - 1, box.Height - top - 1);
                }
                else
                {
                    int textStart = Math.Max(0, textOffset - textPadding);
                    int textEnd = Math.Min(box.Width - 1, textOffset + textSize.Width + textPadding);

                    // Top left segment
                    if (textStart > 0)
                    {
                        e.Graphics.DrawLine(pen, 0, top, textStart, top);
                    }

                    // Top right segment
                    if (box.Width - 1 > textEnd)
                    {
                        e.Graphics.DrawLine(pen, textEnd, top, box.Width - 1, top);
                    }

                    // Right segment
                    e.Graphics.DrawLine(pen, box.Width - 1, top, box.Width - 1, box.Height - 1);

                    // Bottom segment
                    e.Graphics.DrawLine(pen, box.Width - 1, box.Height - 1, 0, box.Height - 1);

                    // Left segment
                    e.Graphics.DrawLine(pen, 0, box.Height - 1, 0, top);
                }
            }
        }

        private void ApplyHigherContrastBorders()
        {
            GroupBox[] groupBoxes = new GroupBox[] { groupBox3, groupBox1, groupBox4, groupBox2 };
            foreach (GroupBox gb in groupBoxes)
            {
                if (gb == null) continue;
                gb.Paint += HigherContrastGroupBox_Paint;
                gb.Resize += (s, e) => gb.Invalidate();
            }

            if (panel1Viewport != null)
            {
                panel1Viewport.BorderStyle = BorderStyle.FixedSingle;
            }

            if (panel2 != null)
            {
                panel2.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        private void UpdateTilePanelBackground()
        {
            if (panel1 == null || panel1Viewport == null) return;
            bool hasTiles = panel1.Controls.Count > 0;
            Color targetColor = hasTiles ? Color.White : SystemColors.Control;
            if (panel1.BackColor != targetColor)
            {
                panel1.BackColor = targetColor;
            }
            if (panel1Viewport.BackColor != targetColor)
            {
                panel1Viewport.BackColor = targetColor;
            }
        }

        private void RenderRotationButtonImage(Button btn, string text, float angle)
        {
            if (btn == null) return;
            int w = btn.ClientSize.Width;
            int h = btn.ClientSize.Height;
            if (w <= 0 || h <= 0)
            {
                w = btn.Width;
                h = btn.Height;
            }
            if (w <= 0 || h <= 0)
            {
                w = 38;
                h = 54;
            }

            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                
                g.TranslateTransform(w / 2f, h / 2f);
                g.RotateTransform(angle);
                
                using (Font font = new Font(this.Font.FontFamily, 10.0f, FontStyle.Regular))
                using (Brush brush = new SolidBrush(Color.Black))
                {
                    SizeF size = g.MeasureString(text, font);
                    g.DrawString(text, font, brush, -size.Width / 2f, -size.Height / 2f);
                }
                g.ResetTransform();
            }
            if (btn.Image != null)
            {
                btn.Image.Dispose();
            }
            btn.Image = bmp;
        }

        private void LayoutRotationControls()
        {
            if (btnRotate180 == null || btnRotate0 == null || btnRotateminus90 == null || btnRotate90 == null ||
                pctRotate == null || txtRotate == null || hScrollRotate == null || lblTextPlacement == null)
            {
                return;
            }

            int rotationY = chkIncrement.Bottom + 16;

            int horizBtnW = 54;
            int horizBtnH = 32;
            int vertBtnW = 38;
            int vertBtnH = 46;
            int crossGap = 4;

            int crossMidX = 14 + horizBtnW + crossGap;
            btnRotateminus90.Location = new Point(crossMidX, rotationY);
            btnRotateminus90.Size = new Size(vertBtnW, vertBtnH);

            int midRowY = btnRotateminus90.Bottom + crossGap;
            btnRotate180.Location = new Point(14, midRowY);
            btnRotate180.Size = new Size(horizBtnW, horizBtnH);

            btnRotate0.Location = new Point(crossMidX + vertBtnW + crossGap, midRowY);
            btnRotate0.Size = new Size(horizBtnW, horizBtnH);

            btnRotate90.Location = new Point(crossMidX, midRowY + horizBtnH + crossGap);
            btnRotate90.Size = new Size(vertBtnW, vertBtnH);

            int rightAreaX = btnRotate0.Right + 16;
            int rightAreaWidth = groupBox1.ClientSize.Width - rightAreaX - 14;

            pctRotate.Size = new Size(80, 80);
            pctRotate.Location = new Point(rightAreaX + (rightAreaWidth - pctRotate.Width) / 2, rotationY);

            int angleControlsY = pctRotate.Bottom + 12;
            txtRotate.Font = new Font(this.Font.FontFamily, 10f, FontStyle.Regular);
            txtRotate.TextAlign = HorizontalAlignment.Center;
            txtRotate.Size = new Size(50, 28);
            txtRotate.Location = new Point(rightAreaX, angleControlsY);

            hScrollRotate.Left = txtRotate.Right + 8;
            hScrollRotate.Height = 24;
            hScrollRotate.Top = txtRotate.Top + (txtRotate.Height - hScrollRotate.Height) / 2;
            hScrollRotate.Width = groupBox1.ClientSize.Width - hScrollRotate.Left - 14;
            hScrollRotate.BringToFront();

            RenderRotationButtonImage(btnRotate0, "0", 0.0f);
            RenderRotationButtonImage(btnRotate90, "90", 90.0f);
            RenderRotationButtonImage(btnRotate180, "180", 180.0f);
            RenderRotationButtonImage(btnRotateminus90, "270", 270.0f);
        }

        private bool dropdownHighlightFixConfigured = false;

        private void SetupDropdownHighlightFix(ComboBox cbo)
        {
            if (cbo == null) return;

            Action clearSel = () =>
            {
                if (cbo.IsHandleCreated)
                {
                    cbo.BeginInvoke(new Action(() =>
                    {
                        cbo.SelectionLength = 0;
                        cbo.SelectionStart = 0;
                    }));
                }
                else
                {
                    cbo.SelectionLength = 0;
                    cbo.SelectionStart = 0;
                }
            };

            cbo.SelectedIndexChanged += (s, e) => clearSel();
            cbo.DropDownClosed += (s, e) => clearSel();
            cbo.Leave += (s, e) => clearSel();
            cbo.MouseUp += (s, e) => clearSel();
            cbo.Enter += (s, e) => clearSel();
        }

        private void LayoutTileWorkingArea()
        {
            if (groupBox3 == null || txtWidth == null || label1 == null || txtHeight == null ||
                cboCornerSize == null || btnCorners == null || btnStretch == null || btnResize == null ||
                cboBorderSize == null || btnGradient == null || pctTransparentColor == null || label2 == null ||
                cboClear == null || btnClear == null)
            {
                return;
            }

            // Wire highlight clear handlers on dropdowns to prevent persistent blue selection
            if (!dropdownHighlightFixConfigured)
            {
                dropdownHighlightFixConfigured = true;
                SetupDropdownHighlightFix(cboCornerSize);
                SetupDropdownHighlightFix(cboBorderSize);
                SetupDropdownHighlightFix(cboClear);
            }

            // Enlarge groupBox3 to standard 390 width
            groupBox3.Size = new Size(390, 156);

            // Clear any ImageList on text buttons to avoid reserved icon space clipping text
            btnStretch.ImageList = null;
            btnResize.ImageList = null;
            btnCorners.ImageList = null;
            btnGradient.ImageList = null;
            btnClear.ImageList = null;

            btnStretch.TextAlign = ContentAlignment.MiddleCenter;
            btnResize.TextAlign = ContentAlignment.MiddleCenter;
            btnCorners.TextAlign = ContentAlignment.MiddleCenter;
            btnGradient.TextAlign = ContentAlignment.MiddleCenter;
            btnClear.TextAlign = ContentAlignment.MiddleCenter;

            Font btnFont = new Font(this.Font.FontFamily, 9.75f, FontStyle.Regular);
            Font inputFont = new Font(this.Font.FontFamily, 10f, FontStyle.Regular);
            Font labelFont = new Font(this.Font.FontFamily, 9.5f, FontStyle.Regular);

            btnStretch.Font = btnFont;
            btnResize.Font = btnFont;
            btnCorners.Font = btnFont;
            btnGradient.Font = btnFont;
            btnClear.Font = btnFont;

            txtWidth.Font = inputFont;
            txtHeight.Font = inputFont;
            label1.Font = inputFont;
            cboCornerSize.Font = inputFont;
            cboBorderSize.Font = inputFont;
            cboClear.Font = inputFont;
            label2.Font = labelFont;

            int leftColX = 14;
            int rightButtonWidth = 86;
            int rightDropdownWidth = 54;
            int rightButtonX = groupBox3.ClientSize.Width - 14 - rightButtonWidth;
            int rightDropdownX = rightButtonX - 6 - rightDropdownWidth;

            int row1Y = 24;
            int row2Y = 62;
            int row3Y = 100;
            int buttonHeight = 32;

            // Row 1: Dimensions (Left) & Clip (Right)
            txtWidth.Location = new Point(leftColX, row1Y);
            txtWidth.Size = new Size(52, 26);

            label1.Text = "x";
            label1.AutoSize = true;
            label1.Location = new Point(txtWidth.Right + 5, row1Y + (txtWidth.Height - label1.Height) / 2);

            txtHeight.Location = new Point(label1.Right + 5, row1Y);
            txtHeight.Size = new Size(52, 26);

            cboCornerSize.Location = new Point(rightDropdownX, row1Y + 1);
            cboCornerSize.Size = new Size(rightDropdownWidth, 26);

            btnCorners.Location = new Point(rightButtonX, row1Y);
            btnCorners.Size = new Size(rightButtonWidth, buttonHeight);

            // Row 2: Size Actions (Left) & Bevel (Right)
            btnStretch.Location = new Point(leftColX, row2Y);
            btnStretch.Size = new Size(88, buttonHeight);

            btnResize.Location = new Point(btnStretch.Right + 8, row2Y);
            btnResize.Size = new Size(88, buttonHeight);

            cboBorderSize.Location = new Point(rightDropdownX, row2Y + 1);
            cboBorderSize.Size = new Size(rightDropdownWidth, 26);

            btnGradient.Location = new Point(rightButtonX, row2Y);
            btnGradient.Size = new Size(rightButtonWidth, buttonHeight);

            // Row 3: Transparent Color Swatch & Label (Left) & Clear (Right)
            pctTransparentColor.Location = new Point(leftColX, row3Y + 3);
            pctTransparentColor.Size = new Size(30, 26);

            label2.AutoSize = true;
            label2.Location = new Point(pctTransparentColor.Right + 8, row3Y + (pctTransparentColor.Height - label2.Height) / 2 + 3);

            cboClear.Location = new Point(rightDropdownX, row3Y + 1);
            cboClear.Size = new Size(rightDropdownWidth, 26);

            btnClear.Location = new Point(rightButtonX, row3Y);
            btnClear.Size = new Size(rightButtonWidth, buttonHeight);

            // Ensure resting dropdowns have zero selection length
            cboCornerSize.SelectionLength = 0;
            cboBorderSize.SelectionLength = 0;
            cboClear.SelectionLength = 0;
        }

        private void LayoutTextWorkingArea()
        {
            if (groupBox1 == null || btnReset == null || btnAlignLeft == null || btnAlignCenter == null ||
                btnAlignRights == null || btnExtendRange == null || btnAlignTop == null || btnAlignMiddle == null ||
                btnAlignBottom == null || btnReduceRange == null || vScrollText == null || txtText == null ||
                lblTileSize == null || lblTextPlacement == null || hScrollText == null || btnHideControls == null ||
                btnPlace == null || btnStore == null || btnReplace == null || chkIncrement == null)
            {
                return;
            }

            // Remove ImageLists on text-only buttons to prevent text clipping
            btnReset.ImageList = null;
            btnHideControls.ImageList = null;
            btnPlace.ImageList = null;
            btnStore.ImageList = null;
            btnReplace.ImageList = null;

            btnReset.TextAlign = ContentAlignment.MiddleCenter;
            btnHideControls.TextAlign = ContentAlignment.MiddleCenter;
            btnPlace.TextAlign = ContentAlignment.MiddleCenter;
            btnStore.TextAlign = ContentAlignment.MiddleCenter;
            btnReplace.TextAlign = ContentAlignment.MiddleCenter;

            Font btnFont = new Font(this.Font.FontFamily, 9.75f, FontStyle.Regular);
            btnReset.Font = btnFont;
            btnHideControls.Font = btnFont;
            btnPlace.Font = btnFont;
            btnStore.Font = btnFont;
            btnReplace.Font = btnFont;

            int buttonHeight = 32;

            btnReset.Location = new Point(14, 24);
            btnReset.Size = new Size(84, buttonHeight);

            btnAlignLeft.Location = new Point(btnReset.Right + 12, 24);
            btnAlignLeft.Size = new Size(32, buttonHeight);

            btnAlignCenter.Location = new Point(btnAlignLeft.Right + 6, 24);
            btnAlignCenter.Size = new Size(32, buttonHeight);

            btnAlignRights.Location = new Point(btnAlignCenter.Right + 6, 24);
            btnAlignRights.Size = new Size(32, buttonHeight);

            int verticalStackX = 14;
            int startY = btnReset.Bottom + 14;
            int stackBtnW = 32;
            int stackBtnH = 30;
            int stackGap = 5;

            btnExtendRange.Location = new Point(verticalStackX, startY);
            btnExtendRange.Size = new Size(stackBtnW, stackBtnH);

            btnAlignTop.Location = new Point(verticalStackX, btnExtendRange.Bottom + stackGap);
            btnAlignTop.Size = new Size(stackBtnW, stackBtnH);

            btnAlignMiddle.Location = new Point(verticalStackX, btnAlignTop.Bottom + stackGap);
            btnAlignMiddle.Size = new Size(stackBtnW, stackBtnH);

            btnAlignBottom.Location = new Point(verticalStackX, btnAlignMiddle.Bottom + stackGap);
            btnAlignBottom.Size = new Size(stackBtnW, stackBtnH);

            btnReduceRange.Location = new Point(verticalStackX, btnAlignBottom.Bottom + stackGap);
            btnReduceRange.Size = new Size(stackBtnW, stackBtnH);

            int stackBottom = btnReduceRange.Bottom;

            vScrollText.Left = verticalStackX + stackBtnW + 8;
            vScrollText.Top = startY;
            vScrollText.Width = 24;
            vScrollText.Height = stackBottom - startY;
            vScrollText.BringToFront();

            int rightColLeft = vScrollText.Right + 8;
            int rightColWidth = groupBox1.ClientSize.Width - rightColLeft - 14;

            txtText.Font = new Font(this.Font.FontFamily, 11f, FontStyle.Regular);
            txtText.Left = rightColLeft;
            txtText.Top = startY;
            txtText.Width = rightColWidth;

            lblTileSize.AutoSize = false;
            lblTileSize.Font = new Font(this.Font.FontFamily, 10f, FontStyle.Regular);
            lblTileSize.TextAlign = ContentAlignment.MiddleCenter;
            lblTileSize.Left = rightColLeft;
            lblTileSize.Top = txtText.Bottom + 12;
            lblTileSize.Width = rightColWidth;
            lblTileSize.Height = 26;

            lblTextPlacement.AutoSize = false;
            lblTextPlacement.Font = new Font(this.Font.FontFamily, 10f, FontStyle.Regular);
            lblTextPlacement.TextAlign = ContentAlignment.MiddleCenter;
            lblTextPlacement.Left = rightColLeft;
            lblTextPlacement.Top = lblTileSize.Bottom + 10;
            lblTextPlacement.Width = rightColWidth;
            lblTextPlacement.Height = 26;

            hScrollText.Left = rightColLeft;
            hScrollText.Width = rightColWidth;
            hScrollText.Height = 24;
            hScrollText.Top = stackBottom - hScrollText.Height;
            hScrollText.BringToFront();

            int buttonY = stackBottom + 16;
            int leftMargin = 14;
            int rightMargin = 14;
            int totalUsable = groupBox1.ClientSize.Width - leftMargin - rightMargin;

            btnReplace.Margin = new Padding(1);
            btnReplace.Padding = new Padding(0);

            if (btnHideControls.Visible)
            {
                int btnGap = 6;
                int hideW = 68;
                int remaining = totalUsable - hideW - (3 * btnGap);
                int otherW = remaining / 3;
                if (otherW < 76) otherW = 76;

                btnHideControls.Location = new Point(leftMargin, buttonY);
                btnHideControls.Size = new Size(hideW, buttonHeight);

                btnPlace.Location = new Point(btnHideControls.Right + btnGap, buttonY);
                btnPlace.Size = new Size(otherW, buttonHeight);

                btnStore.Location = new Point(btnPlace.Right + btnGap, buttonY);
                btnStore.Size = new Size(otherW, buttonHeight);

                btnReplace.Location = new Point(btnStore.Right + btnGap, buttonY);
                btnReplace.Size = new Size(otherW, buttonHeight);
            }
            else
            {
                int btnGap = 10;
                int btnW = (totalUsable - (2 * btnGap)) / 3;
                if (btnW > 105) btnW = 105;
                int startX = leftMargin + (totalUsable - (3 * btnW + 2 * btnGap)) / 2;

                btnPlace.Location = new Point(startX, buttonY);
                btnPlace.Size = new Size(btnW, buttonHeight);

                btnStore.Location = new Point(btnPlace.Right + btnGap, buttonY);
                btnStore.Size = new Size(btnW, buttonHeight);

                btnReplace.Location = new Point(btnStore.Right + btnGap, buttonY);
                btnReplace.Size = new Size(btnW, buttonHeight);
            }

            chkIncrement.Font = new Font(this.Font.FontFamily, 9.75f, FontStyle.Regular);
            chkIncrement.AutoSize = true;
            chkIncrement.Location = new Point(16, buttonY + buttonHeight + 14);

            LayoutRotationControls();
        }

        public enum LeftPanelTab
        {
            Tile,
            Text,
            StoredPositions
        }

        private Panel panelLeftTabs;
        private Button btnTabTile;
        private Button btnTabTileText;
        private Button btnTabPositions;
        private LeftPanelTab currentLeftTab = LeftPanelTab.Text;

        public void SelectLeftTab(LeftPanelTab tab)
        {
            currentLeftTab = tab;

            if (groupBox3 != null)
            {
                groupBox3.Visible = (tab == LeftPanelTab.Tile);
                if (groupBox3.Visible) groupBox3.BringToFront();
            }
            if (groupBox1 != null)
            {
                groupBox1.Visible = (tab == LeftPanelTab.Text);
                if (groupBox1.Visible) groupBox1.BringToFront();
            }
            if (groupBox4 != null)
            {
                groupBox4.Visible = (tab == LeftPanelTab.StoredPositions);
                if (groupBox4.Visible) groupBox4.BringToFront();
            }

            UpdateLeftTabStyles();
            LayoutMainFormContainers();
        }

        public void ShowTextTab()
        {
            SelectLeftTab(LeftPanelTab.Text);
        }

        private void UpdateLeftTabStyles()
        {
            Color activeBg = Color.White;
            Color activeBorder = Color.FromArgb(0, 120, 215);
            Color inactiveBg = Color.FromArgb(235, 238, 242);
            Color inactiveBorder = Color.FromArgb(195, 200, 208);

            UpdateButtonTab(btnTabTile, currentLeftTab == LeftPanelTab.Tile, activeBg, activeBorder, inactiveBg, inactiveBorder);
            UpdateButtonTab(btnTabTileText, currentLeftTab == LeftPanelTab.Text, activeBg, activeBorder, inactiveBg, inactiveBorder);
            UpdateButtonTab(btnTabPositions, currentLeftTab == LeftPanelTab.StoredPositions, activeBg, activeBorder, inactiveBg, inactiveBorder);
        }

        private void UpdateButtonTab(Button btn, bool isActive, Color activeBg, Color activeBorder, Color inactiveBg, Color inactiveBorder)
        {
            if (btn == null) return;
            btn.BackColor = isActive ? activeBg : inactiveBg;
            btn.ForeColor = isActive ? Color.FromArgb(0, 90, 180) : Color.FromArgb(60, 64, 70);
            btn.Font = new Font(this.Font.FontFamily, 9.25f, isActive ? FontStyle.Bold : FontStyle.Regular);
            btn.FlatAppearance.BorderColor = isActive ? activeBorder : inactiveBorder;
            btn.FlatAppearance.BorderSize = isActive ? 2 : 1;
        }

        public void UpdateStoredPositionsTabBadge()
        {
            if (btnTabPositions != null)
            {
                int count = (PositionDisplay != null) ? PositionDisplay.Count : 0;
                btnTabPositions.Text = count > 0 ? $"Positions ({count})" : "Positions";
            }
        }

        private void InitializeLeftPanelTabs()
        {
            panelLeftTabs = new Panel
            {
                Name = "panelLeftTabs",
                Location = new Point(7, toolStrip1.Bottom + 1),
                Size = new Size(390, 32),
                BackColor = SystemColors.Control
            };

            Font tabFont = new Font(this.Font.FontFamily, 9.25f, FontStyle.Regular);

            btnTabTile = new Button
            {
                Name = "btnTabTile",
                Text = "Tile Tools",
                Font = tabFont,
                Location = new Point(0, 0),
                Size = new Size(126, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTabTile.FlatAppearance.BorderSize = 1;
            btnTabTile.Click += (s, e) => SelectLeftTab(LeftPanelTab.Tile);

            btnTabTileText = new Button
            {
                Name = "btnTabTileText",
                Text = "Text Tools",
                Font = tabFont,
                Location = new Point(btnTabTile.Right + 3, 0),
                Size = new Size(126, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTabTileText.FlatAppearance.BorderSize = 1;
            btnTabTileText.Click += (s, e) => SelectLeftTab(LeftPanelTab.Text);

            btnTabPositions = new Button
            {
                Name = "btnTabPositions",
                Text = "Positions",
                Font = tabFont,
                Location = new Point(btnTabTileText.Right + 3, 0),
                Size = new Size(panelLeftTabs.Width - btnTabTileText.Right - 3, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTabPositions.FlatAppearance.BorderSize = 1;
            btnTabPositions.Click += (s, e) => SelectLeftTab(LeftPanelTab.StoredPositions);

            panelLeftTabs.Controls.Add(btnTabTile);
            panelLeftTabs.Controls.Add(btnTabTileText);
            panelLeftTabs.Controls.Add(btnTabPositions);

            this.Controls.Add(panelLeftTabs);
            panelLeftTabs.BringToFront();

            if (panel3 != null)
            {
                panel3.ControlAdded += (s, e) =>
                {
                    if (e.Control is positionTextDisplay ptd)
                    {
                        ptd.btnRestore.Click += (btnSender, btnArgs) => ShowTextTab();
                        ptd.btnSelect.Click += (btnSender, btnArgs) => ShowTextTab();
                    }
                    UpdateStoredPositionsTabBadge();
                };
                panel3.ControlRemoved += (s, e) => UpdateStoredPositionsTabBadge();
            }

            SelectLeftTab(LeftPanelTab.Text);
        }

        private void LayoutMainFormContainers()
        {
            if (toolStrip1 == null || groupBox3 == null || groupBox2 == null || groupBox1 == null || groupBox4 == null)
            {
                return;
            }

            int flushTop = toolStrip1.Bottom + 1;

            if (panelLeftTabs != null)
            {
                panelLeftTabs.Top = flushTop;
                panelLeftTabs.Left = 7;
                panelLeftTabs.Width = 390;
            }

            int contentTop = (panelLeftTabs != null) ? panelLeftTabs.Bottom + 4 : flushTop;
            int availableHeight = Math.Max(200, (statusStrip1 != null ? statusStrip1.Top : ClientRectangle.Height) - contentTop - 8);

            // GroupBox3: Tile Tools (natural height 156)
            groupBox3.Location = new Point(7, contentTop);
            groupBox3.Width = 390;
            groupBox3.Height = 156;

            // GroupBox1: Text Tools (natural height 536, fits completely in availableHeight ~570px)
            groupBox1.Location = new Point(7, contentTop);
            groupBox1.Width = 390;
            groupBox1.Height = availableHeight;

            // GroupBox4: Stored Position Area (fills full available vertical height)
            groupBox4.Location = new Point(7, contentTop);
            groupBox4.Width = 390;
            groupBox4.Height = availableHeight;

            // Middle column: Working Tile Area
            groupBox2.Top = flushTop;

            if (vScrollBar1 != null)
            {
                vScrollBar1.Top = toolStrip1.Bottom;
                if (statusStrip1 != null)
                {
                    vScrollBar1.Height = Math.Max(50, statusStrip1.Top - toolStrip1.Bottom);
                }
            }

            if (panel1Viewport != null)
            {
                panel1Viewport.Top = toolStrip1.Bottom;
                if (statusStrip1 != null)
                {
                    panel1Viewport.Height = Math.Max(50, statusStrip1.Top - toolStrip1.Bottom);
                }
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            ApplyToolbarResolutionProfile();
            LayoutMainFormContainers();
            SyncvScrollBar1();
        }

        private void SyncvScrollBar1()
        {
            if (vScrollBar1 == null || panel1 == null || panel1Viewport == null)
            {
                return;
            }

            UpdateTilePanelBackground();

            int viewportHeight = panel1Viewport.Height;
            int totalHeight = 0;

            if (panel1.Controls.Count > 0)
            {
                totalHeight = panel1.Controls[panel1.Controls.Count - 1].Bottom;
            }

            if (totalHeight <= viewportHeight)
            {
                vScrollBar1.Enabled = false;
                vScrollBar1.Value = 0;
                vScrollBar1.Maximum = 0;
                panel1.Top = 0;
            }
            else
            {
                vScrollBar1.Enabled = true;
                vScrollBar1.Maximum = totalHeight;
                vScrollBar1.LargeChange = viewportHeight;
                vScrollBar1.SmallChange = 20;

                int maxScroll = totalHeight - viewportHeight;
                if (maxScroll < 0) maxScroll = 0;

                if (vScrollBar1.Value > maxScroll)
                {
                    vScrollBar1.Value = maxScroll;
                }

                panel1.Top = -vScrollBar1.Value;
            }
        }

        private void SyncvScrollBar2()
        {
            UpdateStoredPositionsTabBadge();

            if (vScrollBar2 == null || panel2 == null || panel3 == null)
            {
                return;
            }

            int viewportHeight = panel2.Height;
            int totalHeight = 0;

            if (panel3.Controls.Count > 0)
            {
                totalHeight = panel3.Controls[panel3.Controls.Count - 1].Bottom;
            }

            if (totalHeight <= viewportHeight)
            {
                vScrollBar2.Enabled = false;
                vScrollBar2.Value = 0;
                vScrollBar2.Maximum = 0;
                panel3.Top = 4;
            }
            else
            {
                vScrollBar2.Enabled = true;
                vScrollBar2.Maximum = totalHeight;
                vScrollBar2.LargeChange = viewportHeight;
                vScrollBar2.SmallChange = 20;

                int maxScroll = totalHeight - viewportHeight;
                if (maxScroll < 0) maxScroll = 0;

                if (vScrollBar2.Value > maxScroll)
                {
                    vScrollBar2.Value = maxScroll;
                }

                panel3.Top = -vScrollBar2.Value + 4;
            }
        }

        private void ScrollToTile(int index)
        {
            if (vScrollBar1 == null || panel1 == null || panel1Viewport == null || index < 0 || index >= panel1.Controls.Count)
            {
                return;
            }

            int tileTop = panel1.Controls[index].Top;
            int tileBottom = panel1.Controls[index].Bottom;
            int viewportHeight = panel1Viewport.Height;
            int currentScroll = vScrollBar1.Value;

            if (tileTop < currentScroll)
            {
                currentScroll = tileTop;
            }
            else if (tileBottom > currentScroll + viewportHeight)
            {
                currentScroll = tileBottom - viewportHeight;
            }

            int maxScroll = vScrollBar1.Maximum - vScrollBar1.LargeChange + 1;
            if (maxScroll < 0) maxScroll = 0;
            if (currentScroll > maxScroll) currentScroll = maxScroll;
            if (currentScroll < 0) currentScroll = 0;

        }

        public void SetViewportLeft(int left)
        {
            if (panel1Viewport != null && vScrollBar1 != null)
            {
                panel1Viewport.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                panel1Viewport.Left = left;
                panel1Viewport.Width = vScrollBar1.Left - panel1Viewport.Left;
                panel1Viewport.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            }
        }

        private int currentToolbarIconSize = 0;

        private int GetOptimalToolbarIconSize()
        {
            Screen screen = Screen.FromControl(this);
            Rectangle bounds = (screen != null) ? screen.Bounds : Screen.PrimaryScreen.Bounds;

            // Detect 1366x768 and smaller displays (or window height <= 768)
            if (bounds.Width <= 1366 || bounds.Height <= 768 || (this.WindowState != FormWindowState.Minimized && this.Height <= 768))
            {
                return 20; // Compact icon size for 1366x768 and smaller displays
            }
            return 48; // Standard larger icon size for higher resolutions
        }

        private void ApplyToolbarResolutionProfile()
        {
            int iconSize = GetOptimalToolbarIconSize();
            if (iconSize == currentToolbarIconSize)
            {
                return;
            }
            currentToolbarIconSize = iconSize;
            ModernizeToolbarIcons(iconSize);
        }

        private void SetButtonImage(ToolStripButton btn, Bitmap newImg)
        {
            if (btn == null) return;
            Image old = btn.Image;
            btn.Image = newImg;
            if (old != null && old != newImg)
            {
                old.Dispose();
            }
        }

        private void SetMenuItemImage(ToolStripMenuItem mi, Bitmap newImg)
        {
            if (mi == null) return;
            Image old = mi.Image;
            mi.Image = newImg;
            if (old != null && old != newImg)
            {
                old.Dispose();
            }
        }

        private void ModernizeToolbarIcons(int iconSize)
        {
            bool isCompact = (iconSize <= 24);

            toolStrip1.ImageScalingSize = new Size(iconSize, iconSize);

            if (isCompact)
            {
                toolStrip1.AutoSize = false;
                toolStrip1.Height = 30;
                toolStrip1.Padding = new Padding(2, 0, 1, 0);
            }
            else
            {
                toolStrip1.AutoSize = true;
                toolStrip1.Padding = new Padding(0);
            }

            foreach (ToolStripItem item in toolStrip1.Items)
            {
                if (isCompact)
                {
                    if (item is ToolStripButton)
                    {
                        item.Padding = new Padding(1);
                        item.Margin = new Padding(1, 1, 1, 1);
                    }
                    else if (item is ToolStripSeparator)
                    {
                        item.Margin = new Padding(2, 2, 2, 2);
                    }
                }
                else
                {
                    if (item is ToolStripButton)
                    {
                        item.Padding = new Padding(2);
                        item.Margin = new Padding(1, 2, 1, 2);
                    }
                }
            }

            if (toolFontNameButton != null)
            {
                toolFontNameButton.Size = isCompact ? new Size(160, 23) : new Size(200, 25);
            }
            if (toolFontSizeButton != null)
            {
                toolFontSizeButton.Size = isCompact ? new Size(36, 21) : new Size(40, 23);
            }

            imageList1.Images.Clear();
            imageList1.ImageSize = new Size(iconSize, iconSize);
            
            imageList1.Images.Add(GetModernGlyph("\uE7AD", iconSize, Color.Black)); // Index 0: ROTATE_T
            imageList1.Images.Add(GetModernGlyph("\uE7AD", iconSize, Color.Gray));  // Index 1: ROTATE_DISABLED
            imageList1.Images.Add(GetModernGlyph("\uE10F", iconSize, Color.Black)); // Index 2: WriteText2
            imageList1.Images.Add(GetModernGlyph("\uE72E", iconSize, Color.Black)); // Index 3: lock_closed
            imageList1.Images.Add(GetModernGlyph("\uE785", iconSize, Color.Black)); // Index 4: lock_closedNO
            imageList1.Images.Add(GetLowDensityTileGlyph(iconSize, Color.Black)); // Index 5: CreateHalfFromFull
            imageList1.Images.Add(GetLowDensityTileGlyph(iconSize, Color.Gray));  // Index 6: CreateHalfFromFullNO
            imageList1.Images.Add(GetModernGlyph("\uE785", iconSize, Color.Black)); // Index 7: lock_open
            imageList1.Images.Add(GetModernGlyph("\uE814", iconSize, Color.Black)); // Index 8: AutoBevelBW
            imageList1.Images.Add(GetModernGlyph("\uE814", iconSize, Color.Gray));  // Index 9: AutoBevelBW-A
            imageList1.Images.Add(GetModernGlyph("\uE72C", iconSize, Color.Black)); // Index 10: AutoClipBW
            imageList1.Images.Add(GetModernGlyph("\uE72C", iconSize, Color.Gray));  // Index 11: AutoClipBW-A
            imageList1.Images.Add(GetModernGlyph("\uEA39", iconSize, Color.Gray));  // Index 12: AntiAlias-Off
            imageList1.Images.Add(GetModernGlyph("\uEA39", iconSize, Color.DodgerBlue)); // Index 13: antialias-on

            SetButtonImage(openToolStripButton, GetModernGlyph("\uED25", iconSize, Color.Black));
            SetButtonImage(saveToolStripButton, GetModernGlyph("\uE74E", iconSize, Color.Black));
            SetButtonImage(copyToolStripButton, GetModernGlyph("\uE8C8", iconSize, Color.Black));
            SetButtonImage(pasteToolStripButton, GetModernGlyph("\uE77F", iconSize, Color.Black));
            
            SetButtonImage(toolFontSelectButton, GetFontDownloadGlyph(iconSize, Color.FromArgb(0x1F, 0x1F, 0x1F)));
            SetButtonImage(fontColortoolStripButton1, GetFormatColorTextGlyph(iconSize, Color.Black, Color.FromArgb(220, 50, 50)));
            SetButtonImage(toolFontIncreaseButton, GetTextIncreaseGlyph(iconSize, Color.Black));
            SetButtonImage(toolFontDecreaseButton, GetTextDecreaseGlyph(iconSize, Color.Black));
            SetButtonImage(toolFontBoldButton, GetFormatBoldGlyph(iconSize, Color.Black));
            SetButtonImage(toolFontItalicButton, GetFormatItalicGlyph(iconSize, Color.Black));
            SetButtonImage(toolAntiAliasButton1, GetModernGlyph("\uEA39", iconSize, Color.Gray));
            
            SetButtonImage(placeTexttoolStripButton, GetContextPasteGoGlyph(iconSize));
            SetMenuItemImage(placeTexttoolStripMenuItem, GetContextPasteGoGlyph(iconSize));
            SetButtonImage(storetoolStripButton, GetContextCopyGlyph(iconSize));
            SetMenuItemImage(StoreTexttoolStripMenuItem, GetContextCopyGlyph(iconSize));
            
            SetButtonImage(lockCreateHalffromFullStripButton1, GetLowDensityTileGlyph(iconSize, Color.Gray));
            SetButtonImage(createTiletoolStripButton, GetModernGlyph("\uE109", iconSize, Color.Black));
            SetButtonImage(createFromPositiontoolStripButton, GetModernGlyph("\uE8A5", iconSize, Color.Black));
            SetButtonImage(replaceTooltoolStripButton1, GetModernGlyph("\uE117", iconSize, Color.Black));
            
            SetButtonImage(lockRotationtoolStripButton1, GetModernGlyph("\uE72E", iconSize, Color.Black));
            SetButtonImage(toolRotateLeftButton, GetMaterialRotateLeftGlyph(iconSize, Color.Black));
            SetButtonImage(toolRotateRightButton, GetMaterialRotateRightGlyph(iconSize, Color.Black));
            
            SetButtonImage(lockFliptoolStripButton1, GetModernGlyph("\uE72E", iconSize, Color.Black));
            SetButtonImage(toolFlipHorizontal, GetArrowRangeGlyph(iconSize, Color.Black));
            SetButtonImage(toolFlipVertical, GetHeightGlyph(iconSize, Color.Black));
            
            SetButtonImage(fullTiletoolStripButton1, GetModernGlyph("\uF158", iconSize, Color.Black));
            SetButtonImage(halfTiletoolStripButton1, GetModernGlyph("\uF156", iconSize, Color.Black));
            SetButtonImage(smallTiletoolStripButton2, GetModernGlyph("\uF157", iconSize, Color.Black));
            SetButtonImage(deletetoolStripButton1, GetModernGlyph("\uE74D", iconSize, Color.Black));
        }

        //Constructor
        public Form1()                                                                                 
        {
            InitializeComponent();

            // Setup panel1Viewport as the scrollable container for panel1
            panel1Viewport = new Panel();
            panel1Viewport.Name = "panel1Viewport";
            panel1Viewport.BorderStyle = BorderStyle.FixedSingle;
            panel1Viewport.BackColor = SystemColors.Control;
            panel1Viewport.Location = panel1.Location;
            panel1Viewport.Size = panel1.Size;
            panel1Viewport.Anchor = panel1.Anchor;
            
            // Remove panel1 from Form, add panel1Viewport, add panel1 to panel1Viewport
            this.Controls.Remove(panel1);
            this.Controls.Add(panel1Viewport);
            panel1Viewport.Controls.Add(panel1);
            
            // Adjust panel1 to be the content panel inside the viewport
            panel1.Location = new Point(0, 0);
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel1.Width = panel1Viewport.ClientSize.Width;
            panel1.BackColor = SystemColors.Control;
            EnableDoubleBuffering(panel1Viewport);

            panel1.ControlAdded += (s, e) => UpdateTilePanelBackground();
            panel1.ControlRemoved += (s, e) => UpdateTilePanelBackground();

            // Synchronize panel2 (Stored Position Area viewport) and vScrollBar2
            vScrollBar2.Width = 24;
            vScrollBar2.BringToFront();
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            panel2.Width = vScrollBar2.Left - panel2.Left;
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            panel3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel3.AutoSize = false;
            panel3.Location = new Point(0, 4);
            panel3.Width = panel2.ClientSize.Width;

            TileDisplay = new tileDisplay(this, this.components, panel1);
            PositionDisplay = new positionDisplay(this, panel3, vScrollBar2, workingFullTile, workingHalfTile, workingTile);
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(mruRegKey);
            if (regKey != null)
            {
                regKey.Close();
            }
            mruMenu = new MruStripMenuInline(fileToolStripMenuItem, MRUToolStripMenuItem, new MruStripMenu.ClickedHandler(OnMruFile), mruRegKey + "\\MRU",8);
            
            ApplyHigherContrastBorders();
            InitializeLeftPanelTabs();
            
            // Programmatically bind canvas interaction events
            workingTile.MouseDown += new MouseEventHandler(workingTile_MouseDown);
            workingTile.MouseMove += new MouseEventHandler(workingTile_MouseMove);
            workingTile.MouseUp += new MouseEventHandler(workingTile_MouseUp);
            workingTile.MouseDoubleClick += new MouseEventHandler(workingTile_MouseDoubleClick);
            this.Resize += new EventHandler(Form1_Resize);
            if (btnHideControls != null)
            {
                btnHideControls.VisibleChanged += (s, e) => LayoutTextWorkingArea();
            }
        }
        //Methods
        private void Form1_Load(object sender, EventArgs e)                                            
        {
            EnableDoubleBuffering(panel1);
            EnableDoubleBuffering(panel2);
            EnableDoubleBuffering(panel3);
            EnableDoubleBuffering(workingTile);

            ApplyToolbarResolutionProfile();
            this.PerformLayout();
            LayoutMainFormContainers();

            lockCreateHalffromFullStripButton1.Image = this.imageList1.Images[6];

            mruMenu.LoadFromRegistry();

            panel1Height = panel1.Height;

            FontFamily[] fontFamilies;
            InstalledFontCollection installedFontCollection = new InstalledFontCollection();
            fontFamilies = installedFontCollection.Families;

            this.vScrollBar1.Dock = DockStyle.None;
            this.vScrollBar1.Width = 30;
            this.vScrollBar1.Top = this.toolStrip1.Bottom;
            this.vScrollBar1.Height = this.statusStrip1.Top - this.toolStrip1.Bottom;
            this.vScrollBar1.Left = this.ClientRectangle.Width - this.vScrollBar1.Width;
            this.vScrollBar1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            this.vScrollBar1.BringToFront();

            this.panel1Viewport.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.panel1Viewport.Top = this.toolStrip1.Bottom;
            this.panel1Viewport.Height = this.statusStrip1.Top - this.toolStrip1.Bottom;
            this.panel1Viewport.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            
            SetViewportLeft(760);

            this.panel1.Top = 0;
            panelHeight = this.panel1Viewport.Height;

            foreach (FontFamily fontFamily in fontFamilies)
            {
                toolFontNameButton.Items.Add(fontFamily.Name);
            }
            toolFontNameButton.Text = this.Font.Name;
            toolFontSizeButton.Text = this.Font.Size.ToString();

            // Run programmatic modern dynamic layouts
            LayoutTileWorkingArea();
            LayoutTextWorkingArea();

            // Render sharp rotation presets on appropriately scaled buttons
            RenderRotationButtonImage(btnRotate0, "0", 0.0f);
            RenderRotationButtonImage(btnRotate90, "90", 90.0f);
            RenderRotationButtonImage(btnRotate180, "180", 180.0f);
            RenderRotationButtonImage(btnRotateminus90, "270", 270.0f);

            SyncvScrollBar1();

            pctRotate.Image = imageList2.Images[1];

            var cmdArgs = Environment.GetCommandLineArgs();

            //MessageBox.Show(cmdArgs.Length.ToString());

            if (cmdArgs.Length > 1)
            {
                cmdArgs[0] = "";
                currentfilename = String.Join(" ", cmdArgs);
                openFile(currentfilename);

            }
        }
        private void openFile(string filename)                                                         
        {
            if (filename == "")
            {
                openFileDialog1.FileName = "";
                openFileDialog1.Title = "Open GTL file...";
                openFileDialog1.Filter = "GTL files (*.gtl)|*.gtl";

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    openfile(openFileDialog1.FileName);
                }
                currentfilename = openFileDialog1.FileName;
            }
            else
            {
                openFileDialog1.FileName = filename;
                openfile(filename);
            }
        }
        private void openfile(string filename)                                                         
        {
            mruMenu.AddFile(filename);
            mruMenu.SaveToRegistry();

            Application.UseWaitCursor = true;

            pctZoom.Visible = false;

            txtText.BackColor = Color.FromName("Window");

            this.workingTile.Visible = false;
            this.workingFullTile.Visible = false;
            this.workingHalfTile.Visible = false;
            this.workingSmallTile.Visible = false;

            btnBumpUp.Visible = false;
            btnBumpRight.Visible = false;
            btnBumpDown.Visible = false;
            btnBumpLeft.Visible = false;

            Application.DoEvents();
            TileDisplay.RemoveAllTiles();

            panel1.Height = panel1Height;

            
            //gtlFile.ReadFile(openFileDialog1.FileName);
            gtlFile.ReadFile(filename);

            this.Text = "GTL Editor";

            currentWorkingTile = (int)currentworkingtile.FullTile;
            Application.DoEvents();
            try
            {
                workingTile.Image = gtlFile.Tile[0].FullTile;
                workingTile.Width = gtlFile.Tile[0].FullTile.Width;
                workingTile.Height = gtlFile.Tile[0].FullTile.Height;
                workingTile.Top = Convert.ToInt32((groupBox2.ClientSize.Height * 0.5) - (workingTile.Height * 0.5));
                workingTile.Left = Convert.ToInt32((groupBox2.ClientSize.Width * 0.5) - (workingTile.Width * 0.5));

                this.Text = "GTL Editor - [" + Path.GetFileName(filename) + "]";

                workingFullTile.Top = groupBox2.Bottom + 20;
                workingFullTile.Height = gtlFile.Tile[0].FullTile.Height;
                workingFullTile.Width = gtlFile.Tile[0].FullTile.Width;
                workingFullTile.Image = gtlFile.Tile[0].FullTile;

                workingHalfTile.Height = gtlFile.Tile[0].HalfTile.Height;
                workingHalfTile.Width = gtlFile.Tile[0].HalfTile.Width;
                workingHalfTile.Image = gtlFile.Tile[0].HalfTile;

                workingSmallTile.BackColor = gtlFile.Tile[0].SmallTile;

                workingHalfTile.Top = workingFullTile.Bottom + 20;
                if (lockCreateHalffromFullStripButton1.CheckState == CheckState.Checked)
                {
                    workingHalfTile.Height = gtlFile.Tile[0].HalfTile.Height;
                    workingHalfTile.Width = gtlFile.Tile[0].HalfTile.Width;
                    workingHalfTile.Image = gtlFile.Tile[0].HalfTile;
                }
                else
                {
                    workingHalfTile.Width = (workingFullTile.Width / 2) + 1;
                    workingHalfTile.Height = (workingFullTile.Height / 2) + 1;
                    workingHalfTile.Image = FixedSize(workingFullTile.Image, workingHalfTile.Width, workingHalfTile.Height);
                }

                workingSmallTile.Top = workingHalfTile.Bottom + 20;

                if (panel1.Controls.Count > 0)
                    panel1.Controls.Clear();

                for (int i = 0; i < gtlFile.TileCount; i++)
                {
                    TileDisplay.AddTile(gtlFile.Tile[i]);
                }

                this.txtWidth.Text = workingTile.Width.ToString();
                this.txtHeight.Text = workingTile.Height.ToString();
                this.lblTileSize.Text = workingFullTile.Width.ToString() + "," + workingFullTile.Height.ToString();
                this.vScrollText.Maximum = workingFullTile.Width;
                this.hScrollText.Maximum = workingFullTile.Height;
                
                this.vScrollBar1.Value = 0;
                SyncvScrollBar1();

                UpdateWorkingTileGraphics(workingTile.Image);

                groupBox1.Enabled = true;
                groupBox2.Enabled = true;
                groupBox3.Enabled = true;
                groupBox4.Enabled = true;

                for (int i = 1; i < toolStrip1.Items.Count; i++)
                {
                    toolStrip1.Items[i].Enabled = true;
                }

                saveToolStripMenuItem.Enabled = true;
                saveAsToolStripMenuItem.Enabled = true;

                openPositiontoolStripMenuItem.Enabled = true;
                savePositiontoolStripMenuItem.Enabled = true;
                closeFileToolStripMenuItem.Enabled = true;
                createPNGtoolStripMenuItem.Enabled = true;

                for (int i = 0; i < ((ToolStripDropDownItem)editToolStripMenuItem).DropDownItems.Count; i++)
                {
                    try
                    {
                        ((ToolStripDropDownItem)editToolStripMenuItem.DropDownItems[i]).Enabled = true;
                    }
                    catch
                    {
                    }
                }
                for (int i = 0; i < ((ToolStripDropDownItem)texttoolStripMenuItem).DropDownItems.Count; i++)
                {
                    try
                    {
                        ((ToolStripDropDownItem)texttoolStripMenuItem.DropDownItems[i]).Enabled = true;
                    }
                    catch
                    {
                    }
                }
                for (int i = 0; i < ((ToolStripDropDownItem)tiletoolStripMenuItem).DropDownItems.Count; i++)
                {
                    try
                    {
                        ((ToolStripDropDownItem)tiletoolStripMenuItem.DropDownItems[i]).Enabled = true;
                    }
                    catch
                    {
                    }
                }

                if (TileDisplay.Count > 1)
                {
                    deleteTileToolStripMenuItem.Enabled = true;
                    deletetoolStripButton1.Enabled = true;
                }
                else
                {
                    deleteTileToolStripMenuItem.Enabled = false;
                    deletetoolStripButton1.Enabled = false;
                }

                openedFile = openFileDialog1.FileName;

                if (TileDisplay.Count == 1)
                {
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tile";
                }
                else
                {
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tiles";
                }

                pctRotate.Image = imageList2.Images[0];

                //RotateImage = pctRotate.Image;
                //pctRotate.Image = (Image)RotateImage.Clone();
                //UpdateRotateTextGraphics(pctRotate.Image);
                //gRotateText.TranslateTransform(50.0f, 35.0f);
                //gRotateText.DrawString("0", lblTextPlacement.Font, Brushes.Black, 0.0f, 0.0f);

                txtText.BackColor = workingSmallTile.BackColor;
                pctTransparentColor.BackColor = Color.Cyan;

                btnBumpUp.Top = workingTile.Top - btnBumpUp.Height - 5;
                btnBumpUp.Left = workingTile.Left + (workingTile.Width / 2) - (btnBumpUp.Width / 2);
                btnBumpUp.Visible = true;

                btnBumpDown.Top = workingTile.Bottom + 5;
                btnBumpDown.Left = btnBumpUp.Left;
                btnBumpDown.Visible = true;

                btnBumpRight.Top = workingTile.Top + (workingTile.Height / 2) - (btnBumpRight.Height / 2);
                btnBumpRight.Left = workingTile.Right + 5;
                btnBumpRight.Visible = true;

                btnBumpLeft.Top = btnBumpRight.Top;
                btnBumpLeft.Left = workingTile.Left - 5 - btnBumpLeft.Width;
                btnBumpLeft.Visible = true;
                TileDisplay.CurrentTile = 0;

                resizeWorkingArea();
                centerWorkingTile();

                workingTile.Visible = true;
                this.workingFullTile.Visible = true;
                this.workingHalfTile.Visible = true;
                this.workingSmallTile.Visible = true;
            }
            catch
            {
                panel1.Controls.Clear();
                UpdateTilePanelBackground();

                groupBox1.Enabled = false;
                groupBox2.Enabled = false;
                groupBox3.Enabled = false;
                groupBox4.Enabled = false;

                for (int i = 1; i < toolStrip1.Items.Count; i++)
                {
                    toolStrip1.Items[i].Enabled = false;
                }

                saveToolStripMenuItem.Enabled = false;
                saveAsToolStripMenuItem.Enabled = false;

                openPositiontoolStripMenuItem.Enabled = false;
                savePositiontoolStripMenuItem.Enabled = false;
                closeFileToolStripMenuItem.Enabled = false;
                createPNGtoolStripMenuItem.Enabled = false;

                MessageBox.Show("Wrong GLT Tile Library Version", "Danger, Will Robinson!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            paintZoom();
            pctZoom.Visible = true;
            Application.UseWaitCursor = false;
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)                           
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            openFile("");
        }
        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)                              
        {
            try
            {
                int val = vScrollBar1.Value;
                int maxScroll = vScrollBar1.Maximum - vScrollBar1.LargeChange + 1;
                if (maxScroll < 0) maxScroll = 0;
                if (val > maxScroll) val = maxScroll;
                if (val < 0) val = 0;
                vScrollBar1.Value = val;

                this.panel1.Top = -val;
            }
            catch
            {
            }
        }
        private void openToolStripButton_Click(object sender, EventArgs e)                             
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "File Tools");
            }
            else
            {
                openFile("");
            }
        }
        private void toolFontSelectButton_Click(object sender, EventArgs e)                            
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Font Tools");
            }
            else
            {
                fontDialog1.ShowColor = true;

                fontDialog1.Font = txtText.Font;
                fontDialog1.Color = txtText.ForeColor;

                if (fontDialog1.ShowDialog() != DialogResult.Cancel)
                {
                    txtText.Font = fontDialog1.Font;
                    txtText.ForeColor = fontDialog1.Color;

                    this.toolFontNameButton.Text = fontDialog1.Font.Name;
                    this.toolFontSizeButton.Text = fontDialog1.Font.Size.ToString();
                    toolFontBoldButton.Checked = fontDialog1.Font.Bold;
                    toolFontItalicButton.Checked = fontDialog1.Font.Italic;
                }
            }
        }
        private void toolFontBoldButton_Click(object sender, EventArgs e)                              
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Font Tools");
            }
            else
            {
                if (toolFontBoldButton.Checked)
                {
                    txtText.Font = new Font(txtText.Font, txtText.Font.Style | FontStyle.Bold);

                }
                else
                {
                    txtText.Font = new Font(txtText.Font, txtText.Font.Style ^ FontStyle.Bold);
                }
                drawText();
            }
        }
        private void toolFontItalicButton_Click(object sender, EventArgs e)                            
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Font Tools");
            }
            else
            {
                if (toolFontItalicButton.Checked)
                {
                    txtText.Font = new Font(txtText.Font, txtText.Font.Style | FontStyle.Italic);
                }
                else
                {
                    txtText.Font = new Font(txtText.Font, txtText.Font.Style ^ FontStyle.Italic);
                }
                drawText();
            }
        }
        private void btnPlace_Click(object sender, EventArgs e)                                        
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Place Text Button");
            }
            else
            {
                drawText();
                workingTile.Focus();
            }
        }
        private void btnReplace_Click(object sender, EventArgs e)                                      
        {
            int currentPosition = Convert.ToInt32(btnReplace.Tag);
            
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Store Text Button");
            }
            else
            {
                PositionDisplay[currentPosition].txtPositionText.Text = txtText.Text;
                PositionDisplay[currentPosition].txtPositionText.ForeColor = txtText.ForeColor;
                PositionDisplay[currentPosition].txtPositionText.Font = txtText.Font;

                PositionDisplay[currentPosition].lblPositionFont.Text = txtText.Font.Name;


                PositionDisplay[currentPosition].txtPositionText.Height = txtText.Height;
                PositionDisplay[currentPosition].txtPositionText.Text = txtText.Text;
                PositionDisplay[currentPosition].txtPositionText.ForeColor = txtText.ForeColor;
                PositionDisplay[currentPosition].txtPositionText.BackColor = txtText.BackColor;
                PositionDisplay[currentPosition].txtPositionText.Font = txtText.Font;

                PositionDisplay[currentPosition].lblPositionFont.Text = txtText.Font.Name;

                PositionDisplay[currentPosition].lblPositionFontSize.Text = txtText.Font.Size.ToString();

                Point newPoint = new Point(hScrollText.Value, vScrollText.Value);
                PositionDisplay[currentPosition].PointPlacement = newPoint;
                PositionDisplay[currentPosition].btnPositionPlace.Text = newPoint.X.ToString() + "," + newPoint.Y.ToString();

                if (chkIncrement.Checked == true)
                {
                    PositionDisplay[currentPosition].txtPositionIncrement.Visible = true;
                    PositionDisplay[currentPosition].txtPositionIncrement.Text = "1";
                    PositionDisplay[currentPosition].txtPositionText.Width = PositionDisplay[currentPosition].txtPositionText.Width - PositionDisplay[currentPosition].txtPositionIncrement.Width - 2;
                    PositionDisplay[currentPosition].txtPositionText.Left = PositionDisplay[currentPosition].txtPositionIncrement.Right + 2;
                }

                PositionDisplay[currentPosition].RotationDegrees = Convert.ToInt32(txtRotate.Text);
                PositionDisplay[currentPosition].lblRotation.Text = Convert.ToInt32(txtRotate.Text).ToString();


                chkIncrement.Checked = false;
            }
            btnReplace.Enabled = false;
            workingTile.Focus();
        }
        private void hScrollText_Scroll(object sender, ScrollEventArgs e)                              
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Horizontal Text Scroll Bar");
            }
            else
            {
                try
                {
                    this.lblTextPlacement.Text = this.hScrollText.Value.ToString() + "," + this.vScrollText.Value.ToString();
                    drawText();
                }
                catch
                {
                }
            }
        }
        private void vScrollText_Scroll(object sender, ScrollEventArgs e)                              
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Vertical Text Scroll Bar");
            }
            else
            {
                try
                {
                    this.lblTextPlacement.Text = this.hScrollText.Value.ToString() + "," + this.vScrollText.Value.ToString();
                    drawText();
                }
                catch
                {
                }
            }

        }
        private void toolFontIncreaseButton_Click(object sender, EventArgs e)                          
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Font Tools");
            }
            else
            {
                float currentSize;
                currentSize = txtText.Font.Size;
                currentSize += 0.5F;
                txtText.Visible = true;
                txtText.Font = new Font(txtText.Font.Name, currentSize, txtText.Font.Style, txtText.Font.Unit);
                this.toolFontSizeButton.Text = txtText.Font.Size.ToString();
                drawText();
            }
        }
        private void toolFontDecreaseButton_Click(object sender, EventArgs e)                          
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Font Tools");
            }
            else
            {
                float currentSize;
                currentSize = txtText.Font.Size;
                currentSize -= 0.5F;
                txtText.Visible = true;
                txtText.Font = new Font(txtText.Font.Name, currentSize, txtText.Font.Style, txtText.Font.Unit);
                this.toolFontSizeButton.Text = txtText.Font.Size.ToString();
                drawText();
            }
        }
        private void workingFullTile_Click(object sender, EventArgs e)                                 
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Full Size Tile");
            }
            else
            {
                workingTile.Width = workingFullTile.Width;
                workingTile.Height = workingFullTile.Height;
                workingTile.Image = workingFullTile.Image;
                
                workingTile.Invalidate();
                workingTile.Update();
                UpdateWorkingTileGraphics(workingTile.Image);
                currentWorkingTile = (int)currentworkingtile.FullTile;
                lblCurrentWorkingTile.Text = currentworkingtile.FullTile.ToString();
                resizeWorkingArea();
                centerWorkingTile();
                //resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void workingHalfTile_Click(object sender, EventArgs e)                                 
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Full Size Tile");
            }
            else
            {
                workingTile.Width = workingHalfTile.Width;
                workingTile.Height = workingHalfTile.Height;
                workingTile.Image = workingHalfTile.Image;
                centerWorkingTile();
                workingTile.Invalidate();
                workingTile.Update();
                UpdateWorkingTileGraphics(workingTile.Image);
                currentWorkingTile = (int)currentworkingtile.HalfTile;
                lblCurrentWorkingTile.Text = currentworkingtile.HalfTile.ToString();
                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void toolFontSizeButton_TextChanged(object sender, EventArgs e)                        
        {
            try
            {
                txtText.Font = new Font(txtText.Font.Name, Convert.ToSingle(toolFontSizeButton.Text), txtText.Font.Style, txtText.Font.Unit);
                if (txtText.Height > 20)
                {
                    btnHideControls.Visible = true;
                }
                else
                {
                    btnHideControls.Visible = false;
                    txtText.Visible = true;
                }
            }
            catch
            {

            }
            try
            {
                drawText();
            }
            catch
            {

            }
        }
        private void toolFontSizeButton_KeyDown(object sender, KeyEventArgs e)                         
        {
            nonNumberEntered = false;
            if (e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9) 
            {
                if (e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9)
                {
                    if (e.KeyCode != Keys.Back)
                    {
                        nonNumberEntered = true;
                    }
                }
            }
        }
        private void toolFontSizeButton_KeyPress(object sender, KeyPressEventArgs e)                   
        {
            if (nonNumberEntered == true)
            {
                e.Handled = true;
            }
        }
        private void btnHideControls_Click(object sender, EventArgs e)                                 
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Hide/Show Button");
            }
            else
            {
                if (this.txtText.Visible)
                {
                    txtText.Visible = false;
                    btnHideControls.Text = "Show";
                }
                else
                {
                    txtText.Visible = true;
                    btnHideControls.Text = "Hide";
                }
                workingTile.Focus();
            }
        }
        private void toolRotateLeftButton_Click(object sender, EventArgs e)                            
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                Image workerImage;
                workerImage = (Image)workingTile.Image.Clone();
                workerImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                workingTile.Width = workerImage.Width;
                workingTile.Height = workerImage.Height;
                workingTile.Image = (Image)workerImage.Clone();
                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
                if (lockRotationtoolStripButton1.Checked == false)
                {
                    workerImage = (Image)workingFullTile.Image.Clone();
                    workerImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    workingFullTile.Width = workerImage.Width;
                    workingFullTile.Height = workerImage.Height;
                    workingFullTile.Image = (Image)workerImage.Clone();

                    workerImage = (Image)workingHalfTile.Image.Clone();
                    workerImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    workingHalfTile.Width = workerImage.Width;
                    workingHalfTile.Height = workerImage.Height;
                    workingHalfTile.Image = (Image)workerImage.Clone();
                }
            }
        }
        private void toolRotateRightButton_Click(object sender, EventArgs e)                           
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                Image workerImage;
                workerImage = (Image)workingTile.Image.Clone();
                workerImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                workingTile.Width = workerImage.Width;
                workingTile.Height = workerImage.Height;
                workingTile.Image = (Image)workerImage.Clone();
                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
                if (lockRotationtoolStripButton1.Checked == false)
                {
                    workerImage = (Image)workingFullTile.Image.Clone();
                    workerImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    workingFullTile.Width = workerImage.Width;
                    workingFullTile.Height = workerImage.Height;
                    workingFullTile.Image = (Image)workerImage.Clone();

                    workerImage = (Image)workingHalfTile.Image.Clone();
                    workerImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    workingHalfTile.Width = workerImage.Width;
                    workingHalfTile.Height = workerImage.Height;
                    workingHalfTile.Image = (Image)workerImage.Clone();
                }
            }
        }
        private void toolFlipHorizontal_Click(object sender, EventArgs e)                              
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                Image workerImage;
                workerImage = (Image)workingTile.Image.Clone();
                workerImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                workingTile.Width = workerImage.Width;
                workingTile.Height = workerImage.Height;
                workingTile.Image = (Image)workerImage.Clone();
                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
                if (lockFliptoolStripButton1.Checked == false)
                {
                    workerImage = (Image)workingFullTile.Image.Clone();
                    workerImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    workingFullTile.Width = workerImage.Width;
                    workingFullTile.Height = workerImage.Height;
                    workingFullTile.Image = (Image)workerImage.Clone();

                    workerImage = (Image)workingHalfTile.Image.Clone();
                    workerImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    workingHalfTile.Width = workerImage.Width;
                    workingHalfTile.Height = workerImage.Height;
                    workingHalfTile.Image = (Image)workerImage.Clone();
                }
            }
        }
        private void toolFlipVertical_Click(object sender, EventArgs e)                                
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                Image workerImage;
                workerImage = (Image)workingTile.Image.Clone();
                workerImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
                workingTile.Width = workerImage.Width;
                workingTile.Height = workerImage.Height;
                workingTile.Image = (Image)workerImage.Clone();
                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
                if (lockFliptoolStripButton1.Checked == false)
                {
                    workerImage = (Image)workingFullTile.Image.Clone();
                    workerImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    workingFullTile.Width = workerImage.Width;
                    workingFullTile.Height = workerImage.Height;
                    workingFullTile.Image = (Image)workerImage.Clone();

                    workerImage = (Image)workingHalfTile.Image.Clone();
                    workerImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    workingHalfTile.Width = workerImage.Width;
                    workingHalfTile.Height = workerImage.Height;
                    workingHalfTile.Image = (Image)workerImage.Clone();
                }
            }
        }
        private void hScrollRotate_Scroll(object sender, ScrollEventArgs e)                            
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Rotation Scroll Bar and Display");
            }
            else
            {
                try
                {
                    txtRotate.Text = hScrollRotate.Value.ToString();
                    drawRotateAngle();
                    drawText();
                }
                catch
                {

                }
            }
        }
        private void drawRotateAngle()                                                                 
        {
            try
            {
                pctRotate.Image = (Image)RotateImage.Clone();
                UpdateRotateTextGraphics(pctRotate.Image);
                gRotateText.TranslateTransform(50.0f, 35.0f);
                gRotateText.RotateTransform(hScrollRotate.Value);
                gRotateText.DrawString(hScrollRotate.Value.ToString(), lblTextPlacement.Font, Brushes.Black, 0.0f, 0.0f);
                gRotateText.DrawString(hScrollRotate.Value.ToString(), lblTextPlacement.Font, Brushes.Black, 0.0f, 0.0f);
                //gRotateText.ResetTransform();
                //pctRotate.Invalidate();
                //pctRotate.Update();
                if (pctZoom.Visible)
                    paintZoom();
            }
            catch
            {

            }
        }
        public void drawText()                                                                         
        {
            if (lblCurrentWorkingTile == null || 
                workingFullTile?.Image == null || 
                workingHalfTile?.Image == null || 
                txtText == null || 
                hScrollText == null || 
                vScrollText == null || 
                toolAntiAliasButton1 == null || 
                hScrollRotate == null || 
                workingTile == null)
            {
                return;
            }
            Image workerTile;
            workerTile = null;
            SolidBrush drawBrush = new SolidBrush(txtText.ForeColor);
            if (lblCurrentWorkingTile.Text == currentworkingtile.FullTile.ToString())
            {
                workerTile = (Image)workingFullTile.Image.Clone();
                UpdateWorkingTileGraphics(workerTile);
            }
            else
            {
                workerTile = (Image)workingHalfTile.Image.Clone();
                UpdateWorkingTileGraphics(workerTile);
            }
            gWorkingTile.TranslateTransform(Convert.ToSingle(hScrollText.Value), Convert.ToSingle(vScrollText.Value));

            if (toolAntiAliasButton1.Checked)
                gWorkingTile.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;
            else
                gWorkingTile.TextRenderingHint = TextRenderingHint.AntiAlias;

            gWorkingTile.RotateTransform(Convert.ToSingle(hScrollRotate.Value));
            gWorkingTile.DrawString(txtText.Text, txtText.Font, drawBrush, 0.0f, 0.0f);
            gWorkingTile.DrawString(txtText.Text, txtText.Font, drawBrush, 0.0f, 0.0f);
            gWorkingTile.ResetTransform();

            workingTile.Image = workerTile;
            workingTile.Invalidate();
            workingTile.Update();
            if (pctZoom != null && pctZoom.Visible)
                paintZoom();
        }
        private void resizeWorkingArea()                                                               
        {
            int intTempWidth;
                if (groupBox2.Controls["workingTile"].Width + 24 <= groupBox2.Width)
                {
                    if (groupBox2.Controls["workingTile"].Width < workingFullTile.Width)
                    {
                        intTempWidth = workingFullTile.Width + 16;

                        if (intTempWidth < 350)
                        {
                            groupBox2.Width = 350;
                        }
                        else
                        {
                            groupBox2.Width = intTempWidth;
                        }
                    }
                    else
                    {
                        groupBox2.Width = 350;
                    }
                    groupBox2.Controls["workingTile"].Left = Convert.ToInt32(groupBox2.Width * 0.5
                                                           - groupBox2.Controls["workingTile"].Width * 0.5);
                }
                else
                {
                    groupBox2.Controls["workingTile"].Left = 8;
                    groupBox2.Width = groupBox2.Controls["workingTile"].Width + 16;
                }
                if (groupBox2.Controls["workingTile"].Height + 37 <= groupBox2.Height)
                {
                    groupBox2.Height = 381;
                    groupBox2.Controls["workingTile"].Top = Convert.ToInt32(groupBox2.Height * 0.5
                                                          - groupBox2.Controls["workingTile"].Height * 0.5);
                }
                else
                {
                    groupBox2.Controls["workingTile"].Top = 15;
                    groupBox2.Height = groupBox2.Controls["workingTile"].Height + 22;
                }
            if (groupBox2.Right + 6 > 760)
            {
                SetViewportLeft(groupBox2.Right + 6);
            }
            else
            {
                SetViewportLeft(760);
            }
            workingFullTile.Top = groupBox2.Bottom + 20;
            workingHalfTile.Top = workingFullTile.Bottom + 20;
            workingSmallTile.Top = workingHalfTile.Bottom + 20;
        }
        private void centerWorkingTile()                                                               
        {
            groupBox2.Controls["workingTile"].Left = Convert.ToInt32(groupBox2.Width * 0.5
                                                   - groupBox2.Controls["workingTile"].Width * 0.5);
            groupBox2.Controls["workingTile"].Top = Convert.ToInt32(groupBox2.Height * 0.5
                                                  - groupBox2.Controls["workingTile"].Height * 0.5);
            
            btnBumpUp.Top = workingTile.Top - btnBumpUp.Height - 5;
            btnBumpUp.Left = workingTile.Left + (workingTile.Width / 2) - (btnBumpUp.Width / 2);

            btnBumpDown.Top = workingTile.Bottom + 5;
            btnBumpDown.Left = btnBumpUp.Left;

            btnBumpRight.Top = workingTile.Top + (workingTile.Height / 2) - (btnBumpRight.Height / 2);
            btnBumpRight.Left = workingTile.Right + 5;

            btnBumpLeft.Top = btnBumpRight.Top;
            btnBumpLeft.Left = workingTile.Left - 5 - btnBumpLeft.Width;
        }
        private void btnRotate0_Click(object sender, EventArgs e)                                      
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Rotation Presets");
            }
            else
            {
                this.hScrollRotate.Value = 0;
                txtRotate.Text = "0";
                drawRotateAngle();
                drawText();
            }
            workingTile.Focus();
        }
        private void btnRotate90_Click(object sender, EventArgs e)                                     
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Rotation Presets");
            }
            else
            {
                this.hScrollRotate.Value = 90;
                txtRotate.Text = "90";
                drawRotateAngle();
                drawText();
            }
            workingTile.Focus();
        }
        private void btnRotateminus90_Click(object sender, EventArgs e)                                
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Rotation Presets");
            }
            else
            {
                this.hScrollRotate.Value = 270;
                txtRotate.Text = "270";
                drawRotateAngle();
                drawText();
            }
            workingTile.Focus();
        }
        private void btnRotate180_Click(object sender, EventArgs e)                                    
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Rotation Presets");
            }
            else
            {
                this.hScrollRotate.Value = 180;
                txtRotate.Text = "180";
                drawRotateAngle();
                drawText();
            }
            workingTile.Focus();
        }
        private void btnExtendRange_Click(object sender, EventArgs e)                                  
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Expand Text Range");
            }
            else
            {
                hScrollText.Minimum = hScrollText.Minimum - 10;
                hScrollText.Maximum = hScrollText.Maximum + 10;
                vScrollText.Minimum = vScrollText.Minimum - 10;
                vScrollText.Maximum = vScrollText.Maximum + 10;
                txtText.Focus();
            }
        }
        private void toolFontNameButton_SelectedIndexChanged(object sender, EventArgs e)               
        {
            FontFamily fontFamily;
            try
            {
                fontFamily = new FontFamily(toolFontNameButton.Text);
            }
            catch
            {
                fontFamily = new FontFamily(this.Font.Name);
            }
            try
            {
                txtText.Font = new Font(fontFamily, txtText.Font.Size, txtText.Font.Style);
                if (txtText.Height > 20)
                {
                    btnHideControls.Visible = true;
                }
                else
                {
                    btnHideControls.Visible = false;
                    txtText.Visible = true;
                }
            }
            catch
            {

            }
            try
            {
                drawText();
            }
            catch
            {

            }
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)                         
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            saveFileDialog1.FileName = "";
            saveFileDialog1.Title = "Save GTL file...";
            saveFileDialog1.Filter = "GTL files (*.gtl)|*.gtl";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                gtlFile.WriteFile(saveFileDialog1.FileName, panel1);
                this.Text = "GTL Editor - [" + Path.GetFileName(saveFileDialog1.FileName) + "]";

                mruMenu.AddFile(saveFileDialog1.FileName);
                mruMenu.SaveToRegistry();

                currentfilename = saveFileDialog1.FileName;
            }
            
        }
        private void saveToolStripButton_Click(object sender, EventArgs e)                             
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                if (Cursor.Current == Cursors.Help)
                {
                    this.Cursor = Cursors.Default;
                    Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "File Tools");
                }
                else
                {
                    saveGTLFile();
                }
            }
        }
        private void saveGTLFile()                                                                     
        {
            gtlFile.WriteFile(openedFile, panel1);
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)                           
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            saveGTLFile();
        }
        private void txtText_TextChanged(object sender, EventArgs e)                                   
        {
            this.toolFontNameButton.Text = txtText.Font.Name;
            this.toolFontSizeButton.Text = txtText.Font.Size.ToString();;
            drawText();
        }
        private void deletetoolStripButton1_Click(object sender, EventArgs e)                          
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {

                List<int> DeleteList = new List<int>();

                if (TileDisplay.Count > 1)
                {
                    for (int i = 0; i < panel1.Controls.Count; i++)
                    {
                        if (panel1.Controls[i].BackColor == Color.LightBlue)
                        {
                            DeleteList.Add(i);
                        }
                    }
                    

                    for (int i = DeleteList.Count - 1; i >= 0; i--)
                    {
                        TileDisplay.RemoveTile(DeleteList[i]);
                    }

                    panel1.Controls[0].Top = 0;
                    for (int i = 1; i < panel1.Controls.Count; i++)
                        panel1.Controls[i].Top = panel1.Controls[i - 1].Bottom;

                    int tempHeight;
                    tempHeight = panel1.Controls[panel1.Controls.Count - 1].Bottom;
                    if (tempHeight > panel1Height)
                        panel1.Height = panel1.Controls[panel1.Controls.Count - 1].Bottom;
                    else
                    {
                        panel1.Height = panel1Height;
                    }

                    SyncvScrollBar1();

                    if (TileDisplay.CurrentTile > 0)
                    {
                        if (TileDisplay.CurrentTile > panel1.Controls.Count - 1)
                        {
                            TileDisplay.CurrentTile = panel1.Controls.Count - 1;
                        }
                        try
                        {
                            panel1.Controls[TileDisplay.CurrentTile].BackColor = Color.LightBlue;
                        }
                        catch
                        {
                            panel1.Controls[panel1.Controls.Count - 1].BackColor = Color.LightBlue;
                        }
                    }
                    else
                    {
                        TileDisplay.CurrentTile = 0;
                        panel1.Controls[0].BackColor = Color.LightBlue;
                    }

                    int currenttile = 0;
                    for (int i = 0; i < panel1.Controls.Count; i++)
                    {
                        if (panel1.Controls[i].BackColor == Color.LightBlue)
                        {
                            currenttile = i;
                        }
                    }
                    ScrollToTile(currenttile);

                    workingTile.Image = ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls[2]).Image;
                    workingTile.Width = ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls[2]).Width;
                    workingTile.Height = ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls[2]).Height;

                    workingTile.Top = Convert.ToInt32((groupBox2.ClientSize.Height * 0.5) - (workingTile.Height * 0.5));
                    workingTile.Left = Convert.ToInt32((groupBox2.ClientSize.Width * 0.5) - (workingTile.Width * 0.5));

                    workingFullTile.Top = groupBox2.Bottom + 20;
                    workingFullTile.Image = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).fullTile.Image;
                    workingFullTile.Width = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).fullTile.Width;
                    workingFullTile.Height = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).fullTile.Height;

                    workingHalfTile.Image = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).halfTile.Image;
                    workingHalfTile.Width = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).halfTile.Width;
                    workingHalfTile.Height = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).halfTile.Height;

                    workingSmallTile.BackColor = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).smallTile.BackColor;

                    if (chkAutoBevel.CheckState == CheckState.Checked)
                    {
                        applyGradient(Convert.ToInt32(cboBorderSize.Text));
                    }

                    if (chkAutoClip.CheckState == CheckState.Checked)
                    {
                        applyCorners(Convert.ToInt32(cboCornerSize.Text));
                    }

                    workingHalfTile.Top = workingFullTile.Bottom + 20;
                    if (lockCreateHalffromFullStripButton1.CheckState == CheckState.Checked)
                    {
                        workingHalfTile.Height = gtlFile.Tile[TileDisplay.CurrentTile].HalfTile.Height;
                        workingHalfTile.Width = gtlFile.Tile[TileDisplay.CurrentTile].HalfTile.Width;
                        workingHalfTile.Image = gtlFile.Tile[TileDisplay.CurrentTile].HalfTile;
                    }
                    else
                    {
                        workingHalfTile.Width = (workingFullTile.Width / 2) + 1;
                        workingHalfTile.Height = (workingFullTile.Height / 2) + 1;
                        workingHalfTile.Image = FixedSize(workingFullTile.Image, workingHalfTile.Width, workingHalfTile.Height);
                    }

                    workingSmallTile.Top = workingHalfTile.Bottom + 20;
                }
                if (TileDisplay.Count == 1)
                {
                    deletetoolStripButton1.Enabled = false;
                    deleteTileToolStripMenuItem.Enabled = false;
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tile";
                }
                else
                {
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tiles";
                }
                txtText.BackColor = workingSmallTile.BackColor;
                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)                           
        {
            this.Close();
        }
        private void copyToolStripButton_Click(object sender, EventArgs e)                             
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Edit Tools");
            }
            else
            {
                Clipboard.SetImage(workingTile.Image);
            }
        }
        private void pasteToolStripButton_Click(object sender, EventArgs e)                            
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Edit Tools");
            }
            else
            {
                if (Clipboard.ContainsImage())
                {
                    Bitmap bmpClipboard;
                    bmpClipboard = (Bitmap)Clipboard.GetImage();
                    workingTile.Width = bmpClipboard.Width;
                    workingTile.Height = bmpClipboard.Height;
                    centerWorkingTile();
                    resizeWorkingArea();
                    if (pctZoom.Visible)
                        paintZoom();
                    workingTile.Image = bmpClipboard;
                }
            }
        }
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)                           
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            Clipboard.SetImage(workingTile.Image);
        }
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)                          
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            if (Clipboard.ContainsImage())
            {
                Bitmap bmpClipboard;
                bmpClipboard = (Bitmap)Clipboard.GetImage();
                workingTile.Width = bmpClipboard.Width;
                workingTile.Height = bmpClipboard.Height;
                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
                workingTile.Image = bmpClipboard;
            }
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)                          
        {
            About aboutForm = new About();
            aboutForm.Show();
        }
        private void fontColortoolStripButton1_Click(object sender, EventArgs e)                       
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Font Tools");
            }
            else
            {
                colorDialog1.Color = txtText.ForeColor;
                if (colorDialog1.ShowDialog() == DialogResult.OK)
                {
                    txtText.ForeColor = colorDialog1.Color;
                }
            }
        }
        private void txtText_ForeColorChanged(object sender, EventArgs e)                              
        {
            this.toolFontNameButton.Text = txtText.Font.Name;
            this.toolFontSizeButton.Text = txtText.Font.Size.ToString();
            drawText();
        }
        private void OnMruFile(int number, String filename)                                            
        {
            FileInfo fInfo = new FileInfo(filename);

            if (fInfo.Exists)
            {
                openFile(filename);
                currentfilename = filename;
            }
            else
            {
                MessageBox.Show("The file '" + Path.GetFileName(filename) + "' was not found.");
                mruMenu.RemoveFile(number);
            }
        }
        private void deleteTileToolStripMenuItem_Click(object sender, EventArgs e)                     
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {

                List<int> DeleteList = new List<int>();

                if (TileDisplay.Count > 1)
                {
                    for (int i = 0; i < panel1.Controls.Count; i++)
                    {
                        if (panel1.Controls[i].BackColor == Color.LightBlue)
                        {
                            DeleteList.Add(i);
                        }
                    }
                    

                    for (int i = DeleteList.Count - 1; i >= 0; i--)
                    {
                        TileDisplay.RemoveTile(DeleteList[i]);
                    }

                    panel1.Controls[0].Top = 0;
                    for (int i = 1; i < panel1.Controls.Count; i++)
                        panel1.Controls[i].Top = panel1.Controls[i - 1].Bottom;

                    int tempHeight;
                    tempHeight = panel1.Controls[panel1.Controls.Count - 1].Bottom;
                    if (tempHeight > panel1Height)
                    {
                        panel1.Height = panel1.Controls[panel1.Controls.Count - 1].Bottom;
                    }
                    else
                    {
                        panel1.Height = panel1Height;
                    }

                    SyncvScrollBar1();

                    if (TileDisplay.CurrentTile > 0)
                    {
                        if (TileDisplay.CurrentTile > panel1.Controls.Count - 1)
                        {
                            TileDisplay.CurrentTile = panel1.Controls.Count - 1;
                        }
                        try
                        {
                            panel1.Controls[TileDisplay.CurrentTile].BackColor = Color.LightBlue;
                        }
                        catch
                        {
                            panel1.Controls[panel1.Controls.Count - 1].BackColor = Color.LightBlue;
                        }
                    }
                    else
                    {
                        TileDisplay.CurrentTile = 0;
                        panel1.Controls[0].BackColor = Color.LightBlue;
                    }

                    int currenttile = 0;
                    for (int i = 0; i < panel1.Controls.Count; i++)
                    {
                        if (panel1.Controls[i].BackColor == Color.LightBlue)
                        {
                            currenttile = i;
                        }
                    }
                    ScrollToTile(currenttile);

                    workingTile.Image = ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls[2]).Image;
                    workingTile.Width = ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls[2]).Width;
                    workingTile.Height = ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls[2]).Height;

                    workingTile.Top = Convert.ToInt32((groupBox2.ClientSize.Height * 0.5) - (workingTile.Height * 0.5));
                    workingTile.Left = Convert.ToInt32((groupBox2.ClientSize.Width * 0.5) - (workingTile.Width * 0.5));

                    workingFullTile.Top = groupBox2.Bottom + 20;
                    workingFullTile.Image = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).fullTile.Image;
                    workingFullTile.Width = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).fullTile.Width;
                    workingFullTile.Height = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).fullTile.Height;

                    workingHalfTile.Image = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).halfTile.Image;
                    workingHalfTile.Width = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).halfTile.Width;
                    workingHalfTile.Height = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).halfTile.Height;

                    workingSmallTile.BackColor = ((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).smallTile.BackColor;

                    if (chkAutoBevel.CheckState == CheckState.Checked)
                    {
                        applyGradient(Convert.ToInt32(cboBorderSize.Text));
                    }

                    if (chkAutoClip.CheckState == CheckState.Checked)
                    {
                        applyCorners(Convert.ToInt32(cboCornerSize.Text));
                    }

                    workingHalfTile.Top = workingFullTile.Bottom + 20;
                    if (lockCreateHalffromFullStripButton1.CheckState == CheckState.Checked)
                    {
                        workingHalfTile.Height = gtlFile.Tile[TileDisplay.CurrentTile].HalfTile.Height;
                        workingHalfTile.Width = gtlFile.Tile[TileDisplay.CurrentTile].HalfTile.Width;
                        workingHalfTile.Image = gtlFile.Tile[TileDisplay.CurrentTile].HalfTile;
                    }
                    else
                    {
                        workingHalfTile.Width = (workingFullTile.Width / 2) + 1;
                        workingHalfTile.Height = (workingFullTile.Height / 2) + 1;
                        workingHalfTile.Image = FixedSize(workingFullTile.Image, workingHalfTile.Width, workingHalfTile.Height);
                    }

                    workingSmallTile.Top = workingHalfTile.Bottom + 20;
                }
                if (TileDisplay.Count == 1)
                {
                    deletetoolStripButton1.Enabled = false;
                    deleteTileToolStripMenuItem.Enabled = false;
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tile";
                }
                else
                {
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tiles";
                }
                txtText.BackColor = workingSmallTile.BackColor;
                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void ReplaceCurrentTile()
        {
            if (panel1 == null || panel1.Controls.Count == 0)
                return;

            if (TileDisplay == null || TileDisplay.CurrentTile < 0 || TileDisplay.CurrentTile >= panel1.Controls.Count)
                return;

            if (workingTile?.Image == null)
                return;

            var targetDisplay = panel1.Controls[TileDisplay.CurrentTile] as gtlTileDisplay;
            if (targetDisplay == null)
                return;

            if (lblCurrentWorkingTile != null && (lblCurrentWorkingTile.Text == currentworkingtile.HalfTile.ToString() || lblCurrentWorkingTile.Text == "HalfTile"))
            {
                // Half Tile Mode
                Image oldTarget = targetDisplay.halfTile.Image;
                Image oldBuffer = workingHalfTile.Image;

                Image newHalfImg = (Image)workingTile.Image.Clone();
                targetDisplay.halfTile.Image = newHalfImg;
                targetDisplay.halfTile.Width = workingTile.Width;
                targetDisplay.halfTile.Height = workingTile.Height;

                workingHalfTile.Image = (Image)newHalfImg.Clone();
                workingHalfTile.Width = workingTile.Width;
                workingHalfTile.Height = workingTile.Height;

                if (oldTarget != null && oldTarget != newHalfImg && oldTarget != workingHalfTile.Image)
                {
                    oldTarget.Dispose();
                }
                if (oldBuffer != null && oldBuffer != oldTarget && oldBuffer != newHalfImg && oldBuffer != workingHalfTile.Image)
                {
                    oldBuffer.Dispose();
                }
            }
            else
            {
                // Full Tile Mode (default)
                Image oldTargetFull = targetDisplay.fullTile.Image;
                Image oldBufferFull = workingFullTile.Image;

                Image newFullImg = (Image)workingTile.Image.Clone();
                targetDisplay.fullTile.Image = newFullImg;
                targetDisplay.fullTile.Width = workingTile.Width;
                targetDisplay.fullTile.Height = workingTile.Height;

                workingFullTile.Image = (Image)newFullImg.Clone();
                workingFullTile.Width = workingTile.Width;
                workingFullTile.Height = workingTile.Height;

                if (oldTargetFull != null && oldTargetFull != newFullImg && oldTargetFull != workingFullTile.Image)
                {
                    oldTargetFull.Dispose();
                }
                if (oldBufferFull != null && oldBufferFull != oldTargetFull && oldBufferFull != newFullImg && oldBufferFull != workingFullTile.Image)
                {
                    oldBufferFull.Dispose();
                }

                if (lockCreateHalffromFullStripButton1 != null && lockCreateHalffromFullStripButton1.CheckState == CheckState.Unchecked)
                {
                    int halfW = (workingFullTile.Width / 2) + 1;
                    int halfH = (workingFullTile.Height / 2) + 1;
                    Image resizedHalf = FixedSize(workingTile.Image, halfW, halfH);

                    Image oldTargetHalf = targetDisplay.halfTile.Image;
                    Image oldBufferHalf = workingHalfTile.Image;

                    targetDisplay.halfTile.Image = resizedHalf;
                    targetDisplay.halfTile.Width = halfW;
                    targetDisplay.halfTile.Height = halfH;

                    workingHalfTile.Image = (Image)resizedHalf.Clone();
                    workingHalfTile.Width = halfW;
                    workingHalfTile.Height = halfH;

                    if (oldTargetHalf != null && oldTargetHalf != resizedHalf && oldTargetHalf != workingHalfTile.Image)
                    {
                        oldTargetHalf.Dispose();
                    }
                    if (oldBufferHalf != null && oldBufferHalf != oldTargetHalf && oldBufferHalf != resizedHalf && oldBufferHalf != workingHalfTile.Image)
                    {
                        oldBufferHalf.Dispose();
                    }
                }
            }

            if (workingSmallTile != null && targetDisplay.smallTile != null)
            {
                targetDisplay.smallTile.BackColor = workingSmallTile.BackColor;
            }

            targetDisplay.fullTile.Invalidate();
            targetDisplay.halfTile.Invalidate();
            targetDisplay.Invalidate();
            panel1.Invalidate();
        }

        private void replaceTiletoolStripMenuItem_Click(object sender, EventArgs e)                    
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            ReplaceCurrentTile();
        }
        private void btnStore_Click(object sender, EventArgs e)                                        
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Store Text Button");
            }
            else
            {
                if (chkIncrement.Checked == true)
                {
                    Regex _Regex = new Regex(@"#");
                    Match m = _Regex.Match(txtText.Text);
                    if (!m.Success)
                    {
                        if (MessageBox.Show("The increment feature requires the format '#d', example: (A/#1/1).", "Danger Will Robinson!", MessageBoxButtons.OK) == DialogResult.OK)
                        {
                            chkIncrement.Checked = false;
                        }
                    }
                    else
                    {
                        Point newPoint = new Point(hScrollText.Value, vScrollText.Value);
                        TextBox newTextbox = new TextBox();
                        newTextbox = txtText;
                        newTextbox.Width = txtText.Width;
                        newTextbox.Height = txtText.Font.Height;
                        PositionDisplay.addPosition(newTextbox,
                                                    chkIncrement,
                                                    newPoint,
                                                    Convert.ToInt32(txtRotate.Text),
                                                    lblCurrentWorkingTile.Text);

                        chkIncrement.Checked = false;

                        SyncvScrollBar2();
                    }
                }
                else
                {
                    Point newPoint = new Point(hScrollText.Value, vScrollText.Value);
                    TextBox newTextbox = new TextBox();
                    newTextbox = txtText;
                    newTextbox.Width = txtText.Width;
                    PositionDisplay.addPosition(newTextbox,
                                                chkIncrement,
                                                newPoint,
                                                Convert.ToInt32(txtRotate.Text),
                                                lblCurrentWorkingTile.Text);

                    chkIncrement.Checked = false;

                    SyncvScrollBar2();
                }
            }
            workingTile.Focus();
        }
        private void btnReduceRange_Click(object sender, EventArgs e)                                  
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Expand Text Range");
            }
            else
            {
                hScrollText.Minimum = hScrollText.Minimum + 10;
                hScrollText.Maximum = hScrollText.Maximum - 10;
                vScrollText.Minimum = vScrollText.Minimum + 10;
                vScrollText.Maximum = vScrollText.Maximum - 10;
                txtText.Focus();
            }
        }
        private void fullTiletoolStripButton1_Click(object sender, EventArgs e)                        
        {
            workingTile.Width = workingFullTile.Width;
            workingTile.Height = workingFullTile.Height;
            workingTile.Image = workingFullTile.Image;
            centerWorkingTile();
            resizeWorkingArea();
            if (pctZoom.Visible)
                paintZoom();
            workingTile.Invalidate();
            workingTile.Update();
            UpdateWorkingTileGraphics(workingTile.Image);
            currentWorkingTile = (int)currentworkingtile.FullTile;
            lblCurrentWorkingTile.Text = currentworkingtile.FullTile.ToString();
        }
        private void halfTiletoolStripButton1_Click(object sender, EventArgs e)                        
        {
            workingTile.Width = workingHalfTile.Width;
            workingTile.Height = workingHalfTile.Height;
            workingTile.Image = workingHalfTile.Image;
            centerWorkingTile();
            workingTile.Invalidate();
            workingTile.Update();
            UpdateWorkingTileGraphics(workingTile.Image);
            currentWorkingTile = (int)currentworkingtile.HalfTile;
            lblCurrentWorkingTile.Text = currentworkingtile.HalfTile.ToString();
        }
        private void smallTiletoolStripButton2_Click(object sender, EventArgs e)                       
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
                workingSmallTile.BackColor = colorDialog1.Color;
        }
        private void createFromPositiontoolStripMenuItem4_Click(object sender, EventArgs e)            
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            for (int i = 0; i < panel3.Controls.Count; i++)
            {
                string workingString;
                Image positionImage;
                Graphics gPositionText;
                positionTextDisplay workingPosition = (positionTextDisplay)panel3.Controls[i];
                SolidBrush drawBrush = new SolidBrush(workingPosition.txtPositionText.ForeColor);

                Regex _Regex = new Regex(@"#");
                workingString = _Regex.Replace(workingPosition.txtPositionText.Text, "");

                if (workingPosition.WorkingSize == "FullTile")
                {
                    positionImage = workingFullTile.Image;
                    workingFullTile.Image = (Image)positionImage.Clone();
                    gPositionText = Graphics.FromImage(workingFullTile.Image);
                    gPositionText.TranslateTransform(workingPosition.PointPlacement.X, workingPosition.PointPlacement.Y);
                    gPositionText.RotateTransform(workingPosition.RotationDegrees);
                    gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                    gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                    gPositionText.ResetTransform();
                    workingFullTile.Invalidate();
                    workingFullTile.Update();
                }
                else
                {
                    positionImage = workingHalfTile.Image;
                    workingHalfTile.Image = (Image)positionImage.Clone();
                    gPositionText = Graphics.FromImage(workingHalfTile.Image);
                    gPositionText.TranslateTransform(workingPosition.PointPlacement.X, workingPosition.PointPlacement.Y);
                    gPositionText.RotateTransform(workingPosition.RotationDegrees);
                    gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                    gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                    gPositionText.ResetTransform();
                    workingHalfTile.Invalidate();
                    workingHalfTile.Update();
                }
                if (workingPosition.txtPositionIncrement.Visible)
                {
                    try
                    {
                        _Regex = new Regex(@"#\d+");
                        Match m = _Regex.Match(workingPosition.txtPositionText.Text);
                        if (m.Success)
                        {
                            workingPosition.txtPositionText.Text = _Regex.Replace(workingPosition.txtPositionText.Text, "#" + Convert.ToString((Convert.ToInt32(m.Value.Substring(1)) + Convert.ToInt32(workingPosition.txtPositionIncrement.Text))));
                        }
                        else
                        {
                            workingPosition.txtPositionText.Text = Convert.ToString(Convert.ToInt32(workingPosition.txtPositionText.Text) + Convert.ToInt32(workingPosition.txtPositionIncrement.Text));
                        }
                    }
                    catch
                    {

                    }
                }
            }
            GTL_File.gtltile newTile = new GTL_File.gtltile();

            newTile.FullTile = (Bitmap)workingFullTile.Image;
            if (lockCreateHalffromFullStripButton1.CheckState == CheckState.Unchecked)
            {
                newTile.HalfTile = (Bitmap)FixedSize(workingFullTile.Image, workingHalfTile.Width, workingHalfTile.Height);
            }
            else
            {
                newTile.HalfTile = (Bitmap)workingHalfTile.Image;
            }
            newTile.SmallTile = workingSmallTile.BackColor;

            TileDisplay.AddTile(newTile);
            SyncvScrollBar1();
            if (TileDisplay.Count > 1)
                deletetoolStripButton1.Enabled = true;
            lblCurrentWorkingTile.Text = "FullTile";
            workingTile.Image = TileDisplay[TileDisplay.CurrentTile].fullTile.Image;
            workingFullTile.Image = TileDisplay[TileDisplay.CurrentTile].fullTile.Image;

            workingSmallTile.BackColor = TileDisplay[TileDisplay.CurrentTile].smallTile.BackColor;

            if (lockCreateHalffromFullStripButton1.CheckState == CheckState.Unchecked)
            {
                workingHalfTile.Width = (workingFullTile.Width / 2) + 1;
                workingHalfTile.Height = (workingFullTile.Height / 2) + 1;
                workingHalfTile.Image = FixedSize(workingFullTile.Image, workingHalfTile.Width, workingHalfTile.Height);
            }
            else
            {
                workingHalfTile.Image = TileDisplay[TileDisplay.CurrentTile].halfTile.Image;
            }
            if (TileDisplay.Count == 1)
            {
                this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tile";
            }
            else
            {
                this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tiles";
            }
            txtWidth.Text = workingTile.Width.ToString();
            txtHeight.Text = workingTile.Height.ToString();
            centerWorkingTile();
            resizeWorkingArea();
            if (pctZoom.Visible)
                paintZoom();
        }
        private void selectFullTiletoolStripMenuItem_Click(object sender, EventArgs e)                 
        {
            workingTile.Width = workingFullTile.Width;
            workingTile.Height = workingFullTile.Height;
            workingTile.Image = workingFullTile.Image;
            centerWorkingTile();
            resizeWorkingArea();
            if (pctZoom.Visible)
                paintZoom();
            workingTile.Invalidate();
            workingTile.Update();
            UpdateWorkingTileGraphics(workingTile.Image);
            currentWorkingTile = (int)currentworkingtile.FullTile;
            lblCurrentWorkingTile.Text = currentworkingtile.FullTile.ToString();
        }
        private void selectHalftoolStripMenuItem_Click(object sender, EventArgs e)                     
        {
            workingTile.Width = workingHalfTile.Width;
            workingTile.Height = workingHalfTile.Height;
            workingTile.Image = workingHalfTile.Image;
            centerWorkingTile();
            workingTile.Invalidate();
            workingTile.Update();
            UpdateWorkingTileGraphics(workingTile.Image);
            currentWorkingTile = (int)currentworkingtile.HalfTile;
            lblCurrentWorkingTile.Text = currentworkingtile.HalfTile.ToString();
        }
        private void selectSmalltoolStripMenuItem_Click(object sender, EventArgs e)                    
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
                workingSmallTile.BackColor = colorDialog1.Color;
        }
        private void savePositiontoolStripMenuItem_Click(object sender, EventArgs e)                   
        {
            saveFileDialog1.FileName =  Path.GetFileNameWithoutExtension(currentfilename) + ".gtp";
            saveFileDialog1.Title = "Save GTL Position file...";
            saveFileDialog1.Filter = "GTP files (*.gtp)|*.gtp";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                List<PositionDataDto> dtoList = new List<PositionDataDto>();
                positionTextDisplay workingPosition = new positionTextDisplay();

                for (int i = 0; i < panel3.Controls.Count; i++)
                {
                    workingPosition = (positionTextDisplay)panel3.Controls[i];

                    dtoList.Add(new PositionDataDto
                    {
                        TxtText = workingPosition.txtPositionText.Text,
                        TxtIncrement = workingPosition.txtPositionIncrement.Text,
                        BolIncrementVisible = workingPosition.txtPositionIncrement.Visible,
                        FontName = workingPosition.txtPositionText.Font.Name,
                        FontSize = workingPosition.txtPositionText.Font.Size,
                        FontStyle = (int)workingPosition.txtPositionText.Font.Style,
                        ColorArgb = workingPosition.txtPositionText.ForeColor.ToArgb(),
                        Rotation = workingPosition.RotationDegrees,
                        PointX = workingPosition.PointPlacement.X,
                        PointY = workingPosition.PointPlacement.Y,
                        TxtTile = workingPosition.WorkingSize
                    });
                }

                try 
                {
                    string json = JsonSerializer.Serialize(dtoList, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(saveFileDialog1.FileName, json);
                }
                catch
                {
                }
            }
        }
        private void openPositiontoolStripMenuItem_Click(object sender, EventArgs e)                   
        {
            openFileDialog1.FileName = Path.GetFileNameWithoutExtension(currentfilename) + ".gtp";
            openFileDialog1.Title = "Open GTL Position file...";
            openFileDialog1.Filter = "GTP files (*.gtp)|*.gtp";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string json = File.ReadAllText(openFileDialog1.FileName);
                    List<PositionDataDto> dtoList = JsonSerializer.Deserialize<List<PositionDataDto>>(json);
                    if (dtoList != null)
                    {
                        foreach (PositionDataDto workingdata in dtoList)
                        {
                            TextBox newTextbox = new TextBox();
                            newTextbox.Text = workingdata.TxtText;
                            newTextbox.Font = new Font(workingdata.FontName, workingdata.FontSize, (FontStyle)workingdata.FontStyle);
                            newTextbox.Height = newTextbox.Font.Height;
                            newTextbox.ForeColor = Color.FromArgb(workingdata.ColorArgb);
                            newTextbox.BackColor = txtText.BackColor;

                            CheckBox newCheckbox = new CheckBox();
                            newCheckbox.Checked = workingdata.BolIncrementVisible;

                            PositionDisplay.addPosition(newTextbox,
                                                        newCheckbox,
                                                        new Point(workingdata.PointX, workingdata.PointY),
                                                        workingdata.Rotation,
                                                        workingdata.TxtTile);

                            if (panel2.Controls[panel2.Controls.Count - 1].Bottom > panel2.Height)
                            {
                                vScrollBar2.Enabled = true;
                            }
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("Invalid file format");
                }
            }
        }
        private void placeTexttoolStripButton_Click(object sender, EventArgs e)                        
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Text Tools");
            }
            else
            {
                drawText();
            }
        }
        private void StoreTexttoolStripMenuItem_Click(object sender, EventArgs e)                      
        {
            Point newPoint = new Point(hScrollText.Value, vScrollText.Value);
            TextBox newTextbox = new TextBox();
            newTextbox = txtText;
            PositionDisplay.addPosition(newTextbox,
                                        chkIncrement,
                                        newPoint,
                                        Convert.ToInt32(txtRotate.Text),
                                        lblCurrentWorkingTile.Text);

            chkIncrement.Checked = false;

            if (panel2.Controls[panel2.Controls.Count - 1].Bottom > panel2.Height)
            {
                vScrollBar2.Enabled = true;
            }
        }
        private void storetoolStripButton_Click(object sender, EventArgs e)                            
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Text Tools");
            }
            else
            {
                Point newPoint = new Point(hScrollText.Value, vScrollText.Value);
                TextBox newTextbox = new TextBox();
                newTextbox = txtText;
                PositionDisplay.addPosition(newTextbox,
                                            chkIncrement,
                                            newPoint,
                                            Convert.ToInt32(txtRotate.Text),
                                            lblCurrentWorkingTile.Text);

                chkIncrement.Checked = false;

                if (panel2.Controls[panel2.Controls.Count - 1].Bottom > panel2.Height)
                {
                    vScrollBar2.Enabled = true;
                }
            }
        }
        private void createTiletoolStripButton_Click(object sender, EventArgs e)                       
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                GTL_File.gtltile newTile = new GTL_File.gtltile();
                newTile.FullTile = (Bitmap)workingFullTile.Image;
                newTile.HalfTile = (Bitmap)workingHalfTile.Image;
                newTile.SmallTile = workingSmallTile.BackColor;
                TileDisplay.AddTile(newTile);
                SyncvScrollBar1();
                if (TileDisplay.Count > 1)
                {
                    deleteTileToolStripMenuItem.Enabled = true;
                    deletetoolStripButton1.Enabled = true;
                }
                lblCurrentWorkingTile.Text = "FullTile";

                workingTile.Width = TileDisplay[TileDisplay.CurrentTile].fullTile.Width;
                workingTile.Height = TileDisplay[TileDisplay.CurrentTile].fullTile.Height;
                workingTile.Image = TileDisplay[TileDisplay.CurrentTile].fullTile.Image;

                workingFullTile.Width = TileDisplay[TileDisplay.CurrentTile].fullTile.Width;
                workingFullTile.Height = TileDisplay[TileDisplay.CurrentTile].fullTile.Height;
                workingFullTile.Image = TileDisplay[TileDisplay.CurrentTile].fullTile.Image;

                workingHalfTile.Width = TileDisplay[TileDisplay.CurrentTile].halfTile.Width;
                workingHalfTile.Height = TileDisplay[TileDisplay.CurrentTile].halfTile.Height;
                workingHalfTile.Image = TileDisplay[TileDisplay.CurrentTile].halfTile.Image;

                workingSmallTile.BackColor = TileDisplay[TileDisplay.CurrentTile].smallTile.BackColor;

                if (TileDisplay.Count == 1)
                {
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tile";
                }
                else
                {
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tiles";
                }
                txtWidth.Text = workingTile.Width.ToString();
                txtHeight.Text = workingTile.Height.ToString();
                centerWorkingTile();
                //resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void createFromPositiontoolStripButton_Click(object sender, EventArgs e)               
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                for (int i = 0; i < panel3.Controls.Count; i++)
                {
                    string workingString;
                    Image positionImage;
                    Graphics gPositionText;
                    positionTextDisplay workingPosition = (positionTextDisplay)panel3.Controls[i];
                    SolidBrush drawBrush = new SolidBrush(workingPosition.txtPositionText.ForeColor);

                    if (workingPosition.cbInclude.Checked)
                    {
                        Regex _Regex = new Regex(@"#");
                        workingString = _Regex.Replace(workingPosition.txtPositionText.Text, "");

                        if (workingPosition.WorkingSize == "FullTile")
                        {
                            positionImage = workingFullTile.Image;
                            workingFullTile.Image = (Image)positionImage.Clone();
                            gPositionText = Graphics.FromImage(workingFullTile.Image);
                            gPositionText.TranslateTransform(workingPosition.PointPlacement.X, workingPosition.PointPlacement.Y);
                            gPositionText.RotateTransform(workingPosition.RotationDegrees);
                            gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                            gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                            gPositionText.ResetTransform();
                            workingFullTile.Invalidate();
                            workingFullTile.Update();
                        }
                        else
                        {
                            positionImage = workingHalfTile.Image;
                            workingHalfTile.Image = (Image)positionImage.Clone();
                            gPositionText = Graphics.FromImage(workingHalfTile.Image);
                            gPositionText.TranslateTransform(workingPosition.PointPlacement.X, workingPosition.PointPlacement.Y);
                            gPositionText.RotateTransform(workingPosition.RotationDegrees);
                            gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                            gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                            gPositionText.ResetTransform();
                            workingHalfTile.Invalidate();
                            workingHalfTile.Update();
                        }
                        if (workingPosition.txtPositionIncrement.Visible)
                        {
                            try
                            {
                                _Regex = new Regex(@"#\d+");
                                Match m = _Regex.Match(workingPosition.txtPositionText.Text);
                                if (m.Success)
                                {
                                    workingPosition.txtPositionText.Text = _Regex.Replace(workingPosition.txtPositionText.Text, "#" + Convert.ToString((Convert.ToInt32(m.Value.Substring(1)) + Convert.ToInt32(workingPosition.txtPositionIncrement.Text))));
                                }
                                else
                                {
                                    workingPosition.txtPositionText.Text = Convert.ToString(Convert.ToInt32(workingPosition.txtPositionText.Text) + Convert.ToInt32(workingPosition.txtPositionIncrement.Text));
                                }
                            }
                            catch
                            {

                            }
                        }
                    }

                }
                GTL_File.gtltile newTile = new GTL_File.gtltile();

                newTile.FullTile = (Bitmap)workingFullTile.Image;
                if (lockCreateHalffromFullStripButton1.CheckState == CheckState.Unchecked)
                {
                    newTile.HalfTile = (Bitmap)FixedSize(workingFullTile.Image, workingHalfTile.Width, workingHalfTile.Height);
                }
                else
                {
                    newTile.HalfTile = (Bitmap)workingHalfTile.Image;
                }
                newTile.SmallTile = workingSmallTile.BackColor;

                TileDisplay.AddTile(newTile);
                SyncvScrollBar1();
                if (TileDisplay.Count > 1)
                {
                    deleteTileToolStripMenuItem.Enabled = true;
                    deletetoolStripButton1.Enabled = true;
                }
                lblCurrentWorkingTile.Text = "FullTile";

                workingTile.Image = TileDisplay[TileDisplay.CurrentTile].fullTile.Image;
                workingFullTile.Image = TileDisplay[TileDisplay.CurrentTile].fullTile.Image;

                workingSmallTile.BackColor = TileDisplay[TileDisplay.CurrentTile].smallTile.BackColor;

                if (lockCreateHalffromFullStripButton1.CheckState == CheckState.Unchecked)
                {
                    workingHalfTile.Width = (workingFullTile.Width / 2) + 1;
                    workingHalfTile.Height = (workingFullTile.Height / 2) + 1;
                    workingHalfTile.Image = FixedSize(workingFullTile.Image, workingHalfTile.Width, workingHalfTile.Height);
                }
                else
                {
                    workingHalfTile.Image = TileDisplay[TileDisplay.CurrentTile].halfTile.Image;
                }

                if (TileDisplay.Count == 1)
                {
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tile";
                }
                else
                {
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tiles";
                }
                txtWidth.Text = workingTile.Width.ToString();
                txtHeight.Text = workingTile.Height.ToString();
                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void replaceTooltoolStripButton1_Click(object sender, EventArgs e)                     
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                ReplaceCurrentTile();
            }
        }
        public void CloseCurrentFile()
        {
            currentfilename = "";
            openedFile = "";
            this.Text = "GTL Editor";
            gtlFile = new GTL_File();

            UpdateWorkingTileGraphics(null);
            workingTile.Image = null;
            workingTile.Visible = false;

            if (workingFullTile.Image != null)
            {
                workingFullTile.Image.Dispose();
                workingFullTile.Image = null;
            }
            workingFullTile.Visible = false;

            if (workingHalfTile.Image != null)
            {
                workingHalfTile.Image.Dispose();
                workingHalfTile.Image = null;
            }
            workingHalfTile.Visible = false;

            workingSmallTile.Visible = false;
            pctZoom.Visible = false;

            btnBumpUp.Visible = false;
            btnBumpRight.Visible = false;
            btnBumpDown.Visible = false;
            btnBumpLeft.Visible = false;

            TileDisplay.RemoveAllTiles();
            if (panel1.Controls.Count > 0)
            {
                panel1.Controls.Clear();
            }
            UpdateTilePanelBackground();

            vScrollBar1.Value = 0;
            vScrollBar1.Maximum = 0;
            vScrollBar1.Enabled = false;
            panel1.Top = 0;

            PositionDisplay.removeAllPositions();

            txtWidth.Text = "";
            txtHeight.Text = "";
            lblTileSize.Text = "0,0";
            lblTextPlacement.Text = "0,0";
            txtText.Text = "";
            txtText.BackColor = SystemColors.Window;
            pctTransparentColor.BackColor = Color.Cyan;
            this.toolStripStatusLabel1.Text = "";

            groupBox1.Enabled = false;
            groupBox2.Enabled = false;
            groupBox3.Enabled = false;
            groupBox4.Enabled = false;

            for (int i = 1; i < toolStrip1.Items.Count; i++)
            {
                toolStrip1.Items[i].Enabled = false;
            }

            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            closeFileToolStripMenuItem.Enabled = false;

            openPositiontoolStripMenuItem.Enabled = false;
            savePositiontoolStripMenuItem.Enabled = false;
            createPNGtoolStripMenuItem.Enabled = false;

            for (int i = 0; i < ((ToolStripDropDownItem)editToolStripMenuItem).DropDownItems.Count; i++)
            {
                try
                {
                    ((ToolStripDropDownItem)editToolStripMenuItem.DropDownItems[i]).Enabled = false;
                }
                catch
                {
                }
            }
            for (int i = 0; i < ((ToolStripDropDownItem)texttoolStripMenuItem).DropDownItems.Count; i++)
            {
                try
                {
                    ((ToolStripDropDownItem)texttoolStripMenuItem.DropDownItems[i]).Enabled = false;
                }
                catch
                {
                }
            }
            for (int i = 0; i < ((ToolStripDropDownItem)tiletoolStripMenuItem).DropDownItems.Count; i++)
            {
                try
                {
                    ((ToolStripDropDownItem)tiletoolStripMenuItem.DropDownItems[i]).Enabled = false;
                }
                catch
                {
                }
            }

            deleteTileToolStripMenuItem.Enabled = false;
            deletetoolStripButton1.Enabled = false;
        }

        private void closeFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseCurrentFile();
        }
        private void createPNGtoolStripMenuItem_Click(object sender, EventArgs e)                      
        {
            if (panel1.Controls.Count == 0)
            {
                MessageBox.Show("No tiles are currently loaded to export.", "Export to PNG", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Export Tiles to PNG (Select Folder & Base Name)...";
                sfd.Filter = "PNG Image (*.png)|*.png";
                sfd.OverwritePrompt = false;
                sfd.AddExtension = true;
                sfd.DefaultExt = "png";

                string defaultBase = !string.IsNullOrWhiteSpace(currentfilename)
                    ? Path.GetFileNameWithoutExtension(currentfilename)
                    : "Tile";
                sfd.FileName = $"{defaultBase}-01.png";

                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return;

                string targetDir = Path.GetDirectoryName(sfd.FileName);
                if (string.IsNullOrWhiteSpace(targetDir) || !Directory.Exists(targetDir))
                {
                    MessageBox.Show("Invalid destination folder selected.", "Export to PNG", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string rawName = Path.GetFileNameWithoutExtension(sfd.FileName);

                // Strip trailing counter suffix from user-entered name (e.g. trailing "-01", " - 01", "_01", " 1", "_1")
                string baseName = Regex.Replace(rawName, @"[\s_-]+0*\d+$", "").Trim();
                char[] invalidChars = Path.GetInvalidFileNameChars();
                string safeBaseName = string.Concat(baseName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
                if (string.IsNullOrWhiteSpace(safeBaseName))
                    safeBaseName = defaultBase;

                int totalTiles = panel1.Controls.Count;
                int padDigits = Math.Max(2, totalTiles.ToString().Length);
                string formatString = new string('0', padDigits);

                // Check if matching output files already exist in the target folder
                bool anyExisting = false;
                for (int i = 0; i < totalTiles; i++)
                {
                    string testPath = Path.Combine(targetDir, $"{safeBaseName}-{(i + 1).ToString(formatString)}.png");
                    if (File.Exists(testPath))
                    {
                        anyExisting = true;
                        break;
                    }
                }

                if (anyExisting)
                {
                    DialogResult overwriteChoice = MessageBox.Show(
                        $"One or more files matching '{safeBaseName}-XX.png' already exist in the selected folder.\n\nDo you want to overwrite them?",
                        "Confirm Overwrite",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (overwriteChoice != DialogResult.Yes)
                        return;
                }

                int exportedCount = 0;
                Color maskColor = pctTransparentColor.BackColor;

                for (int i = 0; i < totalTiles; i++)
                {
                    if (panel1.Controls[i] is gtlTileDisplay display && display.fullTile?.Image != null)
                    {
                        using (Bitmap tileBmp = (Bitmap)display.fullTile.Image.Clone())
                        {
                            tileBmp.MakeTransparent(maskColor);
                            string outFileName = $"{safeBaseName}-{(i + 1).ToString(formatString)}.png";
                            string fullPath = Path.Combine(targetDir, outFileName);
                            tileBmp.Save(fullPath, ImageFormat.Png);
                            exportedCount++;
                        }
                    }
                }

                MessageBox.Show(
                    $"Successfully exported {exportedCount} tile(s) to PNG:\n{targetDir}",
                    "Export Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        private void btnAlignLeft_Click(object sender, EventArgs e)                                    
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Align Text Buttons");
            }
            else
            {
                hScrollText.Value = 0;
                this.lblTextPlacement.Text = this.hScrollText.Value.ToString() + "," + this.vScrollText.Value.ToString();
                drawText();
                txtText.Focus();
            }
        }
        private void btnAlignCenter_Click(object sender, EventArgs e)                                  
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Align Text Buttons");
            }
            else
            {
                SizeF stringSize = new SizeF();
                stringSize = gWorkingTile.MeasureString(txtText.Text, txtText.Font);
                int intStringCenter = Convert.ToInt32(stringSize.Width * 0.5);
                int intTileCenter = Convert.ToInt32(workingTile.Width * 0.5);
                int intLeft = intTileCenter - intStringCenter;
                try
                {
                    hScrollText.Value = intLeft;
                }
                catch
                {
                    hScrollText.Maximum = intLeft;
                    hScrollText.Value = intLeft;
                }
                this.lblTextPlacement.Text = this.hScrollText.Value.ToString() + "," + this.vScrollText.Value.ToString();
                drawText();
                txtText.Focus();
            }
        }
        private void btnAlignRights_Click(object sender, EventArgs e)                                  
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Align Text Buttons");
            }
            else
            {
                SizeF stringSize = new SizeF();
                stringSize = gWorkingTile.MeasureString(txtText.Text, txtText.Font);
                int intLeft = workingTile.Width - Convert.ToInt32(stringSize.Width);
                try
                {
                    hScrollText.Value = intLeft;
                }
                catch
                {
                    hScrollText.Maximum = intLeft;
                    hScrollText.Value = intLeft;
                }
                this.lblTextPlacement.Text = this.hScrollText.Value.ToString() + "," + this.vScrollText.Value.ToString();
                drawText();
                txtText.Focus();
            }
        }
        private void btnAlignTop_Click(object sender, EventArgs e)                                     
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Align Text Buttons");
            }
            else
            {
                vScrollText.Value = 0;
                this.lblTextPlacement.Text = this.hScrollText.Value.ToString() + "," + this.vScrollText.Value.ToString();
                drawText();
                txtText.Focus();
            }
        }
        private void btnAlignMiddle_Click(object sender, EventArgs e)                                  
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Align Text Buttons");
            }
            else
            {
                SizeF stringSize = new SizeF();
                stringSize = gWorkingTile.MeasureString(txtText.Text, txtText.Font);
                int intStringCenter = Convert.ToInt32(stringSize.Height * 0.5);
                int intTileMiddle = Convert.ToInt32(workingTile.Height * 0.5);
                int intTop = intTileMiddle - intStringCenter;
                try
                {
                    vScrollText.Value = intTop;
                }
                catch
                {
                    vScrollText.Maximum = intTop;
                    vScrollText.Value = intTop;
                }
                this.lblTextPlacement.Text = this.hScrollText.Value.ToString() + "," + this.vScrollText.Value.ToString();
                drawText();
                txtText.Focus();
            }
        }
        private void btnAlignBottom_Click(object sender, EventArgs e)                                  
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Align Text Buttons");
            }
            else
            {
                SizeF stringSize = new SizeF();
                stringSize = gWorkingTile.MeasureString(txtText.Text, txtText.Font);
                int intTop = workingTile.Height - Convert.ToInt32(stringSize.Height);
                try
                {
                    vScrollText.Value = intTop;
                }
                catch
                {
                    vScrollText.Maximum = intTop;
                    vScrollText.Value = intTop;
                }
                this.lblTextPlacement.Text = this.hScrollText.Value.ToString() + "," + this.vScrollText.Value.ToString();
                drawText();
                txtText.Focus();
            }
        }
        private void vScrollBar2_Scroll(object sender, ScrollEventArgs e)                              
        {
            try
            {
                int val = vScrollBar2.Value;
                int maxScroll = vScrollBar2.Maximum - vScrollBar2.LargeChange + 1;
                if (maxScroll < 0) maxScroll = 0;
                if (val > maxScroll) val = maxScroll;
                if (val < 0) val = 0;
                vScrollBar2.Value = val;

                this.panel3.Top = -val + 4;
            }
            catch
            {
            }
        }
        private void btnGradient_Click(object sender, EventArgs e)                                     
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Bevel Button");
            }
            else
            {
                applyGradient(Convert.ToInt32(cboBorderSize.Text));
                workingTile.Focus();
                if (pctZoom.Visible)
                    paintZoom();

            }
        }
        private void applyGradient(int bordersize)                                                     
        {
            Image workerTile;
            Bitmap bmpTile;
            workerTile = null;

            workerTile = (Image)workingTile.Image.Clone();
            bmpTile = (Bitmap)workingTile.Image.Clone();

            UpdateWorkingTileGraphics(workerTile);

            LinearGradientBrush linearBrush;

            float[] r1Positions = { 0.0f, 1.0f };
            float[] r1Intensitives = { 0.5f, 1.0f };
            Blend blend1 = new Blend();
            blend1.Factors = r1Positions;
            blend1.Positions = r1Positions;

            Point point1 = new Point(0, 0);
            Point point2 = new Point(bordersize, bordersize);
            Point point3 = new Point(bordersize, workingTile.Height - bordersize);
            Point point4 = new Point(0, workingTile.Height);
            Point point5 = new Point(bordersize, 0);

            linearBrush = new LinearGradientBrush(point1, point5, Color.Beige, workingSmallTile.BackColor);
            linearBrush.Blend = blend1;
            Point[] areaPointsLeft = { point1, point2, point3, point4 };
            gWorkingTile.FillPolygon(linearBrush, areaPointsLeft);

            point1 = new Point(0, 0);
            point2 = new Point(bordersize, bordersize);
            point3 = new Point(workingTile.Width - bordersize, bordersize);
            point4 = new Point(workingTile.Width, 0);
            point5 = new Point(0, bordersize);

            linearBrush = new LinearGradientBrush(point1, point5, Color.Beige, workingSmallTile.BackColor);
            linearBrush.Blend = blend1;
            Point[] areaPointsTop = { point1, point2, point3, point4 };
            gWorkingTile.FillPolygon(linearBrush, areaPointsTop);

            point1 = new Point(workerTile.Width, 0);
            point2 = new Point(workerTile.Width - bordersize, bordersize);
            point3 = new Point(workingTile.Width - bordersize, workerTile.Height - bordersize);
            point4 = new Point(workingTile.Width, workerTile.Height);
            point5 = new Point(workerTile.Width - bordersize, 0);

            linearBrush = new LinearGradientBrush(point1, point5, Color.Black, workingSmallTile.BackColor);
            linearBrush.Blend = blend1;
            Point[] areaPointsRight = { point1, point2, point3, point4 };
            gWorkingTile.FillPolygon(linearBrush, areaPointsRight);

            point1 = new Point(0, workingTile.Height);
            point2 = new Point(bordersize, workingTile.Height - bordersize);
            point3 = new Point(workingTile.Width - bordersize, workerTile.Height - bordersize);
            point4 = new Point(workingTile.Width, workerTile.Height);
            point5 = new Point(0, workingTile.Height - bordersize);

            linearBrush = new LinearGradientBrush(point1, point5, Color.Black, workingSmallTile.BackColor);
            linearBrush.Blend = blend1;
            Point[] areaPointsBottom = { point1, point2, point3, point4 };
            gWorkingTile.FillPolygon(linearBrush, areaPointsBottom);

            workingTile.Image = workerTile;
            workingTile.Invalidate();
            workingTile.Update();
        }
        private void btnCorners_Click(object sender, EventArgs e)                                      
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Clip Button");
            }
            else
            {
                applyCorners(Convert.ToInt32(cboCornerSize.Text));
                workingTile.Focus();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void applyCorners(int cornersize)                                                      
        {
            Image workerTile;
            workerTile = null;
            SolidBrush drawBrush = new SolidBrush(txtText.ForeColor);

            workerTile = (Image)workingTile.Image.Clone();
            UpdateWorkingTileGraphics(workerTile);

            Pen pen = new Pen(pctTransparentColor.BackColor);
           
            //Upper Left
            gWorkingTile.DrawLine(pen, 0, 0, 0, cornersize);
            gWorkingTile.DrawLine(pen, 0, 0, cornersize, 0);
            //Upper Right
            gWorkingTile.DrawLine(pen, workerTile.Width - 1, 0, workerTile.Width - cornersize - 1, 0);
            gWorkingTile.DrawLine(pen, workerTile.Width - 1, 0, workerTile.Width - 1, cornersize);
            //Lower Right
            gWorkingTile.DrawLine(pen, workingTile.Width - 1, workingTile.Height - 1, workingTile.Width - 1, workingTile.Height - cornersize - 1);
            gWorkingTile.DrawLine(pen, workingTile.Width - 1, workingTile.Height - 1, workingTile.Width - cornersize-1, workingTile.Height - 1);
            //Lower Left
            gWorkingTile.DrawLine(pen, 0, workingTile.Height - 1, cornersize, workingTile.Height - 1);
            gWorkingTile.DrawLine(pen, 0, workingTile.Height - 1, 0, workingTile.Height - cornersize - 1);

            if (cornersize == 4)
            {
                gWorkingTile.DrawLine(pen, 1, 1, 0, 0);
                gWorkingTile.DrawLine(pen, workingTile.Width - 2, 1, workingTile.Width - 1, 0);
                gWorkingTile.DrawLine(pen, workingTile.Width - 2, workingTile.Height - 2, workingTile.Width - 1, workingTile.Height - 1);
                gWorkingTile.DrawLine(pen, 1, workingTile.Height - 2, 0, workingTile.Height - 1);
            }

            if (cornersize == 5)
            {
                //Upper Left
                gWorkingTile.DrawLine(pen, 1, 1, 2, 1);
                gWorkingTile.DrawLine(pen, 1, 1, 1, 2);
                //Upper Right
                gWorkingTile.DrawLine(pen, workerTile.Width -2 , 1, workerTile.Width - 3, 1);
                gWorkingTile.DrawLine(pen, workerTile.Width - 2, 1, workerTile.Width - 2, 2);
                //Lower Right
                gWorkingTile.DrawLine(pen, workingTile.Width - 2, workingTile.Height - 2, workingTile.Width - 2, workingTile.Height - 3);
                gWorkingTile.DrawLine(pen, workingTile.Width - 2, workingTile.Height - 2, workingTile.Width - 3, workingTile.Height - 2);
                //Lower Left
                gWorkingTile.DrawLine(pen, 1, workingTile.Height - 2, 1, workingTile.Height - 3);
                gWorkingTile.DrawLine(pen, 1, workingTile.Height - 2, 3, workingTile.Height - 2);
            }
            workingTile.Image = workerTile;
            workingTile.Invalidate();
            workingTile.Update();
        }
        private void btnReset_Click(object sender, EventArgs e)                                        
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Reset Button");
            }
            else
            {
                FontFamily fontFamily;
                fontFamily = new FontFamily(this.Font.Name);
                txtText.Font = new Font(fontFamily, txtText.Font.Size, txtText.Font.Style);

                toolFontNameButton.Text = this.Font.Name;
                toolFontSizeButton.Text = this.Font.Size.ToString();

                txtText.Text = "";
                txtText.Font = this.Font;
                txtText.ForeColor = Color.Black;
                txtText.BackColor = workingSmallTile.BackColor;
                vScrollText.Minimum = -5;
                vScrollText.Maximum = workingTile.Height;
                hScrollText.Minimum = -5;
                hScrollText.Maximum = workingTile.Width;
                vScrollText.Value = 0;
                hScrollText.Value = 0;
                hScrollRotate.Value = 0;
                lblTextPlacement.Text = "0,0";
                txtRotate.Text = "0";
                chkIncrement.Checked = false;
                drawRotateAngle();
                toolFontBoldButton.Checked = false;
                toolAntiAliasButton1.Checked = false;
                toolFontItalicButton.Checked = false;
                btnHideControls.Visible = false;
                txtText.Focus();
            }
        }
        static Image FixedSize(Image imgPhoto, int Width, int Height)                                  
        {
            int destWidth = Width;
            int destHeight = Height;
            Bitmap bmPhoto = null;

            bmPhoto = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(0, 0, Width, Height),
                new Rectangle(0, 0, imgPhoto.Width, imgPhoto.Height),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();

            return bmPhoto;
        }
        private void btnResize_Click(object sender, EventArgs e)                                       
        {
            int bordersize = Convert.ToInt16(cboBorderSize.Text);

            Image workerTile;
            Bitmap bmpTile;
            workerTile = null;

            workerTile = (Image)workingFullTile.Image.Clone();
            bmpTile = (Bitmap)workingFullTile.Image.Clone();

            UpdateWorkingTileGraphics(workerTile);

            LinearGradientBrush linearBrush;

            float[] r1Positions = { 0.0f, 1.0f };
            float[] r1Intensitives = { 0.5f, 1.0f };
            Blend blend1 = new Blend();
            blend1.Factors = r1Positions;
            blend1.Positions = r1Positions;


            for (int y = bordersize; y < workerTile.Height - bordersize; y++)
            {
                linearBrush = new LinearGradientBrush(new Point(0, y),
                                                      new Point(bordersize, y),
                                                      Color.Beige,
                                                      bmpTile.GetPixel(bordersize, y));
                linearBrush.Blend = blend1;
                gWorkingTile.FillRectangle(linearBrush, 
                                           new Rectangle(new Point(0, y), 
                                           new Size(bordersize, 1)));
            }

            float[] r2Positions = { 0.0f, 1.0f };
            float[] r2Intensitives = { 0.5f, 1.0f };
            Blend blend2 = new Blend();
            blend2.Factors = r2Intensitives;
            blend2.Positions = r2Positions;
            for (int x = bordersize; x < workerTile.Width - bordersize; x++)
            {
                linearBrush = new LinearGradientBrush(new Point(x, 0),
                                                      new Point(x, bordersize),
                                                      Color.Black,
                                                      bmpTile.GetPixel(x, bordersize));
                linearBrush.Blend = blend2;
                gWorkingTile.FillRectangle(linearBrush, 
                                           new Rectangle(new Point(x, 0), 
                                           new Size(1, bordersize)));
            }

            float[] r3Positions = { 0.0f, 1.0f };
            float[] r3Intensitives = { 0.0f, 0.5f };
            Blend blend3 = new Blend();
            blend3.Factors = r3Intensitives;
            blend3.Positions = r3Positions;
            for (int y = bordersize; y < workerTile.Height - bordersize; y++)
            {
                linearBrush = new LinearGradientBrush(new Point(workerTile.Width - bordersize, y),
                                                      new Point(workerTile.Width, y),
                                                      bmpTile.GetPixel(workerTile.Width - bordersize, y),
                                                      Color.Black);
                linearBrush.Blend = blend3;
                gWorkingTile.FillRectangle(linearBrush, 
                                           new Rectangle(new Point(workerTile.Width - bordersize, y),
                                           new Size(bordersize, 1)));
            }

            float[] r4Positions = { 0.0f, 1.0f };
            float[] r4Intensitives = { 1.0f, 0.5f };
            Blend blend4 = new Blend();
            blend4.Factors = r4Intensitives;
            blend4.Positions = r4Positions;
            for (int x = bordersize; x < workerTile.Width - bordersize; x++)
            {
                linearBrush = new LinearGradientBrush(new Point(x, workerTile.Height - bordersize),
                                                      new Point(x, workerTile.Height),
                                                      bmpTile.GetPixel(x, workerTile.Height - bordersize),
                                                      Color.Beige);
                linearBrush.Blend = blend4;
                gWorkingTile.FillRectangle(linearBrush, 
                                           new Rectangle(new Point(x, workerTile.Height - bordersize),
                                           new Size(1, bordersize)));
            }
            centerWorkingTile();
            resizeWorkingArea();
            if (pctZoom.Visible)
                paintZoom();
        }
        private void lockRotationtoolStripButton1_Click(object sender, EventArgs e)                    
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                if (lockRotationtoolStripButton1.CheckState == CheckState.Checked)
                {
                    lockRotationtoolStripButton1.Image = imageList1.Images[4];
                }
                else
                {
                    lockRotationtoolStripButton1.Image = imageList1.Images[3];
                }
            }
        }
        private void lockFliptoolStripButton1_Click(object sender, EventArgs e)                        
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                if (lockFliptoolStripButton1.CheckState == CheckState.Checked)
                {
                    lockFliptoolStripButton1.Image = imageList1.Images[4];
                }
                else
                {
                    lockFliptoolStripButton1.Image = imageList1.Images[3];
                }
            }
        }
        private void chkIncrement_CheckedChanged(object sender, EventArgs e)                           
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Increment Check Box");
            }
        }
        private void lockCreateHalffromFullStripButton1_Click(object sender, EventArgs e)              
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                if (lockCreateHalffromFullStripButton1.CheckState == CheckState.Unchecked)
                {
                    lockCreateHalffromFullStripButton1.Image = this.imageList1.Images[5];
                }
                else
                {
                    lockCreateHalffromFullStripButton1.Image = this.imageList1.Images[6];
                }
            }
        }
        private void workingSmallTile_MouseClick(object sender, MouseEventArgs e)                      
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Full Size Tile");
            }
            else
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (Cursor.Current == Cursors.Default)
                    {
                        if (Cursor.Current == Cursors.Help)
                        {
                            this.Cursor = Cursors.Default;
                            Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Working Tile");
                        }
                        else
                        {
                            ColorSelectSmallTile = true;
                            this.Cursor = eyedropperCursor;
                            Cursor.Clip = workingTile.Parent.RectangleToScreen(workingTile.Bounds);
                            workingTile.Focus();
                        }
                    }
                    else
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
                else
                {
                    if (colorDialog1.ShowDialog() == DialogResult.OK)
                        workingSmallTile.BackColor = colorDialog1.Color;
                }
            }
        }
        private void workingTile_MouseDown(object sender, MouseEventArgs e)
        {
            if (Cursor.Current == Cursors.Default && e.Button == MouseButtons.Left)
            {
                isDraggingText = true;
                dragStartMouseLoc = e.Location;
                dragStartTextLoc = new Point(hScrollText.Value, vScrollText.Value);
            }
        }

        private void workingTile_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingText && e.Button == MouseButtons.Left)
            {
                int deltaX = e.X - dragStartMouseLoc.X;
                int deltaY = e.Y - dragStartMouseLoc.Y;

                int newX = dragStartTextLoc.X + deltaX;
                int newY = dragStartTextLoc.Y + deltaY;

                if (hScrollText != null)
                {
                    if (newX < hScrollText.Minimum) newX = hScrollText.Minimum;
                    if (newX > hScrollText.Maximum) newX = hScrollText.Maximum;
                    hScrollText.Value = newX;
                }
                if (vScrollText != null)
                {
                    if (newY < vScrollText.Minimum) newY = vScrollText.Minimum;
                    if (newY > vScrollText.Maximum) newY = vScrollText.Maximum;
                    vScrollText.Value = newY;
                }

                if (lblTextPlacement != null)
                {
                    lblTextPlacement.Text = hScrollText.Value.ToString() + "," + vScrollText.Value.ToString();
                }

                drawText();
            }
        }

        private void workingTile_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDraggingText)
            {
                isDraggingText = false;
                
                int deltaX = Math.Abs(e.X - dragStartMouseLoc.X);
                int deltaY = Math.Abs(e.Y - dragStartMouseLoc.Y);
                if (deltaX < 3 && deltaY < 3)
                {
                    int newX = e.X;
                    int newY = e.Y;

                    if (hScrollText != null)
                    {
                        if (newX < hScrollText.Minimum) newX = hScrollText.Minimum;
                        if (newX > hScrollText.Maximum) newX = hScrollText.Maximum;
                        hScrollText.Value = newX;
                    }
                    if (vScrollText != null)
                    {
                        if (newY < vScrollText.Minimum) newY = vScrollText.Minimum;
                        if (newY > vScrollText.Maximum) newY = vScrollText.Maximum;
                        vScrollText.Value = newY;
                    }

                    if (lblTextPlacement != null)
                    {
                        lblTextPlacement.Text = hScrollText.Value.ToString() + "," + vScrollText.Value.ToString();
                    }

                    drawText();
                }
            }
        }

        private void workingTile_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (Cursor.Current == Cursors.Default && e.Button == MouseButtons.Left)
            {
                if (lblCurrentWorkingTile.Text == currentworkingtile.FullTile.ToString())
                {
                    workingFullTile.Width = workingTile.Width;
                    workingFullTile.Height = workingTile.Height;
                    workingFullTile.Image = (Image)workingTile.Image.Clone();

                    if (lockCreateHalffromFullStripButton1.CheckState == CheckState.Unchecked)
                    {
                        workingHalfTile.Width = (workingFullTile.Width / 2) + 1;
                        workingHalfTile.Height = (workingFullTile.Height / 2) + 1;
                        workingHalfTile.Image = FixedSize(workingTile.Image, workingHalfTile.Width, workingHalfTile.Height);
                    }
                }
                if (lblCurrentWorkingTile.Text == currentworkingtile.HalfTile.ToString())
                {
                    workingHalfTile.Width = workingTile.Width;
                    workingHalfTile.Height = workingTile.Height;
                    workingHalfTile.Image = workingTile.Image;
                }
                resizeWorkingArea();
                workingTile.Focus();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }

        private void workingTile_MouseClick(object sender, MouseEventArgs e)                           
        {
            if (Cursor.Current != Cursors.Default)
            {
                if (Cursor.Current == Cursors.Help)
                {
                    this.Cursor = Cursors.Default;
                    Help.ShowHelp(this, "gtleditor.chm",HelpNavigator.KeywordIndex, "Working Tile");
                }
                else
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        if (ColorSelectSmallTile)
                        {
                            Color grabColor = new Color();
                            Bitmap grabBitmap = (Bitmap)workingTile.Image.Clone();
                            grabColor = grabBitmap.GetPixel(e.X, e.Y);
                            workingSmallTile.BackColor = grabColor;
                            txtText.BackColor = grabColor;
                        }
                        else
                        {
                            Color grabColor = new Color();
                            Bitmap grabBitmap = (Bitmap)workingTile.Image.Clone();
                            grabColor = grabBitmap.GetPixel(e.X, e.Y);
                            pctTransparentColor.BackColor = grabColor;
                        }
                    }
                    this.Cursor = Cursors.Default;
                    Cursor.Clip = Screen.PrimaryScreen.Bounds;
                }
            }
        }
        private void workingTile_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)              
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Cursor = Cursors.Default;
                Cursor.Clip = Screen.PrimaryScreen.Bounds;
            }
        }
        private void placeTexttoolStripMenuItem_Click(object sender, EventArgs e)                      
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            drawText();
        }
        private void btnStretch_Click(object sender, EventArgs e)                                      
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Stretch Button");
            }
            else
            {
                Bitmap bmp = new Bitmap((Image)workingTile.Image.Clone());
                Image thumbnail = bmp.GetThumbnailImage(Convert.ToInt16(txtWidth.Text), Convert.ToInt16(txtHeight.Text), null, IntPtr.Zero);
                Bitmap image;
                image = new Bitmap(thumbnail); 
                //workingTile.Image = image.GetThumbnailImage(Convert.ToInt16(txtWidth.Text), Convert.ToInt16(txtHeight.Text), null, IntPtr.Zero);
                workingTile.Image = image;
                workingTile.Width = workingTile.Image.Width;
                workingTile.Height = workingTile.Image.Height;

                this.txtWidth.Text = workingTile.Width.ToString();
                this.txtHeight.Text = workingTile.Height.ToString();
                this.lblTileSize.Text = workingTile.Width.ToString() + "," + workingTile.Height.ToString();

                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
                workingTile.Focus();
            }
        }
        private void btnResize_Click2(object sender, EventArgs e)                                      
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Resize Button");
            }
            else
            {
                if ((Convert.ToInt16(txtWidth.Text) < workingTile.Width) && (Convert.ToInt16(txtHeight.Text) < workingTile.Height))
                {
                    this.Cursor = Cursors.WaitCursor;
                    if (workingTile.Width > 2 && workingTile.Height > 2)
                    {
                        Image workerTile;
                        workerTile = (Image)workingTile.Image.Clone();
                        Bitmap iBitmap = (Bitmap)workerTile;

                        workingTile.BackColor = Color.Black;
                        gWorkingTile = ((Control)workingTile).CreateGraphics();

                        Bitmap gBitmap = new Bitmap(Convert.ToInt16(txtWidth.Text), Convert.ToInt16(txtHeight.Text), gWorkingTile);

                        Color c = new Color();

                        int startX = Convert.ToInt16((workingTile.Width - Convert.ToInt16(txtWidth.Text)) * 0.5);
                        int startY = Convert.ToInt16((workingTile.Height - Convert.ToInt16(txtHeight.Text)) * 0.5);

                        for (int X = 0; X < gBitmap.Width; X++)
                        {
                            for (int Y = 0; Y < gBitmap.Height; Y++)
                            {
                                c = iBitmap.GetPixel(X + startX, Y + startY);
                                gBitmap.SetPixel(X, Y, c);
                            }
                        }
                        workingTile.Width = gBitmap.Width; ;
                        workingTile.Height = gBitmap.Height; ;
                        workingTile.Image = (Image)gBitmap.Clone();

                        this.txtWidth.Text = workingTile.Width.ToString();
                        this.txtHeight.Text = workingTile.Height.ToString();
                        lblTileSize.Text = workingTile.Width.ToString() + "," + workingTile.Height.ToString();

                        gWorkingTile.Dispose();
                        workingTile.Invalidate();
                        workingTile.Update();

                        centerWorkingTile();
                        resizeWorkingArea();
                        if (pctZoom.Visible)
                            paintZoom();
                        workingTile.Focus();
                    }
                }
                if ((Convert.ToInt16(txtWidth.Text) > workingTile.Width) && (Convert.ToInt16(txtHeight.Text) > workingTile.Height))
                {
                    this.Cursor = Cursors.WaitCursor;
                    Image workerTile;
                    workerTile = (Image)workingTile.Image.Clone();
                    Bitmap iBitmap = (Bitmap)workerTile;

                    workingTile.BackColor = Color.Black;
                    gWorkingTile = ((Control)workingTile).CreateGraphics();

                    Bitmap gBitmap = new Bitmap(Convert.ToInt16(txtWidth.Text), Convert.ToInt16(txtHeight.Text), gWorkingTile);

                    Color c = new Color();

                    int borderX = Convert.ToInt16((Convert.ToInt16(txtWidth.Text) - workingTile.Width) * 0.5);
                    int borderY = Convert.ToInt16((Convert.ToInt16(txtHeight.Text) - workingTile.Height) * 0.5);
                    int buffer = Convert.ToInt16(cboClear.Text);

                    for (int X = 0; X < gBitmap.Width; X++)
                    {
                        for (int Y = 0; Y < gBitmap.Height; Y++)
                        {
                            if ((X >= 0 + borderX && X < workingTile.Width + borderX) &&
                                (Y >= 0 + borderY && Y < workingTile.Height + borderY))
                            {
                                c = iBitmap.GetPixel(X - borderX, Y - borderY);
                            }
                            else
                            {
                                c = workingSmallTile.BackColor;
                            }
                            gBitmap.SetPixel(X, Y, c);
                        }
                    }

                    workingTile.Width = gBitmap.Width; ;
                    workingTile.Height = gBitmap.Height;
                    workingTile.Image = (Image)gBitmap.Clone();

                    lblTileSize.Text = workingTile.Width.ToString() + "," + workingTile.Height.ToString();

                    gWorkingTile.Dispose();
                    workingTile.Invalidate();
                    workingTile.Update();

                    centerWorkingTile();
                    resizeWorkingArea();
                    if (pctZoom.Visible)
                        paintZoom();
                }
                this.Cursor = Cursors.Default;
            }
        }
        private void btnClear_Click(object sender, EventArgs e)                                        
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Clear Button");
            }
            else
            {
                Image workerTile;
                workerTile = (Image)workingTile.Image.Clone();
                Bitmap iBitmap = (Bitmap)workerTile;

                gWorkingTile = ((Control)workingTile).CreateGraphics();

                Bitmap gBitmap = new Bitmap(workingTile.Width, workingTile.Height, gWorkingTile);

                Color c = new Color();
                c = workingSmallTile.BackColor;

                int buffer = Convert.ToInt16(cboClear.Text);

                for (int X = 0; X < workingTile.Width; X++)
                {
                    for (int Y = 0; Y < workingTile.Height; Y++)
                    {
                        if ((X >= 0 + buffer && X < workingTile.Width - buffer) &&
                            (Y >= 0 + buffer && Y < workingTile.Height - buffer))
                        {
                            c = iBitmap.GetPixel(X, Y);
                        }
                        else
                        {
                            c = workingSmallTile.BackColor;
                        }
                        gBitmap.SetPixel(X, Y, c);
                    }
                }
                workingTile.Image = (Image)gBitmap;

                gWorkingTile.Dispose();
                workingTile.Invalidate();
                workingTile.Update();
                workingTile.Focus();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void txtWidth_Validating(object sender, CancelEventArgs e)                             
        {
            string errorMsg;
            if (!ValidNumber(txtWidth.Text, out errorMsg))
            {
                e.Cancel = true;
                txtWidth.Select(0, txtWidth.Text.Length);
                this.errorProvider1.SetError(txtWidth, errorMsg);
            }
        }
        private void txtWidth_Validated(object sender, EventArgs e)                                    
        {
            errorProvider1.SetError(txtWidth, "");
            Single ratio = Convert.ToSingle(txtWidth.Text) / workingTile.Width;
            txtHeight.Text = Convert.ToInt16(workingTile.Height * ratio).ToString();
        }
        private void txtHeight_Validating(object sender, CancelEventArgs e)                            
        {
            string errorMsg;
            if (!ValidNumber(txtHeight.Text, out errorMsg))
            {
                e.Cancel = true;
                txtHeight.Select(0, txtHeight.Text.Length);
                this.errorProvider1.SetError(txtHeight, errorMsg);
            }
        }
        private void txtHeight_Validated(object sender, EventArgs e)                                   
        {
            errorProvider1.SetError(txtHeight, "");
            Single ratio = Convert.ToSingle(txtHeight.Text) / workingTile.Height;
            txtWidth.Text = Convert.ToInt16(workingTile.Width * ratio).ToString();
        }
        private Boolean ValidNumber(string txtNumber, out string errorMessage)                         
        {
            try
            {
                int intText = Convert.ToInt16(txtNumber);
                if (intText < 2)
                {
                    errorMessage = "2 or greater";
                    return false;
                }
                else
                {
                    errorMessage = "";
                    return true;
                }
            }
            catch
            {
                errorMessage = "2 or greater";
                return false;
            }
        }
        private Boolean ValidAngle(string txtAngle, out string errorMessage)                           
        {
            try
            {
                int intText = Convert.ToInt16(txtAngle);
                if (intText < 0 || intText > 360)
                {
                    errorMessage = "0 to 360";
                    return false;
                }
                else
                {
                    errorMessage = "";
                    return true;
                }
            }
            catch
            {
                errorMessage = "0 to 360";
                return false;
            }
        }
        private void txtRotate_Validating(object sender, CancelEventArgs e)                            
        {
            string errorMsg;
            if (!ValidAngle(txtRotate.Text, out errorMsg))
            {
                e.Cancel = true;
                txtRotate.Select(0, txtRotate.Text.Length);
                this.errorProvider1.SetError(txtRotate, errorMsg);
            }
        }
        private void txtRotate_Validated(object sender, EventArgs e)                                   
        {
            errorProvider1.SetError(txtRotate, "");
            hScrollRotate.Value = Convert.ToInt32(txtRotate.Text);
            drawRotateAngle();
            drawText();
        }
        private void cboBorderSize_MouseClick(object sender, MouseEventArgs e)                         
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Bevel Button");
            }
        }
        private void cboCornerSize_MouseClick(object sender, MouseEventArgs e)                         
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Clip Button");
            }
        }
        private void cboClear_MouseClick(object sender, MouseEventArgs e)                              
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Clear Button");
            }
        }
        private void lblTileSize_Click(object sender, EventArgs e)                                     
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Size Window");
            }
        }
        private void lblTextPlacement_Click(object sender, EventArgs e)                                
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Text Location Window");
            }
        }
        private void pctRotate_Click(object sender, EventArgs e)                                       
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Rotation Scroll Bar and Display");
            }
        }
        private void HowtotoolStripMenuItem_Click(object sender, EventArgs e)                          
        {
            Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "How to...");
        }
        private void toolFontNameButton_Click(object sender, EventArgs e)                              
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Font Tools");
            }
        }
        private void toolFontSizeButton_Click(object sender, EventArgs e)                              
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Font Tools");
            }
        }
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)                           
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Help Menu");
            }
        }
        private void fileToolStripMenuItem_Click(object sender, EventArgs e)                           
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "File Menu");
            }
        }
        private void editToolStripMenuItem_Click(object sender, EventArgs e)                           
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Edit Menu");
            }
        }
        private void texttoolStripMenuItem_Click(object sender, EventArgs e)                           
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Text Menu");
            }
        }
        private void tiletoolStripMenuItem_Click(object sender, EventArgs e)                           
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Menu");
            }
        }
        private void chkIncrement_Click(object sender, EventArgs e)                                    
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Increment Check Box");
            }
        }
        private void contentstoolStripMenuItem_Click(object sender, EventArgs e)                       
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            Help.ShowHelp(this, "gtleditor.chm");
        }
        private void txtText_Click(object sender, EventArgs e)                                         
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Text Entry Box");
            }
        }
        private void txtRotate_Click(object sender, EventArgs e)                                       
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Rotation Value Box");
            }
        }
        private void pctTransparentColor_MouseClick(object sender, MouseEventArgs e)                   
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Transparent Color Box");
            }
            else
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (Cursor.Current == Cursors.Default)
                    {
                        if (Cursor.Current == Cursors.Help)
                        {
                            this.Cursor = Cursors.Default;
                            Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Transparent Color Box");
                        }
                        else
                        {
                            ColorSelectSmallTile = false;
                            this.Cursor = eyedropperCursor;
                            Cursor.Clip = workingTile.Parent.RectangleToScreen(workingTile.Bounds);
                            workingTile.Focus();
                        }
                    }
                    else
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
                else
                {
                    if (colorDialog1.ShowDialog() == DialogResult.OK)
                        pctTransparentColor.BackColor = colorDialog1.Color;
                }
            }
        }
        private void label2_Click(object sender, EventArgs e)                                          
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Transparent Color Box");
            }
        }
        private void btnBumpUp_Click(object sender, EventArgs e)                                       
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Shift Buttons");
            }
            else
            {
                Image workerTile;
                workerTile = (Image)workingTile.Image.Clone();
                Bitmap iBitmap = (Bitmap)workerTile;

                gWorkingTile = ((Control)workingTile).CreateGraphics();

                Bitmap gBitmap = new Bitmap(workingTile.Width, workingTile.Height, gWorkingTile);

                Color c = new Color();
                c = workingSmallTile.BackColor;

                for (int X = 0; X < workingTile.Width; X++)
                {
                    for (int Y = 0; Y < workingTile.Height; Y++)
                    {
                        if(Y < workingTile.Height - 1)
                        {
                            c = iBitmap.GetPixel(X, Y + 1);
                        }
                        else
                        {
                            c = workingSmallTile.BackColor;
                        }
                        gBitmap.SetPixel(X, Y, c);
                    }
                }
                workingTile.Width = Convert.ToInt16(txtWidth.Text);
                workingTile.Height = Convert.ToInt16(txtHeight.Text);
                workingTile.Image = (Image)gBitmap;

                gWorkingTile.Dispose();
                workingTile.Invalidate();
                workingTile.Update();
                workingTile.Focus();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void btnBumpRight_Click(object sender, EventArgs e)                                    
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Shift Buttons");
            }
            else
            {
                Image workerTile;
                workerTile = (Image)workingTile.Image.Clone();
                Bitmap iBitmap = (Bitmap)workerTile;

                gWorkingTile = ((Control)workingTile).CreateGraphics();

                Bitmap gBitmap = new Bitmap(workingTile.Width, workingTile.Height, gWorkingTile);

                Color c = new Color();
                c = workingSmallTile.BackColor;

                for (int X = workingTile.Width - 1; X >= 0; X--)
                {
                    for (int Y = 0; Y < workingTile.Height; Y++)
                    {
                        if (X > 0)
                        {
                            c = iBitmap.GetPixel(X - 1, Y);
                        }
                        else
                        {
                            c = workingSmallTile.BackColor;
                        }
                        gBitmap.SetPixel(X, Y, c);
                    }
                }
                workingTile.Width = Convert.ToInt16(txtWidth.Text);
                workingTile.Height = Convert.ToInt16(txtHeight.Text);
                workingTile.Image = (Image)gBitmap;

                gWorkingTile.Dispose();
                workingTile.Invalidate();
                workingTile.Update();
                workingTile.Focus();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void btnBumpDown_Click(object sender, EventArgs e)                                     
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Shift Buttons");
            }
            else
            {
                Image workerTile;
                workerTile = (Image)workingTile.Image.Clone();
                Bitmap iBitmap = (Bitmap)workerTile;

                gWorkingTile = ((Control)workingTile).CreateGraphics();

                Bitmap gBitmap = new Bitmap(workingTile.Width, workingTile.Height, gWorkingTile);

                Color c = new Color();
                c = workingSmallTile.BackColor;

                for (int X = 0; X < workingTile.Width; X++)
                {
                    for (int Y = 0; Y < workingTile.Height; Y++ )
                    {
                        if (Y > 0)
                        {
                            c = iBitmap.GetPixel(X, Y - 1);
                        }
                        else
                        {
                            c = workingSmallTile.BackColor;
                        }
                        gBitmap.SetPixel(X, Y, c);
                    }
                }
                workingTile.Width = Convert.ToInt16(txtWidth.Text);
                workingTile.Height = Convert.ToInt16(txtHeight.Text);
                workingTile.Image = (Image)gBitmap;

                gWorkingTile.Dispose();
                workingTile.Invalidate();
                workingTile.Update();
                workingTile.Focus();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void btnBumpLeft_Click(object sender, EventArgs e)                                     
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Shift Buttons");
            }
            else
            {
                Image workerTile;
                workerTile = (Image)workingTile.Image.Clone();
                Bitmap iBitmap = (Bitmap)workerTile;

                gWorkingTile = ((Control)workingTile).CreateGraphics();

                Bitmap gBitmap = new Bitmap(workingTile.Width, workingTile.Height, gWorkingTile);

                Color c = new Color();
                c = workingSmallTile.BackColor;

                for (int X = 0; X < workingTile.Width; X++)
                {
                    for (int Y = 0; Y < workingTile.Height; Y++)
                    {
                        if (X < workingTile.Width - 1)
                        {
                            c = iBitmap.GetPixel(X + 1, Y);
                        }
                        else
                        {
                            c = workingSmallTile.BackColor;
                        }
                        gBitmap.SetPixel(X, Y, c);
                    }
                }
                workingTile.Width = Convert.ToInt16(txtWidth.Text);
                workingTile.Height = Convert.ToInt16(txtHeight.Text);
                workingTile.Image = (Image)gBitmap;

                gWorkingTile.Dispose();
                workingTile.Invalidate();
                workingTile.Update();
                workingTile.Focus();
                if (pctZoom.Visible)
                    paintZoom();

            }
        }
        private void StoreTexttoolStripMenuItem_Click_1(object sender, EventArgs e)                    
        {
            this.Cursor = Cursors.Default;
            workingTile.Focus();
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Store Text Button");
            }
            else
            {
                if (chkIncrement.Checked == true)
                {
                    Regex _Regex = new Regex(@"#");
                    Match m = _Regex.Match(txtText.Text);
                    if (!m.Success)
                    {
                        if (MessageBox.Show("The increment feature requires the format '#d', example: (A/#1/1).", "Danger Will Robinson!", MessageBoxButtons.OK) == DialogResult.OK)
                        {
                            chkIncrement.Checked = false;
                        }
                    }
                    else
                    {
                        Point newPoint = new Point(hScrollText.Value, vScrollText.Value);
                        TextBox newTextbox = new TextBox();
                        newTextbox = txtText;
                        newTextbox.Width = txtText.Width;
                        PositionDisplay.addPosition(newTextbox,
                                                    chkIncrement,
                                                    newPoint,
                                                    Convert.ToInt32(txtRotate.Text),
                                                    lblCurrentWorkingTile.Text);

                        chkIncrement.Checked = false;

                        SyncvScrollBar2();
                    }
                }
                else
                {
                    Point newPoint = new Point(hScrollText.Value, vScrollText.Value);
                    TextBox newTextbox = new TextBox();
                    newTextbox = txtText;
                    newTextbox.Width = txtText.Width;
                    PositionDisplay.addPosition(newTextbox,
                                                chkIncrement,
                                                newPoint,
                                                Convert.ToInt32(txtRotate.Text),
                                                lblCurrentWorkingTile.Text);

                    chkIncrement.Checked = false;

                    SyncvScrollBar2();
                }
            }
            workingTile.Focus();
        }
        private void createTiletoolStripMenuItem4_Click(object sender, EventArgs e)                    
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Tile Tools");
            }
            else
            {
                GTL_File.gtltile newTile = new GTL_File.gtltile();
                newTile.FullTile = (Bitmap)workingFullTile.Image;
                newTile.HalfTile = (Bitmap)workingHalfTile.Image;
                newTile.SmallTile = workingSmallTile.BackColor;
                TileDisplay.AddTile(newTile);
                SyncvScrollBar1();
                if (TileDisplay.Count > 1)
                {
                    deleteTileToolStripMenuItem.Enabled = true;
                    deletetoolStripButton1.Enabled = true;
                }
                lblCurrentWorkingTile.Text = "FullTile";

                workingTile.Width = TileDisplay[TileDisplay.CurrentTile].fullTile.Width;
                workingTile.Height = TileDisplay[TileDisplay.CurrentTile].fullTile.Height;
                workingTile.Image = TileDisplay[TileDisplay.CurrentTile].fullTile.Image;

                workingFullTile.Width = TileDisplay[TileDisplay.CurrentTile].fullTile.Width;
                workingFullTile.Height = TileDisplay[TileDisplay.CurrentTile].fullTile.Height;
                workingFullTile.Image = TileDisplay[TileDisplay.CurrentTile].fullTile.Image;

                workingHalfTile.Width = TileDisplay[TileDisplay.CurrentTile].halfTile.Width;
                workingHalfTile.Height = TileDisplay[TileDisplay.CurrentTile].halfTile.Height;
                workingHalfTile.Image = TileDisplay[TileDisplay.CurrentTile].halfTile.Image;

                workingSmallTile.BackColor = TileDisplay[TileDisplay.CurrentTile].smallTile.BackColor;

                if (TileDisplay.Count == 1)
                {
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tile";
                }
                else
                {
                    this.toolStripStatusLabel1.Text = TileDisplay.Count.ToString() + " tiles";
                }
                txtWidth.Text = workingTile.Width.ToString();
                txtHeight.Text = workingTile.Height.ToString();
                centerWorkingTile();
                resizeWorkingArea();
                if (pctZoom.Visible)
                    paintZoom();
            }
        }
        private void ZoomtoolStripMenuItem_Click(object sender, EventArgs e)                           
        {
            if (ZoomtoolStripMenuItem.Text == "Show Zoom")
            {
                ZoomtoolStripMenuItem.Text = "Hide Zoom";
                this.backgroundWorker1.RunWorkerAsync();
                pctZoom.Visible = true;
            }
            else
            {
                ZoomtoolStripMenuItem.Text = "Show Zoom";
                pctZoom.Image = null;
                pctZoom.Visible = false;
            }
        }
        private void paintZoom()                                                                       
        {
            if (workingTile?.Image == null || pctZoom == null || groupBox4 == null || groupBox2 == null)
            {
                return;
            }
            Image tempImage = Bitmap_Utilities.Zoom(workingTile.Image);
            pctZoom.Width = tempImage.Width;
            pctZoom.Height = tempImage.Height;
            pctZoom.Top = groupBox4.Bottom - pctZoom.Height;
            pctZoom.Left = groupBox2.Right - pctZoom.Width;
            pctZoom.Image = tempImage;
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            paintZoom();
        }
        private void pctZoom_Click(object sender, EventArgs e)                                         
        {
            if (Cursor.Current == Cursors.Help)
            {
                this.Cursor = Cursors.Default;
                if (workingTile != null) workingTile.Focus();
                Help.ShowHelp(this, "gtleditor.chm", HelpNavigator.KeywordIndex, "Zoomed Working Tile Image");
            }
        }
        private void txtText_Validated(object sender, EventArgs e)                                     
        {
            if (toolFontNameButton != null && toolFontSizeButton != null && txtText != null)
            {
                this.toolFontNameButton.Text = txtText.Font.Name;
                this.toolFontSizeButton.Text = txtText.Font.Size.ToString();
            }
            drawText();
        }
        private void txtText_SizeChanged(object sender, EventArgs e)                                   
        {
            if (toolFontNameButton != null && toolFontSizeButton != null && txtText != null)
            {
                this.toolFontNameButton.Text = txtText.Font.Name;
                this.toolFontSizeButton.Text = txtText.Font.Size.ToString();
            }
            drawText();
        }
        private void txtText_FontChanged(object sender, EventArgs e)                                   
        {
            if (toolFontNameButton != null && toolFontSizeButton != null && txtText != null)
            {
                this.toolFontNameButton.Text = txtText.Font.Name;
                this.toolFontSizeButton.Text = txtText.Font.Size.ToString();
            }
            drawText();
        }
        private void txtText_BackColorChanged(object sender, EventArgs e)                              
        {
            if (toolFontNameButton != null && toolFontSizeButton != null && txtText != null)
            {
                this.toolFontNameButton.Text = txtText.Font.Name;
                this.toolFontSizeButton.Text = txtText.Font.Size.ToString();
            }
            drawText();
        }
        private void txtText_StyleChanged(object sender, EventArgs e)                                  
        {
            if (toolFontNameButton != null && toolFontSizeButton != null && txtText != null)
            {
                this.toolFontNameButton.Text = txtText.Font.Name;
                this.toolFontSizeButton.Text = txtText.Font.Size.ToString();
            }
            drawText();
        }
        private void lblTileSize_TextChanged(object sender, EventArgs e)                               
        {
            if (toolFontNameButton != null && toolFontSizeButton != null && txtText != null)
            {
                this.toolFontNameButton.Text = txtText.Font.Name;
                this.toolFontSizeButton.Text = txtText.Font.Size.ToString();
            }
            drawText();
        }
        private void lblTextPlacement_TextChanged(object sender, EventArgs e)                          
        {
            if (toolFontNameButton != null && toolFontSizeButton != null && txtText != null)
            {
                this.toolFontNameButton.Text = txtText.Font.Name;
                this.toolFontSizeButton.Text = txtText.Font.Size.ToString();
            }
            drawText();
        }
        private void toolAntiAliasButton1_Click(object sender, EventArgs e)                            
        {
            if (toolAntiAliasButton1.Checked)
            {
                toolAntiAliasButton1.Image = this.imageList1.Images[12];
            }
            else
            {
                toolAntiAliasButton1.Image = this.imageList1.Images[13];
            }
            drawText();
        }
    }

    public class HighContrastVScrollBar : VScrollBar
    {
        private bool isHovered = false;
        private bool isDragging = false;

        public HighContrastVScrollBar()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            isDragging = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isDragging = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnValueChanged(EventArgs e)
        {
            base.OnValueChanged(e);
            Invalidate();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            const int WM_PAINT = 0x000F;
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_LBUTTONUP = 0x0202;
            const int WM_VSCROLL = 0x0115;

            if (m.Msg == WM_PAINT || m.Msg == WM_MOUSEMOVE || m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONUP || m.Msg == WM_VSCROLL)
            {
                CustomPaint();
            }
        }

        private void CustomPaint()
        {
            if (Width <= 0 || Height <= 0 || !IsHandleCreated) return;

            using (Graphics g = Graphics.FromHwnd(Handle))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                Rectangle client = ClientRectangle;

                // 1. Distinct track fill
                Color trackColor = Enabled ? Color.FromArgb(232, 235, 239) : Color.FromArgb(245, 245, 247);
                using (SolidBrush trackBrush = new SolidBrush(trackColor))
                {
                    g.FillRectangle(trackBrush, client);
                }

                // 2. Arrow buttons
                int arrowSize = Math.Min(Width, 22);
                Rectangle topArrowRect = new Rectangle(0, 0, Width, arrowSize);
                Rectangle bottomArrowRect = new Rectangle(0, Height - arrowSize, Width, arrowSize);

                Color arrowBg = Enabled ? (isHovered ? Color.FromArgb(218, 223, 230) : Color.FromArgb(226, 230, 236)) : Color.FromArgb(245, 245, 247);
                using (SolidBrush abBrush = new SolidBrush(arrowBg))
                {
                    g.FillRectangle(abBrush, topArrowRect);
                    g.FillRectangle(abBrush, bottomArrowRect);
                }

                // Arrow glyphs
                Color glyphColor = Enabled ? Color.FromArgb(50, 54, 62) : Color.FromArgb(170, 175, 182);
                using (SolidBrush glyphBrush = new SolidBrush(glyphColor))
                {
                    float cx = Width / 2f;
                    float tMargin = arrowSize * 0.32f;
                    PointF[] topArrow = new PointF[]
                    {
                        new PointF(cx, tMargin),
                        new PointF(cx - (arrowSize * 0.28f), arrowSize - tMargin),
                        new PointF(cx + (arrowSize * 0.28f), arrowSize - tMargin)
                    };
                    g.FillPolygon(glyphBrush, topArrow);

                    float bTop = bottomArrowRect.Top;
                    PointF[] bottomArrow = new PointF[]
                    {
                        new PointF(cx, bTop + arrowSize - tMargin),
                        new PointF(cx - (arrowSize * 0.28f), bTop + tMargin),
                        new PointF(cx + (arrowSize * 0.28f), bTop + tMargin)
                    };
                    g.FillPolygon(glyphBrush, bottomArrow);
                }

                // 3. High-contrast Thumb
                if (Enabled)
                {
                    int trackHeight = Height - (2 * arrowSize);
                    int range = Maximum - Minimum;
                    if (range > 0 && trackHeight > 0)
                    {
                        int minThumb = Math.Min(26, trackHeight);
                        int thumbHeight = Math.Max(minThumb, (int)((float)LargeChange / (range + LargeChange) * trackHeight));
                        if (thumbHeight > trackHeight) thumbHeight = trackHeight;

                        int availableTrack = trackHeight - thumbHeight;
                        int effectiveMax = Math.Max(1, range - LargeChange + 1);
                        int valClamped = Math.Max(Minimum, Math.Min(effectiveMax + Minimum, Value));
                        int thumbY = topArrowRect.Bottom + (int)((float)(valClamped - Minimum) / effectiveMax * availableTrack);

                        if (thumbY + thumbHeight > bottomArrowRect.Top) thumbY = bottomArrowRect.Top - thumbHeight;
                        if (thumbY < topArrowRect.Bottom) thumbY = topArrowRect.Bottom;

                        Rectangle thumbRect = new Rectangle(2, thumbY, Width - 4, thumbHeight);
                        Color thumbColor = isDragging ? Color.FromArgb(42, 48, 58) : (isHovered ? Color.FromArgb(68, 76, 90) : Color.FromArgb(92, 100, 114));

                        using (SolidBrush thumbBrush = new SolidBrush(thumbColor))
                        {
                            g.FillRectangle(thumbBrush, thumbRect);
                        }

                        // Subtle dark border on thumb
                        using (Pen thumbBorderPen = new Pen(Color.FromArgb(40, 46, 56), 1f))
                        {
                            g.DrawRectangle(thumbBorderPen, thumbRect);
                        }

                        // Thumb center grip marks (3 horizontal lines)
                        if (thumbHeight >= 20)
                        {
                            using (Pen gripPen = new Pen(Color.FromArgb(218, 224, 234), 1f))
                            {
                                int midY = thumbRect.Top + thumbRect.Height / 2;
                                int gripW = Math.Max(6, thumbRect.Width - 8);
                                int gripX = thumbRect.Left + (thumbRect.Width - gripW) / 2;
                                g.DrawLine(gripPen, gripX, midY - 3, gripX + gripW, midY - 3);
                                g.DrawLine(gripPen, gripX, midY, gripX + gripW, midY);
                                g.DrawLine(gripPen, gripX, midY + 3, gripX + gripW, midY + 3);
                            }
                        }
                    }
                }

                // 4. Clear outer border & arrow dividers
                using (Pen borderPen = new Pen(Color.FromArgb(135, 142, 153), 1))
                {
                    g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                    g.DrawLine(borderPen, 0, topArrowRect.Bottom, Width - 1, topArrowRect.Bottom);
                    g.DrawLine(borderPen, 0, bottomArrowRect.Top, Width - 1, bottomArrowRect.Top);
                }
            }
        }
    }

    public class HighContrastHScrollBar : HScrollBar
    {
        private bool isHovered = false;
        private bool isDragging = false;

        public HighContrastHScrollBar()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            isDragging = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isDragging = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnValueChanged(EventArgs e)
        {
            base.OnValueChanged(e);
            Invalidate();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            const int WM_PAINT = 0x000F;
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_LBUTTONUP = 0x0202;
            const int WM_HSCROLL = 0x0114;

            if (m.Msg == WM_PAINT || m.Msg == WM_MOUSEMOVE || m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONUP || m.Msg == WM_HSCROLL)
            {
                CustomPaint();
            }
        }

        private void CustomPaint()
        {
            if (Width <= 0 || Height <= 0 || !IsHandleCreated) return;

            using (Graphics g = Graphics.FromHwnd(Handle))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                Rectangle client = ClientRectangle;

                // 1. Distinct track fill
                Color trackColor = Enabled ? Color.FromArgb(232, 235, 239) : Color.FromArgb(245, 245, 247);
                using (SolidBrush trackBrush = new SolidBrush(trackColor))
                {
                    g.FillRectangle(trackBrush, client);
                }

                // 2. Arrow buttons
                int arrowSize = Math.Min(Height, 22);
                Rectangle leftArrowRect = new Rectangle(0, 0, arrowSize, Height);
                Rectangle rightArrowRect = new Rectangle(Width - arrowSize, 0, arrowSize, Height);

                Color arrowBg = Enabled ? (isHovered ? Color.FromArgb(218, 223, 230) : Color.FromArgb(226, 230, 236)) : Color.FromArgb(245, 245, 247);
                using (SolidBrush abBrush = new SolidBrush(arrowBg))
                {
                    g.FillRectangle(abBrush, leftArrowRect);
                    g.FillRectangle(abBrush, rightArrowRect);
                }

                // Arrow glyphs
                Color glyphColor = Enabled ? Color.FromArgb(50, 54, 62) : Color.FromArgb(170, 175, 182);
                using (SolidBrush glyphBrush = new SolidBrush(glyphColor))
                {
                    float cy = Height / 2f;
                    float lMargin = arrowSize * 0.32f;
                    PointF[] leftArrow = new PointF[]
                    {
                        new PointF(lMargin, cy),
                        new PointF(arrowSize - lMargin, cy - (arrowSize * 0.28f)),
                        new PointF(arrowSize - lMargin, cy + (arrowSize * 0.28f))
                    };
                    g.FillPolygon(glyphBrush, leftArrow);

                    float rLeft = rightArrowRect.Left;
                    PointF[] rightArrow = new PointF[]
                    {
                        new PointF(rLeft + arrowSize - lMargin, cy),
                        new PointF(rLeft + lMargin, cy - (arrowSize * 0.28f)),
                        new PointF(rLeft + lMargin, cy + (arrowSize * 0.28f))
                    };
                    g.FillPolygon(glyphBrush, rightArrow);
                }

                // 3. High-contrast Thumb
                if (Enabled)
                {
                    int trackWidth = Width - (2 * arrowSize);
                    int range = Maximum - Minimum;
                    if (range > 0 && trackWidth > 0)
                    {
                        int minThumb = Math.Min(28, trackWidth);
                        int thumbWidth = Math.Max(minThumb, (int)((float)LargeChange / (range + LargeChange) * trackWidth));
                        if (thumbWidth > trackWidth) thumbWidth = trackWidth;

                        int availableTrack = trackWidth - thumbWidth;
                        int effectiveMax = Math.Max(1, range - LargeChange + 1);
                        int valClamped = Math.Max(Minimum, Math.Min(effectiveMax + Minimum, Value));
                        int thumbX = leftArrowRect.Right + (int)((float)(valClamped - Minimum) / effectiveMax * availableTrack);

                        if (thumbX + thumbWidth > rightArrowRect.Left) thumbX = rightArrowRect.Left - thumbWidth;
                        if (thumbX < leftArrowRect.Right) thumbX = leftArrowRect.Right;

                        Rectangle thumbRect = new Rectangle(thumbX, 2, thumbWidth, Height - 4);
                        Color thumbColor = isDragging ? Color.FromArgb(42, 48, 58) : (isHovered ? Color.FromArgb(68, 76, 90) : Color.FromArgb(92, 100, 114));

                        using (SolidBrush thumbBrush = new SolidBrush(thumbColor))
                        {
                            g.FillRectangle(thumbBrush, thumbRect);
                        }

                        // Subtle dark border on thumb
                        using (Pen thumbBorderPen = new Pen(Color.FromArgb(40, 46, 56), 1f))
                        {
                            g.DrawRectangle(thumbBorderPen, thumbRect);
                        }

                        // Thumb center grip marks (3 vertical lines)
                        if (thumbWidth >= 20)
                        {
                            using (Pen gripPen = new Pen(Color.FromArgb(218, 224, 234), 1f))
                            {
                                int midX = thumbRect.Left + thumbRect.Width / 2;
                                int gripH = Math.Max(6, thumbRect.Height - 8);
                                int gripY = thumbRect.Top + (thumbRect.Height - gripH) / 2;
                                g.DrawLine(gripPen, midX - 3, gripY, midX - 3, gripY + gripH);
                                g.DrawLine(gripPen, midX, gripY, midX, gripY + gripH);
                                g.DrawLine(gripPen, midX + 3, gripY, midX + 3, gripY + gripH);
                            }
                        }
                    }
                }

                // 4. Clear outer border & arrow dividers
                using (Pen borderPen = new Pen(Color.FromArgb(135, 142, 153), 1))
                {
                    g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                    g.DrawLine(borderPen, leftArrowRect.Right, 0, leftArrowRect.Right, Height - 1);
                    g.DrawLine(borderPen, rightArrowRect.Left, 0, rightArrowRect.Left, Height - 1);
                }
            }
        }
    }
}

