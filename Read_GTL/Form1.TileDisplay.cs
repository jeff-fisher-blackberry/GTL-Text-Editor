using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using gtlDisplay;
using positionDisplayControl;

namespace GTL_Editor
{
    public partial class Form1
    {
        private class tileDisplay : System.Collections.CollectionBase
        {
            //Local Variables
            private readonly Panel HostPanel;
            private readonly Form HostForm;
            private readonly IContainer HostContainer;
            private int rowBuffer = 4;
            private Color rowColor = Color.LightBlue;
            private int currenttile;
            
            //Properties
            public int CurrentTile                                                                     
            {
                get
                {
                    return currenttile;
                }
                set
                {
                    currenttile = value;
                }
            }
            public int TotalTiles                                                                      
            {
                get
                {
                    return HostPanel.Controls.Count - 1;
                }
            }
            //Constructors
            public tileDisplay(Form hostForm, IContainer hostContainer, Panel hostPanel)               
            {
                HostForm = hostForm;
                HostPanel = hostPanel;
                HostContainer = hostContainer;
            }
            //Index
            public gtlTileDisplay this[int Index]                                                      
            {
                get
                {
                    return (gtlTileDisplay)this.List[Index];
                }
            }
            //Methods
            public gtlTileDisplay AddTile(GTL_File.gtltile Tile)                                       
            {
                gtlTileDisplay agtlTileDisplay = new gtlTileDisplay();

                agtlTileDisplay.Visible = false;

                this.List.Add(agtlTileDisplay);
                
                HostPanel.Controls.Add(agtlTileDisplay);

                if (Tile.FullTile.Height > 15)
                {
                    agtlTileDisplay.Height = Tile.FullTile.Height + rowBuffer;
                }
                else
                {
                    agtlTileDisplay.Height = 15 + rowBuffer;
                }
                agtlTileDisplay.Width = HostPanel.Width;

                if (HostPanel.Controls.Count == 1)
                {
                    agtlTileDisplay.Top = 0;
                }
                if (HostPanel.Controls.Count > 1)
                {
                    agtlTileDisplay.Top = ((gtlTileDisplay)HostPanel.Controls[HostPanel.Controls.Count - 2]).Bottom;
                }
                if (agtlTileDisplay.Bottom > HostPanel.Height)
                {
                    HostPanel.Height = agtlTileDisplay.Bottom;
                    HostForm.Controls["vScrollBar1"].Enabled = true;
                }
                else
                {
                    HostForm.Controls["vScrollBar1"].Enabled = false;
                }

                agtlTileDisplay.fullTile.Top = rowBuffer / 2;
                agtlTileDisplay.fullTile.Width = Tile.FullTile.Width;
                agtlTileDisplay.fullTile.Height = Tile.FullTile.Height;

                agtlTileDisplay.halfTile.Top = rowBuffer / 2;
                agtlTileDisplay.halfTile.Left = agtlTileDisplay.fullTile.Right + 20;
                agtlTileDisplay.halfTile.Width = Tile.HalfTile.Width;
                agtlTileDisplay.halfTile.Height = Tile.HalfTile.Height;

                agtlTileDisplay.smallTile.Top = rowBuffer / 2;
                agtlTileDisplay.smallTile.Left = agtlTileDisplay.halfTile.Right + 20;
                agtlTileDisplay.smallTile.Width = 15;
                agtlTileDisplay.smallTile.Height = 15;

                agtlTileDisplay.fullTile.Image = Tile.FullTile;
                agtlTileDisplay.halfTile.Image = Tile.HalfTile;
                agtlTileDisplay.smallTile.BackColor = Tile.SmallTile;

                agtlTileDisplay.Tag = this.Count;
                agtlTileDisplay.fullTile.Tag = this.Count;
                agtlTileDisplay.halfTile.Tag = this.Count;
                agtlTileDisplay.smallTile.Tag = this.Count;

                agtlTileDisplay.Click += new System.EventHandler(ClickHandler);
                agtlTileDisplay.fullTile.Click += new System.EventHandler(ClickHandlerPictureBox);
                agtlTileDisplay.halfTile.Click += new System.EventHandler(ClickHandlerPictureBox);
                agtlTileDisplay.smallTile.Click += new System.EventHandler(ClickHandlerPictureBox);

                if (HostPanel.Controls.Count == 1)
                {
                    agtlTileDisplay.BackColor = rowColor;
                }

                agtlTileDisplay.Visible = true;

                return agtlTileDisplay;
            }
            public void RemoveTile(int tile)                                                           
            {
                HostPanel.Controls.Remove(this[tile]);
                this[tile].Dispose();
                this.List.RemoveAt(tile);
                for (int i = 0; i < HostPanel.Controls.Count; i++)
                {
                    HostPanel.Controls[i].Tag = i + 1;
                    ((PictureBox)HostPanel.Controls[i].Controls["fullTile"]).Tag = i + 1;
                    ((PictureBox)HostPanel.Controls[i].Controls["halfTile"]).Tag = i + 1;
                    ((PictureBox)HostPanel.Controls[i].Controls["smallTile"]).Tag = i + 1;
                }
            }
            public void RemoveAllTiles()                                                               
            {
                ((Panel)HostPanel).Controls.Clear();
                this.List.Clear();
            }
            public void ClickHandler(Object sender, System.EventArgs e)                                
            {
                if (Cursor.Current == Cursors.Help)
                {
                    HostForm.Cursor = Cursors.Default;
                    Help.ShowHelp(HostForm, "gtleditor.chm", HelpNavigator.KeywordIndex, "GTL File Area");
                }
                else
                {
                    if (Control.ModifierKeys == Keys.Control)
                    {
                        ((gtlTileDisplay)sender).BackColor = rowColor;
                    }
                    else
                    {
                        int totaltile;
                        int secondtile;
                        int begintile, endtile;
                        if (Control.ModifierKeys == Keys.Shift)
                        {
                            secondtile = Convert.ToInt32(((gtlTileDisplay)sender).Tag) - 1;
                            if (secondtile > currenttile)
                            {
                                begintile = currenttile;
                                endtile = secondtile;
                            }
                            else
                            {
                                begintile = secondtile;
                                endtile = currenttile;
                            }
                            for (int i = begintile; i <= endtile; i++)
                            {
                                ((gtlTileDisplay)HostPanel.Controls[i]).BackColor = rowColor;
                            }
                            totaltile = endtile - begintile + 1;
                        }
                        else
                        {
                            clearPanel();

                            currenttile = Convert.ToInt32(((gtlTileDisplay)sender).Tag) - 1;

                            ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Image = ((gtlTileDisplay)sender).fullTile.Image;
                            ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width = ((gtlTileDisplay)sender).fullTile.Image.Width;
                            ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height = ((gtlTileDisplay)sender).fullTile.Height;
                            ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Invalidate();
                            ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Update();

                            if ((((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width + 24
                                <= ((GroupBox)HostForm.Controls["groupBox2"]).Width))
                            {
                                ((GroupBox)HostForm.Controls["groupBox2"]).Width = 274;

                                ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Left
                                    = Convert.ToInt32(((GroupBox)HostForm.Controls["groupBox2"]).Width * 0.5
                                    - ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width * 0.5);
                            }
                            else
                            {
                                ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Left = 8;
                                ((GroupBox)HostForm.Controls["groupBox2"]).Width = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width + 16;
                            }

                            if (((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height + 37
                                <= ((GroupBox)HostForm.Controls["groupBox2"]).Height)
                            {
                                ((GroupBox)HostForm.Controls["groupBox2"]).Height = 381;

                                ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Top
                                    = Convert.ToInt32(((GroupBox)HostForm.Controls["groupBox2"]).Height * 0.5
                                    - ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height * 0.5);
                            }
                            else
                            {
                                ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Top = 15;
                                ((GroupBox)HostForm.Controls["groupBox2"]).Height = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height + 22;
                            }


                            if (((GroupBox)HostForm.Controls["groupBox2"]).Right + 6 > 486)
                            {
                                ((Panel)HostForm.Controls["panel1"]).Left = ((GroupBox)HostForm.Controls["groupBox2"]).Right + 6;
                            }
                            else
                            {
                                ((Panel)HostForm.Controls["panel1"]).Left = 486;
                            }

                            ((PictureBox)HostForm.Controls["workingFullTile"]).Image = ((gtlTileDisplay)sender).fullTile.Image;
                            ((PictureBox)HostForm.Controls["workingFullTile"]).Top = ((GroupBox)HostForm.Controls["groupBox2"]).Bottom + 20;
                            ((PictureBox)HostForm.Controls["workingFullTile"]).Size = ((gtlTileDisplay)sender).fullTile.Size;
                            ((PictureBox)HostForm.Controls["workingFullTile"]).Invalidate();
                            ((PictureBox)HostForm.Controls["workingFullTile"]).Update();

                            ((TextBox)HostForm.Controls["groupBox1"].Controls["txtText"]).BackColor = ((gtlTileDisplay)sender).smallTile.BackColor;
                            ((PictureBox)HostForm.Controls["workingSmallTile"]).BackColor = ((gtlTileDisplay)sender).smallTile.BackColor;

                            ((PictureBox)HostForm.Controls["workingHalfTile"]).Top = ((PictureBox)HostForm.Controls["workingFullTile"]).Bottom + 20;

                            if (((ToolStripButton)((ToolStrip)HostForm.Controls["toolStrip1"]).Items["lockCreateHalffromFullStripButton1"]).CheckState == CheckState.Unchecked)
                            {
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Width = (((PictureBox)HostForm.Controls["workingFullTile"]).Width / 2) + 1;
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Height = (((PictureBox)HostForm.Controls["workingFullTile"]).Height / 2) + 1;
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Image = FixedSize(((PictureBox)HostForm.Controls["workingFullTile"]).Image, ((PictureBox)HostForm.Controls["workingHalfTile"]).Width, ((PictureBox)HostForm.Controls["workingHalfTile"]).Height);
                            }
                            else
                            {
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Image = ((gtlTileDisplay)sender).halfTile.Image;
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Top = ((PictureBox)HostForm.Controls["workingFullTile"]).Bottom + 20;
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Size = ((gtlTileDisplay)sender).halfTile.Size;
                            }

                            ((PictureBox)HostForm.Controls["workingHalfTile"]).Invalidate();
                            ((PictureBox)HostForm.Controls["workingHalfTile"]).Update();

                            ((PictureBox)HostForm.Controls["workingSmallTile"]).Top = ((PictureBox)HostForm.Controls["workingHalfTile"]).Bottom + 20;
                            ((PictureBox)HostForm.Controls["workingSmallTile"]).Invalidate();
                            ((PictureBox)HostForm.Controls["workingSmallTile"]).Update();

                            ((gtlTileDisplay)sender).BackColor = rowColor;

                            ((TextBox)HostForm.Controls["groupBox3"].Controls["txtWidth"]).Text = ((PictureBox)HostForm.Controls["workingFullTile"]).Width.ToString();
                            ((TextBox)HostForm.Controls["groupBox3"].Controls["txtHeight"]).Text = ((PictureBox)HostForm.Controls["workingFullTile"]).Height.ToString();
                            ((Label)HostForm.Controls["groupBox1"].Controls["lblTileSize"]).Text = ((PictureBox)HostForm.Controls["workingFullTile"]).Width + "," + ((PictureBox)HostForm.Controls["workingFullTile"]).Height;
                            ((Label)HostForm.Controls["groupBox1"].Controls["lblCurrentWorkingTile"]).Text = "FullTile";

                            ((VScrollBar)HostForm.Controls["groupBox1"].Controls["vScrollText"]).Maximum = ((gtlTileDisplay)sender).fullTile.Height;
                            ((HScrollBar)HostForm.Controls["groupBox1"].Controls["hScrollText"]).Maximum = ((gtlTileDisplay)sender).fullTile.Width;

                            if (((Panel)HostForm.Controls["groupBox4"].Controls["panel2"].Controls["panel3"]).Controls.Count > 0)
                            {
                                for (int i = 0; i < ((Panel)HostForm.Controls["groupBox4"].Controls["panel2"].Controls["panel3"]).Controls.Count; i++)
                                {
                                    ((positionTextDisplay)HostForm.Controls["groupBox4"].Controls["panel2"].Controls["panel3"].Controls[i]).TextBackColor = ((gtlTileDisplay)sender).smallTile.BackColor;
                                }
                            }
                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpUp"]).Top = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Top - ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpUp"]).Height - 5;
                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpUp"]).Left = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Left + (((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width / 2) - (((Button)HostForm.Controls["groupBox2"].Controls["btnBumpUp"]).Width / 2);

                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpDown"]).Top = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Bottom + 5;
                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpDown"]).Left = ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpUp"]).Left;

                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpRight"]).Top = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Top + (((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height / 2) - (((Button)HostForm.Controls["groupBox2"].Controls["btnBumpRight"]).Height / 2);
                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpRight"]).Left = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Right + 5;

                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpLeft"]).Top = ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpRight"]).Top;
                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpLeft"]).Left = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Left - ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpLeft"]).Width - 5;

