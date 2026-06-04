using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using positionDisplayControl;

namespace GTL_Editor
{
    public partial class Form1
    {
        private class positionDisplay : System.Collections.CollectionBase
        {
            //Local Variables
            private readonly Form HostForm;
            private readonly Panel HostPanel;
            private VScrollBar HostVScrollBar;
            private PictureBox FullTile;
            private PictureBox HalfTile;
            private PictureBox WorkingTile;
            private int HostPanelHeight;

            //Constructors
            public positionDisplay(Form hostForm, Panel hostPanel, VScrollBar hostVScrollBar, PictureBox fulltile, PictureBox halftile, PictureBox workingtile)         
            {
                HostForm = hostForm;
                HostPanel = hostPanel;
                HostVScrollBar = hostVScrollBar;
                FullTile = fulltile;
                HalfTile = halftile;
                HostPanelHeight = hostPanel.Height;
                WorkingTile = workingtile;
            }

            //Index
            public positionTextDisplay this[int Index]                                                
            {
                get
                {
                    return (positionTextDisplay)this.List[Index];
                }
            }

            //Methods
            public positionTextDisplay addPosition(TextBox positionText,
                                                   CheckBox positionIncrement,
                                                   Point positionPlacement,
                                                   int positionRotation,
                                                   string workingSize)                                
            {
                positionTextDisplay aPosition = new positionTextDisplay();

                this.List.Add(aPosition);
                HostPanel.Controls.Add(aPosition);

                aPosition.WorkingSize = workingSize;

                aPosition.Tag = this.Count;
                aPosition.btnPositionDelete.Tag = this.Count;
                aPosition.txtPositionIncrement.Tag = this.Count;
                aPosition.txtPositionText.Tag = this.Count;
                aPosition.btnPositionPlace.Tag = this.Count;
                aPosition.btnSelect.Tag = this.Count;
                aPosition.btnSelect.Text = this.Count.ToString();
                aPosition.btnRestore.Tag = this.Count;
                aPosition.lblPositionFont.Tag = this.Count;
                aPosition.lblPositionFontSize.Tag = this.Count;
                aPosition.lblRotation.Tag = this.Count;
                aPosition.cbInclude.Text = "Include " + this.Count.ToString();

                ToolTip tooltip = new ToolTip();
                tooltip.SetToolTip(aPosition.btnPositionDelete, "Delete Position Data");
                tooltip.SetToolTip(aPosition.btnRestore, "Restore Position Data to Text Working Area");

                aPosition.txtPositionText.Height = positionText.Height;
                aPosition.txtPositionText.Text = positionText.Text;
                aPosition.txtPositionText.ForeColor = positionText.ForeColor;
                aPosition.txtPositionText.BackColor = positionText.BackColor;
                aPosition.txtPositionText.Font = positionText.Font;

                aPosition.lblPositionFont.Text = positionText.Font.Name;
                
                aPosition.lblPositionFontSize.Text = positionText.Font.Size.ToString();

                aPosition.PointPlacement = positionPlacement;
                aPosition.btnPositionPlace.Text = positionPlacement.X.ToString() + "," + positionPlacement.Y.ToString();

                if (positionIncrement.Checked == true)
                {
                    aPosition.txtPositionIncrement.Visible = true;
                    aPosition.txtPositionIncrement.Text = "1";
                    aPosition.txtPositionText.Width = aPosition.txtPositionText.Width - aPosition.txtPositionIncrement.Width - 2;
                    aPosition.txtPositionText.Left = aPosition.txtPositionIncrement.Right + 2;
                }

                aPosition.RotationDegrees = positionRotation;
                aPosition.lblRotation.Text = positionRotation.ToString();


                if (HostPanel.Controls.Count == 1)
                {
                    aPosition.Top = 0;
                }
                if (HostPanel.Controls.Count > 1)
                {
                    aPosition.Top = ((positionTextDisplay)HostPanel.Controls[HostPanel.Controls.Count - 2]).Bottom;
                }

                aPosition.btnPositionDelete.Click += new System.EventHandler(ClickbtnPositionDelete); ;
                aPosition.btnPositionPlace.Click += new System.EventHandler(ClickbtnPositionPlace);
                aPosition.btnRestore.Click += new System.EventHandler(ClickbtnRestore);
                aPosition.btnSelect.Click += new System.EventHandler(ClickbtnSelect);

                return aPosition;
            }
            public void highlightPosition(int position)                                               
            {
                HostPanel.Controls[position].BackColor = Color.LightBlue;
            }
            public void removePosition(int position)                                                  
            {
                HostPanel.Controls.Remove(this[position]);
                this.List.RemoveAt(position);

                if (HostPanel.Controls.Count > 0)
                {
                    for (int i = 0; i < HostPanel.Controls.Count; i++)
                    {
                        if (i == 0)
                        {
                            HostPanel.Controls[i].Top = 0;
                        }
                        else
                        {
                            HostPanel.Controls[i].Top = HostPanel.Controls[i - 1].Bottom;
                        }

                        HostPanel.Controls[i].Tag = i + 1;
                        ((Button)HostPanel.Controls[i].Controls["btnPositionDelete"]).Tag = i + 1;
                        ((TextBox)HostPanel.Controls[i].Controls["txtPositionIncrement"]).Tag = i + 1;
                        ((TextBox)HostPanel.Controls[i].Controls["txtPositionText"]).Tag = i + 1;
                        ((Button)HostPanel.Controls[i].Controls["btnPositionPlace"]).Tag = i + 1;
                        ((Button)HostPanel.Controls[i].Controls["btnSelect"]).Tag = i + 1;
                        ((Button)HostPanel.Controls[i].Controls["btnSelect"]).Text = (i + 1).ToString();
                        ((Button)HostPanel.Controls[i].Controls["btnRestore"]).Tag = i + 1;
                        ((Label)HostPanel.Controls[i].Controls["lblPositionFont"]).Tag = i + 1;
                        ((Label)HostPanel.Controls[i].Controls["lblPositionFontSize"]).Tag = i + 1;
                        ((Label)HostPanel.Controls[i].Controls["lblRotation"]).Tag = i + 1;
                    }

                    HostVScrollBar.Maximum = HostPanel.Controls.Count - 1;

                    HostPanel.Height = HostPanel.Controls[HostPanel.Controls.Count - 1].Bottom;
                    if (HostPanel.Height < HostVScrollBar.Height)
                    {
                        HostPanel.Height = HostVScrollBar.Height;
                    }
                    
                    if (HostPanel.Controls[HostPanel.Controls.Count - 1].Bottom < HostVScrollBar.Height)
                    {
                        HostVScrollBar.Enabled = false;
                    }
                }
            }
            public void removeAllPositions()                                                          
            {
                ((Panel)HostPanel).Controls.Clear();
            }
            public void ClicktxtPostitionText(Object sender, System.EventArgs e)                      
            {
                HostPanel.Controls[Convert.ToInt32(((TextBox)sender).Tag) - 1].BackColor = Color.LightBlue;
            }
            public void ClickbtnPositionDelete(Object sender, System.EventArgs e)                     
            {
                if (Cursor.Current == Cursors.Help)
                {
                    HostPanel.Cursor = Cursors.Default;
                    Help.ShowHelp(HostPanel, "gtleditor.chm", HelpNavigator.KeywordIndex, "Delete Stored Text Data Button");
                }
                else
                {
                    removePosition(Convert.ToInt32(((Button)sender).Tag) - 1);
                }
            }
            public void ClickbtnPositionPlace(Object sender, System.EventArgs e)                      
            {
                if (Cursor.Current == Cursors.Help)
                {
                    HostPanel.Cursor = Cursors.Default;
                    Help.ShowHelp(HostPanel, "gtleditor.chm", HelpNavigator.KeywordIndex, "Stored Text Data Area");
                }
                else
                {
                    string workingString;
                    Image positionImage;
                    positionTextDisplay workingPosition = ((positionTextDisplay)HostPanel.Controls[Convert.ToInt32(((Button)sender).Tag) - 1]);
                    
                    using (SolidBrush drawBrush = new SolidBrush(workingPosition.txtPositionText.ForeColor))
                    {
                        Regex _Regex = new Regex(@"#");
                        workingString = _Regex.Replace(workingPosition.txtPositionText.Text, "");

                        if (workingPosition.WorkingSize == "FullTile")
                        {
                            positionImage = FullTile.Image;
                            FullTile.Image = (Image)positionImage.Clone();
                            using (Graphics gPositionText = Graphics.FromImage(FullTile.Image))
                            {
                                gPositionText.TranslateTransform(workingPosition.PointPlacement.X, workingPosition.PointPlacement.Y);
                                gPositionText.RotateTransform(workingPosition.RotationDegrees);
                                gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                                gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                                gPositionText.ResetTransform();
                            }
                            FullTile.Invalidate();
                            FullTile.Update();
                        }
                        else
                        {
                            positionImage = HalfTile.Image;
                            HalfTile.Image = (Image)positionImage.Clone();
                            using (Graphics gPositionText = Graphics.FromImage(HalfTile.Image))
                            {
                                gPositionText.TranslateTransform(workingPosition.PointPlacement.X, workingPosition.PointPlacement.Y);
                                gPositionText.RotateTransform(workingPosition.RotationDegrees);
                                gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                                gPositionText.DrawString(workingString, workingPosition.txtPositionText.Font, drawBrush, 0.0f, 0.0f);
                                gPositionText.ResetTransform();
                            }
                            HalfTile.Invalidate();
                            HalfTile.Update();
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
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                WorkingTile.Focus();
            }

            public void ClickbtnSelect(Object sender, System.EventArgs e)
            {
                positionTextDisplay workingPosition = ((positionTextDisplay)HostPanel.Controls[Convert.ToInt32(((Button)sender).Tag) - 1]);
                
                ((Button)HostForm.Controls["groupBox1"].Controls["btnReplace"]).Text = "Replace " + ((Button)sender).Tag.ToString();
                ((Button)HostForm.Controls["groupBox1"].Controls["btnReplace"]).Tag = (Convert.ToInt32(((Button)sender).Tag) - 1);
                ((Button)HostForm.Controls["groupBox1"].Controls["btnReplace"]).Enabled = true;
            }
            public void ClickbtnRestore(Object sender, System.EventArgs e)                            
            {
                positionTextDisplay workingPosition = ((positionTextDisplay)HostPanel.Controls[Convert.ToInt32(((Button)sender).Tag) - 1]);
                
                ((TextBox)HostForm.Controls["groupBox1"].Controls["txtText"]).Font = workingPosition.txtPositionText.Font;
                ((TextBox)HostForm.Controls["groupBox1"].Controls["txtText"]).ForeColor = workingPosition.txtPositionText.ForeColor;

                if (workingPosition.txtPositionIncrement.Visible)
                {
                    ((CheckBox)HostForm.Controls["groupBox1"].Controls["chkIncrement"]).Checked = true;
                }
                ((Label)HostForm.Controls["groupBox1"].Controls["lblTextPlacement"]).Text = workingPosition.btnPositionPlace.Text;

                ((ScrollBar)HostForm.Controls["groupBox1"].Controls["hScrollText"]).Value = workingPosition.PointPlacement.X;
                ((ScrollBar)HostForm.Controls["groupBox1"].Controls["vScrollText"]).Value = workingPosition.PointPlacement.Y;

                ((TextBox)HostForm.Controls["groupBox1"].Controls["txtRotate"]).Text = workingPosition.RotationDegrees.ToString();
                ((ScrollBar)HostForm.Controls["groupBox1"].Controls["hScrollRotate"]).Value = workingPosition.RotationDegrees;

                ((TextBox)HostForm.Controls["groupBox1"].Controls["txtText"]).Text = workingPosition.txtPositionText.Text;
            }
        }
    }
}
