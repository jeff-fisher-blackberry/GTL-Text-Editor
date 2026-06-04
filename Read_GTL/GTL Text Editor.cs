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
                {
                    SizeF glyphSize = g.MeasureString(glyph, font);
                    g.DrawString(glyph, font, new SolidBrush(color), (size - glyphSize.Width) / 2, (size - glyphSize.Height) / 2);
                }
            }
            return bmp;
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
                w = 30;
                h = 30;
            }

            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                
                g.TranslateTransform(w / 2f, h / 2f);
                g.RotateTransform(angle);
                
                using (Font font = new Font(this.Font.FontFamily, this.Font.Size, FontStyle.Regular))
                {
                    SizeF size = g.MeasureString(text, font);
                    g.DrawString(text, font, Brushes.Black, -size.Width / 2f, -size.Height / 2f);
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

            btnRotate180.Left = 15;
            int rotationY = chkIncrement.Bottom + 10;
            btnRotate180.Top = rotationY + btnRotateminus90.Height;

            int verticalButtonWidth = btnRotateminus90.Width;
            btnRotateminus90.Left = btnRotate180.Right + 5;
            btnRotate90.Left = btnRotateminus90.Left;

            btnRotate0.Left = btnRotateminus90.Right + 5;
            btnRotate0.Top = btnRotate180.Top;

            btnRotateminus90.Top = btnRotate180.Top - btnRotateminus90.Height;
            btnRotate90.Top = btnRotate180.Bottom;

            pctRotate.Left = btnRotate0.Right + 15;
            pctRotate.Top = btnRotateminus90.Top;

            txtRotate.Left = pctRotate.Left;
            txtRotate.Top = pctRotate.Bottom + 5;
            txtRotate.Width = 35;

            hScrollRotate.Left = txtRotate.Right + 5;
            hScrollRotate.Top = txtRotate.Top + (txtRotate.Height - hScrollRotate.Height) / 2;
            hScrollRotate.Width = groupBox1.Width - hScrollRotate.Left - 15;
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

            int startX = 15;
            int startY = 20;

            txtWidth.Location = new Point(startX, startY);
            txtWidth.Width = 40;

            label1.Text = "x";
            label1.AutoSize = true;
            label1.Location = new Point(txtWidth.Right + 5, startY + (txtWidth.Height - label1.Height) / 2);

            txtHeight.Location = new Point(label1.Right + 5, startY);
            txtHeight.Width = 40;

            cboCornerSize.Location = new Point(txtHeight.Right + 15, startY);
            cboCornerSize.Width = 50;

            btnCorners.Location = new Point(cboCornerSize.Right + 15, startY);
            btnCorners.Width = 70;
            btnCorners.Height = txtWidth.Height;

            int row2Y = txtWidth.Bottom + 10;

            btnStretch.Location = new Point(startX, row2Y);
            btnStretch.Width = 60;
            btnStretch.Height = txtWidth.Height;

            btnResize.Location = new Point(btnStretch.Right + 5, row2Y);
            btnResize.Width = 60;
            btnResize.Height = txtWidth.Height;

            cboBorderSize.Location = new Point(txtHeight.Right + 15, row2Y);
            cboBorderSize.Width = 50;

            btnGradient.Location = new Point(cboBorderSize.Right + 15, row2Y);
            btnGradient.Width = 70;
            btnGradient.Height = txtWidth.Height;

            int row3Y = btnStretch.Bottom + 10;

            pctTransparentColor.Location = new Point(startX, row3Y + (txtWidth.Height - 20) / 2);
            pctTransparentColor.Size = new Size(25, 20);

            label2.AutoSize = true;
            label2.Location = new Point(pctTransparentColor.Right + 5, row3Y + (txtWidth.Height - label2.Height) / 2);

            cboClear.Location = new Point(txtHeight.Right + 15, row3Y);
            cboClear.Width = 50;

            btnClear.Location = new Point(cboClear.Right + 15, row3Y);
            btnClear.Width = 70;
            btnClear.Height = txtWidth.Height;
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

            btnReset.Location = new Point(15, 20);
            btnReset.Width = 60;

            btnAlignLeft.Location = new Point(btnReset.Right + 15, 20);
            btnAlignLeft.Width = 24;
            btnAlignLeft.Height = btnReset.Height;

            btnAlignCenter.Location = new Point(btnAlignLeft.Right + 5, 20);
            btnAlignCenter.Width = 24;
            btnAlignCenter.Height = btnReset.Height;

            btnAlignRights.Location = new Point(btnAlignCenter.Right + 5, 20);
            btnAlignRights.Width = 24;
            btnAlignRights.Height = btnReset.Height;

            int verticalStackX = 15;
            int startY = btnReset.Bottom + 10;

            btnExtendRange.Location = new Point(verticalStackX, startY);
            btnExtendRange.Width = 24;
            btnExtendRange.Height = btnReset.Height;

            btnAlignTop.Location = new Point(verticalStackX, btnExtendRange.Bottom + 5);
            btnAlignTop.Width = 24;
            btnAlignTop.Height = btnReset.Height;

            btnAlignMiddle.Location = new Point(verticalStackX, btnAlignTop.Bottom + 5);
            btnAlignMiddle.Width = 24;
            btnAlignMiddle.Height = btnReset.Height;

            btnAlignBottom.Location = new Point(verticalStackX, btnAlignMiddle.Bottom + 5);
            btnAlignBottom.Width = 24;
            btnAlignBottom.Height = btnReset.Height;

            btnReduceRange.Location = new Point(verticalStackX, btnAlignBottom.Bottom + 5);
            btnReduceRange.Width = 24;
            btnReduceRange.Height = btnReset.Height;

            vScrollText.Left = btnExtendRange.Right + 10;
            vScrollText.Top = btnExtendRange.Top;
            vScrollText.Width = 18;

            txtText.Left = vScrollText.Right + 10;
            txtText.Top = vScrollText.Top;
            txtText.Width = groupBox1.Width - txtText.Left - 15;

            lblTileSize.Left = txtText.Left;
            lblTileSize.Top = txtText.Bottom + 5;

            lblTextPlacement.Left = txtText.Left;
            lblTextPlacement.Top = lblTileSize.Bottom + 5;

            hScrollText.Left = txtText.Left;
            hScrollText.Width = txtText.Width;
            hScrollText.Height = 17;
            hScrollText.Top = lblTextPlacement.Bottom + 5;

            vScrollText.Height = hScrollText.Top - vScrollText.Top;

            int buttonY = hScrollText.Bottom + 10;
            int availableWidth = groupBox1.Width - 30;
            int buttonWidth = (availableWidth - 15) / 4;

            btnHideControls.Location = new Point(15, buttonY);
            btnHideControls.Width = buttonWidth;
            btnHideControls.Height = btnReset.Height;

            btnPlace.Location = new Point(btnHideControls.Right + 5, buttonY);
            btnPlace.Width = buttonWidth;
            btnPlace.Height = btnReset.Height;

            btnStore.Location = new Point(btnPlace.Right + 5, buttonY);
            btnStore.Width = buttonWidth;
            btnStore.Height = btnReset.Height;

            btnReplace.Location = new Point(btnStore.Right + 5, buttonY);
            btnReplace.Width = buttonWidth;
            btnReplace.Height = btnReset.Height;

            chkIncrement.Location = new Point(15, btnHideControls.Bottom + 10);

            LayoutRotationControls();
        }

        private void LayoutMainFormContainers()
        {
            if (toolStrip1 == null || groupBox3 == null || groupBox2 == null || groupBox1 == null || groupBox4 == null)
            {
                return;
            }
            int designTimeBottom = 49;
            int currentBottom = toolStrip1.Bottom;
            int deltaY = currentBottom - designTimeBottom;

            if (deltaY > 0)
            {
                this.Height += deltaY;
                groupBox3.Top += deltaY;
                groupBox2.Top += deltaY;
                groupBox1.Top += deltaY;
                groupBox4.Top += deltaY;
            }
        }

        private void ModernizeToolbarIcons()
        {
            toolStrip1.ImageScalingSize = new Size(48, 48);

            imageList1.Images.Clear();
            imageList1.ImageSize = new Size(48, 48);
            
            imageList1.Images.Add(GetModernGlyph("\uE7AD", 48, Color.Black)); // Index 0: ROTATE_T
            imageList1.Images.Add(GetModernGlyph("\uE7AD", 48, Color.Gray));  // Index 1: ROTATE_DISABLED
            imageList1.Images.Add(GetModernGlyph("\uE10F", 48, Color.Black)); // Index 2: WriteText2
            imageList1.Images.Add(GetModernGlyph("\uE72E", 48, Color.Black)); // Index 3: lock_closed
            imageList1.Images.Add(GetModernGlyph("\uE785", 48, Color.Black)); // Index 4: lock_closedNO
            imageList1.Images.Add(GetModernGlyph("\uE8C0", 48, Color.Black)); // Index 5: CreateHalfFromFull
            imageList1.Images.Add(GetModernGlyph("\uE8C0", 48, Color.Gray));  // Index 6: CreateHalfFromFullNO
            imageList1.Images.Add(GetModernGlyph("\uE785", 48, Color.Black)); // Index 7: lock_open
            imageList1.Images.Add(GetModernGlyph("\uE814", 48, Color.Black)); // Index 8: AutoBevelBW
            imageList1.Images.Add(GetModernGlyph("\uE814", 48, Color.Gray));  // Index 9: AutoBevelBW-A
            imageList1.Images.Add(GetModernGlyph("\uE72C", 48, Color.Black)); // Index 10: AutoClipBW
            imageList1.Images.Add(GetModernGlyph("\uE72C", 48, Color.Gray));  // Index 11: AutoClipBW-A
            imageList1.Images.Add(GetModernGlyph("\uE890", 48, Color.Gray));  // Index 12: AntiAlias-Off
            imageList1.Images.Add(GetModernGlyph("\uE890", 48, Color.DodgerBlue)); // Index 13: antialias-on

            openToolStripButton.Image = GetModernGlyph("\uE889", 48, Color.Black);
            saveToolStripButton.Image = GetModernGlyph("\uE74E", 48, Color.Black);
            copyToolStripButton.Image = GetModernGlyph("\uE8C8", 48, Color.Black);
            pasteToolStripButton.Image = GetModernGlyph("\uE77F", 48, Color.Black);
            WhatsThistoolStripButton.Image = GetModernGlyph("\uE897", 48, Color.Black);
            
            toolFontSelectButton.Image = GetModernGlyph("\uE8D2", 48, Color.Black);
            fontColortoolStripButton1.Image = GetModernGlyph("\uE8D3", 48, Color.Black);
            toolFontIncreaseButton.Image = GetModernGlyph("\uE8E7", 48, Color.Black);
            toolFontDecreaseButton.Image = GetModernGlyph("\uE8E8", 48, Color.Black);
            toolFontBoldButton.Image = GetModernGlyph("\uE19B", 48, Color.Black);
            toolFontItalicButton.Image = GetModernGlyph("\uE19C", 48, Color.Black);
            toolAntiAliasButton1.Image = GetModernGlyph("\uE890", 48, Color.Gray);
            
            placeTexttoolStripButton.Image = GetModernGlyph("\uE10F", 48, Color.Black);
            storetoolStripButton.Image = GetModernGlyph("\uE7B3", 48, Color.Black);
            
            lockCreateHalffromFullStripButton1.Image = GetModernGlyph("\uE8C0", 48, Color.Gray);
            createTiletoolStripButton.Image = GetModernGlyph("\uE109", 48, Color.Black);
            createFromPositiontoolStripButton.Image = GetModernGlyph("\uE8A5", 48, Color.Black);
            replaceTooltoolStripButton1.Image = GetModernGlyph("\uE117", 48, Color.Black);
            
            lockRotationtoolStripButton1.Image = GetModernGlyph("\uE72E", 48, Color.Black);
            toolRotateLeftButton.Image = GetModernGlyph("\uE7A6", 48, Color.Black);
            toolRotateRightButton.Image = GetModernGlyph("\uE7A7", 48, Color.Black);
            
            lockFliptoolStripButton1.Image = GetModernGlyph("\uE72E", 48, Color.Black);
            toolFlipHorizontal.Image = GetModernGlyph("\uE8AB", 48, Color.Black);
            toolFlipVertical.Image = GetModernGlyph("\uE8AC", 48, Color.Black);
            
            fullTiletoolStripButton1.Image = GetModernGlyph("\uF158", 48, Color.Black);
            halfTiletoolStripButton1.Image = GetModernGlyph("\uF156", 48, Color.Black);
            smallTiletoolStripButton2.Image = GetModernGlyph("\uF157", 48, Color.Black);
            deletetoolStripButton1.Image = GetModernGlyph("\uE74D", 48, Color.Black);
        }

        //Constructor
        public Form1()                                                                                 
        {
            InitializeComponent();
            TileDisplay = new tileDisplay(this, this.components, panel1);
            PositionDisplay = new positionDisplay(this, panel3, vScrollBar2, workingFullTile, workingHalfTile, workingTile);
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(mruRegKey);
            if (regKey != null)
            {
                regKey.Close();
            }
            mruMenu = new MruStripMenuInline(fileToolStripMenuItem, MRUToolStripMenuItem, new MruStripMenu.ClickedHandler(OnMruFile), mruRegKey + "\\MRU",8);
            
            // Programmatically bind canvas interaction events
            workingTile.MouseDown += new MouseEventHandler(workingTile_MouseDown);
            workingTile.MouseMove += new MouseEventHandler(workingTile_MouseMove);
            workingTile.MouseUp += new MouseEventHandler(workingTile_MouseUp);
            workingTile.MouseDoubleClick += new MouseEventHandler(workingTile_MouseDoubleClick);
        }
        //Methods
        private void Form1_Load(object sender, EventArgs e)                                            
        {
            EnableDoubleBuffering(panel1);
            EnableDoubleBuffering(panel2);
            EnableDoubleBuffering(panel3);
            EnableDoubleBuffering(workingTile);

            ModernizeToolbarIcons();
            this.PerformLayout();
            LayoutMainFormContainers();

            lockCreateHalffromFullStripButton1.Image = this.imageList1.Images[6];

            mruMenu.LoadFromRegistry();

            panel1Height = panel1.Height;

            FontFamily[] fontFamilies;
            InstalledFontCollection installedFontCollection = new InstalledFontCollection();
            fontFamilies = installedFontCollection.Families;

            this.vScrollBar1.Top = this.toolStrip1.Bottom;
            this.panel1.Top = this.toolStrip1.Bottom;
            panelHeight = this.panel1.Height;

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
                
                this.vScrollBar1.Maximum = panel1.Controls.Count - 1;
                this.vScrollBar1.Value = 0;
                this.panel1.Top = -TileDisplay[vScrollBar1.Value].Top + this.toolStrip1.Bottom;

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
                createBMPtoolStripMenuItem.Enabled = true;
                createPNGtoolStripMenuItem.Enabled = true;

                WhatsThistoolStripMenuItem.Enabled = true;

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
                createBMPtoolStripMenuItem.Enabled = false;
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
                this.panel1.Top = -TileDisplay[vScrollBar1.Value].Top + this.toolStrip1.Bottom;
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
                panel1.Left = groupBox2.Right + 6;
            }
            else
            {
                panel1.Left = 760;
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
                    {
                        panel1.Height = panel1.Controls[panel1.Controls.Count - 1].Bottom;
                    }
                    else
                    {
                        panel1.Height = panel1Height;
                        panel1.Top = toolStrip1.Bottom;
                        vScrollBar1.Enabled = false;
                    }

                    this.vScrollBar1.Maximum = panel1.Controls.Count - 1;

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
                    if (panel1.Bottom < toolStrip1.Bottom)
                    {
                        panel1.Top = toolStrip1.Bottom - panel1.Controls[currenttile].Top;
                        vScrollBar1.Value = currenttile;
                    }

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
                        panel1.Top = toolStrip1.Bottom;
                        vScrollBar1.Enabled = false;
                    }

                    this.vScrollBar1.Maximum = panel1.Controls.Count - 1;

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
                    if (panel1.Bottom < toolStrip1.Bottom)
                    {
                        panel1.Top = toolStrip1.Bottom - panel1.Controls[currenttile].Top;
                        vScrollBar1.Value = currenttile;
                    }

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
        private void replaceTiletoolStripMenuItem_Click(object sender, EventArgs e)                    
        {
            this.Cursor = Cursors.Default;
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
            ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls["fullTile"]).Image = workingFullTile.Image;
            ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls["halfTile"]).Image = workingHalfTile.Image;
            ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls["smallTile"]).BackColor = workingSmallTile.BackColor;
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

                        if (panel3.Bottom > panel2.Height)
                        {
                            vScrollBar2.Enabled = true;
                            vScrollBar2.Maximum = panel3.Controls.Count - 1;
                        }
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

                    if (panel3.Bottom > panel2.Height)
                    {
                        vScrollBar2.Enabled = true;
                        vScrollBar2.Maximum = panel3.Controls.Count - 1;
                    }
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
            this.vScrollBar1.Maximum = panel1.Controls.Count - 1;
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
                this.vScrollBar1.Maximum = panel1.Controls.Count - 1;
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
                this.vScrollBar1.Maximum = panel1.Controls.Count - 1;
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
                ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls["fullTile"]).Image = workingFullTile.Image;
                ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls["halfTile"]).Image = workingHalfTile.Image;
                ((PictureBox)((gtlTileDisplay)panel1.Controls[TileDisplay.CurrentTile]).Controls["smallTile"]).BackColor = workingSmallTile.BackColor;
            }
        }
        private void createBMPtoolStripMenuItem_Click(object sender, EventArgs e)                      
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                Image saveImage;
                for (int i = 0; i < panel1.Controls.Count; i++)
                {
                    saveImage = (Image)((gtlTileDisplay)panel1.Controls[i]).fullTile.Image.Clone();
                    if (i < 9)
                        saveImage.Save(folderBrowserDialog1.SelectedPath + "\\" + Path.GetFileNameWithoutExtension(currentfilename) + "_0" + (i + 1).ToString() + "_Full.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
                    else
                        saveImage.Save(folderBrowserDialog1.SelectedPath + "\\" + Path.GetFileNameWithoutExtension(currentfilename) + "_" + (i + 1).ToString() + "_Full.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
                }
            }
        }
        private void createPNGtoolStripMenuItem_Click(object sender, EventArgs e)                      
        {
            Bitmap originalBitmap;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                Image saveImage;
                for (int i = 0; i < panel1.Controls.Count; i++)
                {
                    originalBitmap = (Bitmap)((gtlTileDisplay)panel1.Controls[i]).fullTile.Image.Clone();
                    Color backColor = pctTransparentColor.BackColor; // originalBitmap.GetPixel(0, 0);
                    originalBitmap.MakeTransparent(backColor);
                    saveImage = (Image)originalBitmap;

                    //saveImage = (Image)((gtlTileDisplay)panel1.Controls[i]).fullTile.Image.Clone();
                    
                    if (i < 9)
                        saveImage.Save(folderBrowserDialog1.SelectedPath + "\\" + Path.GetFileNameWithoutExtension(currentfilename) + "_0" + (i + 1).ToString() + "_Full.png", System.Drawing.Imaging.ImageFormat.Png);
                    else
                        saveImage.Save(folderBrowserDialog1.SelectedPath + "\\" + Path.GetFileNameWithoutExtension(currentfilename) + "_" + (i + 1).ToString() + "_Full.png", System.Drawing.Imaging.ImageFormat.Png);
                }
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
                this.panel3.Top = -PositionDisplay[vScrollBar2.Value].Top;
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
                vScrollBar1.Minimum = -5;
                vScrollBar1.Maximum = workingTile.Height;
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
        private void WhatsThistoolStripMenuItem_Click(object sender, EventArgs e)                      
        {
            this.Cursor = Cursors.Help;
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
        private void WhatsThistoolStripButton_Click(object sender, EventArgs e)                        
        {
            if (Cursor.Current == Cursors.Default)
            {
                this.Cursor = Cursors.Help;
            }
            else
            {
                this.Cursor = Cursors.Default;
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

                        if (panel3.Bottom > panel2.Height)
                        {
                            vScrollBar2.Enabled = true;
                            vScrollBar2.Maximum = panel3.Controls.Count - 1;
                        }
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

                    if (panel3.Bottom > panel2.Height)
                    {
                        vScrollBar2.Enabled = true;
                        vScrollBar2.Maximum = panel3.Controls.Count - 1;
                    }
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
                this.vScrollBar1.Maximum = panel1.Controls.Count - 1;
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
}