                            ((PictureBox)HostForm.Controls["pctzoom"]).Image = Zoom(((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Image);
                            ((PictureBox)HostForm.Controls["pctzoom"]).Width = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width * 3;
                            ((PictureBox)HostForm.Controls["pctzoom"]).Height = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height * 3;
                            ((PictureBox)HostForm.Controls["pctzoom"]).Top = ((GroupBox)HostForm.Controls["groupBox4"]).Bottom - ((PictureBox)HostForm.Controls["pctzoom"]).Height;
                            ((PictureBox)HostForm.Controls["pctzoom"]).Left = ((GroupBox)HostForm.Controls["groupBox2"]).Right - ((PictureBox)HostForm.Controls["pctzoom"]).Width;
                        }
                    }
                }
            }
            public void ClickHandlerPictureBox(Object sender, System.EventArgs e)                      
            {
                if (Cursor.Current == Cursors.Help)
                {
                    HostForm.Cursor = Cursors.Default;
                    Help.ShowHelp(HostForm, "gtleditor.chm", HelpNavigator.KeywordIndex, "GTL File Area");
                }
                else
                {
                    if (Control.ModifierKeys == Keys.Control)
                    {
                        ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).BackColor = rowColor;
                    }
                    else
                    {
                        int secondtile;
                        int begintile, endtile;
                        if (Control.ModifierKeys == Keys.Shift)
                        {
                            secondtile = Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1;
                            if (secondtile > currenttile)
                            {
                                begintile = currenttile;
                                endtile = secondtile;
                            }
                            else
                            {
                                begintile = secondtile;
                                endtile = currenttile;
                            }
                            for (int i = begintile; i <= endtile; i++)
                            {
                                ((gtlTileDisplay)HostPanel.Controls[i]).BackColor = rowColor;
                            }
                        }
                        else
                        {
                            clearPanel();

                            currenttile = Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1;

                            ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Image
                                = ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).fullTile.Image;
                            ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width
                                = ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).fullTile.Width;
                            ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height
                                = ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).fullTile.Height;

                            if ((((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width + 24
                                <= ((GroupBox)HostForm.Controls["groupBox2"]).Width))
                            {
                                ((GroupBox)HostForm.Controls["groupBox2"]).Width = 350;

                                ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Left
                                    = Convert.ToInt32(((GroupBox)HostForm.Controls["groupBox2"]).Width * 0.5
                                    - ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width * 0.5);
                            }
                            else
                            {
                                ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Left = 8;
                                ((GroupBox)HostForm.Controls["groupBox2"]).Width = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width + 16;
                            }

                            if (((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height + 37
                                <= ((GroupBox)HostForm.Controls["groupBox2"]).Height)
                            {
                                ((GroupBox)HostForm.Controls["groupBox2"]).Height = 381;

                                ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Top
                                    = Convert.ToInt32(((GroupBox)HostForm.Controls["groupBox2"]).Height * 0.5
                                    - ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height * 0.5);
                            }
                            else
                            {
                                ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Top = 15;
                                ((GroupBox)HostForm.Controls["groupBox2"]).Height = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height + 22;
                            }

                            if (((GroupBox)HostForm.Controls["groupBox2"]).Right + 6 > 486)
                            {
                                ((Panel)HostForm.Controls["panel1"]).Left = ((GroupBox)HostForm.Controls["groupBox2"]).Right + 6;
                            }
                            else
                            {
                                ((Panel)HostForm.Controls["panel1"]).Left = 486;
                            }

                            ((PictureBox)HostForm.Controls["workingFullTile"]).Top = ((GroupBox)HostForm.Controls["groupBox2"]).Bottom + 20;
                            ((PictureBox)HostForm.Controls["workingFullTile"]).Image = ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).fullTile.Image;
                            ((PictureBox)HostForm.Controls["workingFullTile"]).Size = ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).fullTile.Size;

                            ((PictureBox)HostForm.Controls["workingSmallTile"]).BackColor = ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).smallTile.BackColor;
                            ((TextBox)HostForm.Controls["groupBox1"].Controls["txtText"]).BackColor = ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).smallTile.BackColor;

                            ((PictureBox)HostForm.Controls["workingHalfTile"]).Top = ((PictureBox)HostForm.Controls["workingFullTile"]).Bottom + 20;

                            if (((ToolStripButton)((ToolStrip)HostForm.Controls["toolStrip1"]).Items["lockCreateHalffromFullStripButton1"]).CheckState == CheckState.Unchecked)
                            {
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Width = (((PictureBox)HostForm.Controls["workingFullTile"]).Width / 2) + 1;
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Height = (((PictureBox)HostForm.Controls["workingFullTile"]).Height / 2) + 1;
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Image = FixedSize(((PictureBox)HostForm.Controls["workingFullTile"]).Image, ((PictureBox)HostForm.Controls["workingHalfTile"]).Width, ((PictureBox)HostForm.Controls["workingHalfTile"]).Height);
                            }
                            else
                            {
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Image = ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).halfTile.Image;
                                ((PictureBox)HostForm.Controls["workingHalfTile"]).Size = ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).halfTile.Size;
                            }

                            ((PictureBox)HostForm.Controls["workingSmallTile"]).Top = ((PictureBox)HostForm.Controls["workingHalfTile"]).Bottom + 20;

                            ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).BackColor = rowColor;

                            ((TextBox)HostForm.Controls["groupBox3"].Controls["txtWidth"]).Text = ((PictureBox)HostForm.Controls["workingFullTile"]).Width.ToString();
                            ((TextBox)HostForm.Controls["groupBox3"].Controls["txtHeight"]).Text = ((PictureBox)HostForm.Controls["workingFullTile"]).Height.ToString(); ;
                            ((Label)HostForm.Controls["groupBox1"].Controls["lblTileSize"]).Text = ((PictureBox)HostForm.Controls["workingFullTile"]).Width + "," + ((PictureBox)HostForm.Controls["workingFullTile"]).Height;
                            ((Label)HostForm.Controls["groupBox1"].Controls["lblCurrentWorkingTile"]).Text = "FullTile";

                            ((VScrollBar)HostForm.Controls["groupBox1"].Controls["vScrollText"]).Maximum = ((PictureBox)HostForm.Controls["workingFullTile"]).Height;
                            ((HScrollBar)HostForm.Controls["groupBox1"].Controls["hScrollText"]).Maximum = ((PictureBox)HostForm.Controls["workingFullTile"]).Width;

                            if (((Panel)HostForm.Controls["groupBox4"].Controls["panel2"].Controls["panel3"]).Controls.Count > 0)
                            {
                                for (int i = 0; i < ((Panel)HostForm.Controls["groupBox4"].Controls["panel2"].Controls["panel3"]).Controls.Count; i++)
                                {
                                    ((positionTextDisplay)HostForm.Controls["groupBox4"].Controls["panel2"].Controls["panel3"].Controls[i]).TextBackColor = ((gtlTileDisplay)HostPanel.Controls[Convert.ToInt32(((PictureBox)sender).Tag.ToString()) - 1]).smallTile.BackColor;
                                }
                            }
                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpUp"]).Top = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Top - ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpUp"]).Height - 5;
                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpUp"]).Left = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Left + (((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width / 2) - (((Button)HostForm.Controls["groupBox2"].Controls["btnBumpUp"]).Width / 2);

                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpDown"]).Top = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Bottom + 5;
                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpDown"]).Left = ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpUp"]).Left;

                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpRight"]).Top = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Top + (((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height / 2) - (((Button)HostForm.Controls["groupBox2"].Controls["btnBumpRight"]).Height / 2);
                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpRight"]).Left = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Right + 5;

                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpLeft"]).Top = ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpRight"]).Top;
                            ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpLeft"]).Left = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Left - ((Button)HostForm.Controls["groupBox2"].Controls["btnBumpLeft"]).Width - 5;

                            ((PictureBox)HostForm.Controls["pctzoom"]).Image = Zoom(((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Image);
                            ((PictureBox)HostForm.Controls["pctzoom"]).Width = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Width * 3;
                            ((PictureBox)HostForm.Controls["pctzoom"]).Height = ((PictureBox)HostForm.Controls["groupBox2"].Controls["workingTile"]).Height * 3;
                            ((PictureBox)HostForm.Controls["pctzoom"]).Top = ((GroupBox)HostForm.Controls["groupBox4"]).Bottom - ((PictureBox)HostForm.Controls["pctzoom"]).Height;
                            ((PictureBox)HostForm.Controls["pctzoom"]).Left = ((GroupBox)HostForm.Controls["groupBox2"]).Right - ((PictureBox)HostForm.Controls["pctzoom"]).Width;
                        }
                    }
                }
            }
            private void clearPanel()                                                                  
            {
                for (int i = 0; i < HostPanel.Controls.Count; i++)
                {
                    ((gtlTileDisplay)HostPanel.Controls[i]).BackColor = Color.White;
                }
            }
            private void enableControls()                                                              
            {
                if (((GroupBox)HostForm.Controls["groupBox1"]).Enabled != true)
                {
                    ((GroupBox)HostForm.Controls["groupBox1"]).Enabled = true;
                    ((GroupBox)HostForm.Controls["groupBox2"]).Enabled = true;
                    ((GroupBox)HostForm.Controls["groupBox3"]).Enabled = true;
                    ((GroupBox)HostForm.Controls["groupBox4"]).Enabled = true;

                    ((ToolStripDropDownItem)((MenuStrip)HostForm.Controls["menuStrip1"]).Items[0]).DropDownItems[1].Enabled = true;
                    ((ToolStripDropDownItem)((MenuStrip)HostForm.Controls["menuStrip1"]).Items[0]).DropDownItems[2].Enabled = true;

                    ((ToolStripDropDownItem)((MenuStrip)HostForm.Controls["menuStrip1"]).Items[0]).DropDownItems[6].Enabled = true;
                    ((ToolStripDropDownItem)((MenuStrip)HostForm.Controls["menuStrip1"]).Items[0]).DropDownItems[7].Enabled = true;
                    ((ToolStripDropDownItem)((MenuStrip)HostForm.Controls["menuStrip1"]).Items[0]).DropDownItems[9].Enabled = true;

                    for (int i = 0; i < ((ToolStripDropDownItem)((MenuStrip)HostForm.Controls["menuStrip1"]).Items[1]).DropDownItems.Count; i++)
                    ((ToolStripDropDownItem)((MenuStrip)HostForm.Controls["menuStrip1"]).Items[1]).DropDownItems[i].Enabled = true;

                    for (int i = 1; i < ((ToolStrip)HostForm.Controls["toolStrip1"]).Items.Count; i++)
                        ((ToolStrip)HostForm.Controls["toolStrip1"]).Items[i].Enabled = true;

                    if (HostPanel.Controls.Count > 1)
                    {
                        ((ToolStrip)HostForm.Controls["toolStrip1"]).Items["deletetoolStripButton1"].Enabled = true;
                        ((ToolStripDropDownItem)((MenuStrip)HostForm.Controls["menuStrip1"]).Items[3]).DropDownItems[4].Enabled = true;
                    }
                    ((PictureBox)HostForm.Controls["groupBox1"].Controls["pctRotate"]).Image = ((ImageList)HostContainer.Components[0]).Images[0];

                    Image RotateImage = ((PictureBox)HostForm.Controls["groupBox1"].Controls["pctRotate"]).Image;
                    ((PictureBox)HostForm.Controls["groupBox1"].Controls["pctRotate"]).Image = (Image)RotateImage.Clone();
                    using (Graphics gRotateText = Graphics.FromImage(((PictureBox)HostForm.Controls["groupBox1"].Controls["pctRotate"]).Image))
                    {
                        gRotateText.TranslateTransform(50.0f, 35.0f);
                        gRotateText.RotateTransform(Convert.ToSingle(((TextBox)HostForm.Controls["groupBox1"].Controls["txtRotate"]).Text));
                        gRotateText.DrawString(((TextBox)HostForm.Controls["groupBox1"].Controls["txtRotate"]).Text, ((Label)HostForm.Controls["groupBox1"].Controls["lblTextPlacement"]).Font, Brushes.Black, 0.0f, 0.0f);
                        gRotateText.DrawString(((TextBox)HostForm.Controls["groupBox1"].Controls["txtRotate"]).Text, ((Label)HostForm.Controls["groupBox1"].Controls["lblTextPlacement"]).Font, Brushes.Black, 0.0f, 0.0f);
                        gRotateText.ResetTransform();
                    }
                    ((PictureBox)HostForm.Controls["groupBox1"].Controls["pctRotate"]).Invalidate();
                    ((PictureBox)HostForm.Controls["groupBox1"].Controls["pctRotate"]).Update();               
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
            public static Image Zoom(Image inputImage)                                                 
            {
                Bitmap gBitmap = new Bitmap(inputImage.Width * 3, inputImage.Height * 3);
                Graphics g = Graphics.FromImage((Image)gBitmap);
                g.PageScale = 3.0f;
                g.PageUnit = GraphicsUnit.Pixel;
                g.DrawImage(inputImage, new Rectangle(0, 0, inputImage.Width, inputImage.Height), 0, 0, inputImage.Width, inputImage.Height, GraphicsUnit.Pixel);
                g.Dispose();
                return (Image)gBitmap;
            }
        }
    }
}
