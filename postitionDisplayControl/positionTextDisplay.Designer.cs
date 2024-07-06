using System.Runtime.Serialization;

namespace positionDisplayControl
{
    partial class positionTextDisplay
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnPositionDelete = new System.Windows.Forms.Button();
            this.txtPositionText = new System.Windows.Forms.TextBox();
            this.txtPositionIncrement = new System.Windows.Forms.TextBox();
            this.btnPositionPlace = new System.Windows.Forms.Button();
            this.btnRestore = new System.Windows.Forms.Button();
            this.lblPositionFont = new System.Windows.Forms.Label();
            this.lblPositionFontSize = new System.Windows.Forms.Label();
            this.lblRotation = new System.Windows.Forms.Label();
            this.btnSelect = new System.Windows.Forms.Button();
            this.cbInclude = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnPositionDelete
            // 
            this.btnPositionDelete.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPositionDelete.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPositionDelete.ForeColor = System.Drawing.Color.Red;
            this.btnPositionDelete.Location = new System.Drawing.Point(1, 24);
            this.btnPositionDelete.Margin = new System.Windows.Forms.Padding(0);
            this.btnPositionDelete.Name = "btnPositionDelete";
            this.btnPositionDelete.Size = new System.Drawing.Size(25, 25);
            this.btnPositionDelete.TabIndex = 0;
            this.btnPositionDelete.Text = "X";
            this.btnPositionDelete.UseVisualStyleBackColor = false;
            // 
            // txtPositionText
            // 
            this.txtPositionText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPositionText.Location = new System.Drawing.Point(35, 0);
            this.txtPositionText.Margin = new System.Windows.Forms.Padding(0);
            this.txtPositionText.MinimumSize = new System.Drawing.Size(75, 20);
            this.txtPositionText.Multiline = true;
            this.txtPositionText.Name = "txtPositionText";
            this.txtPositionText.Size = new System.Drawing.Size(156, 25);
            this.txtPositionText.TabIndex = 1;
            // 
            // txtPositionIncrement
            // 
            this.txtPositionIncrement.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPositionIncrement.Location = new System.Drawing.Point(35, 0);
            this.txtPositionIncrement.Margin = new System.Windows.Forms.Padding(1);
            this.txtPositionIncrement.Name = "txtPositionIncrement";
            this.txtPositionIncrement.Size = new System.Drawing.Size(25, 20);
            this.txtPositionIncrement.TabIndex = 2;
            this.txtPositionIncrement.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtPositionIncrement.Visible = false;
            // 
            // btnPositionPlace
            // 
            this.btnPositionPlace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPositionPlace.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPositionPlace.Location = new System.Drawing.Point(245, 18);
            this.btnPositionPlace.Margin = new System.Windows.Forms.Padding(0);
            this.btnPositionPlace.Name = "btnPositionPlace";
            this.btnPositionPlace.Size = new System.Drawing.Size(45, 37);
            this.btnPositionPlace.TabIndex = 3;
            this.btnPositionPlace.Text = "->";
            this.btnPositionPlace.UseVisualStyleBackColor = false;
            // 
            // btnRestore
            // 
            this.btnRestore.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRestore.Location = new System.Drawing.Point(229, 18);
            this.btnRestore.Margin = new System.Windows.Forms.Padding(0);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(25, 37);
            this.btnRestore.TabIndex = 6;
            this.btnRestore.Text = "^";
            this.btnRestore.UseVisualStyleBackColor = true;
            // 
            // lblPositionFont
            // 
            this.lblPositionFont.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPositionFont.Location = new System.Drawing.Point(32, 35);
            this.lblPositionFont.Name = "lblPositionFont";
            this.lblPositionFont.Size = new System.Drawing.Size(165, 20);
            this.lblPositionFont.TabIndex = 7;
            this.lblPositionFont.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPositionFontSize
            // 
            this.lblPositionFontSize.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPositionFontSize.Location = new System.Drawing.Point(194, 35);
            this.lblPositionFontSize.Name = "lblPositionFontSize";
            this.lblPositionFontSize.Size = new System.Drawing.Size(30, 25);
            this.lblPositionFontSize.TabIndex = 8;
            this.lblPositionFontSize.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblRotation
            // 
            this.lblRotation.Location = new System.Drawing.Point(194, 2);
            this.lblRotation.Name = "lblRotation";
            this.lblRotation.Size = new System.Drawing.Size(29, 23);
            this.lblRotation.TabIndex = 9;
            this.lblRotation.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnSelect
            // 
            this.btnSelect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSelect.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSelect.ForeColor = System.Drawing.Color.Green;
            this.btnSelect.Location = new System.Drawing.Point(1, 0);
            this.btnSelect.Margin = new System.Windows.Forms.Padding(0);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(25, 24);
            this.btnSelect.TabIndex = 10;
            this.btnSelect.Text = "S";
            this.btnSelect.UseVisualStyleBackColor = false;
            // 
            // cbInclude
            // 
            this.cbInclude.AutoSize = true;
            this.cbInclude.Checked = true;
            this.cbInclude.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbInclude.Location = new System.Drawing.Point(229, 2);
            this.cbInclude.Name = "cbInclude";
            this.cbInclude.Size = new System.Drawing.Size(61, 17);
            this.cbInclude.TabIndex = 11;
            this.cbInclude.Text = "Include";
            this.cbInclude.UseVisualStyleBackColor = true;
            // 
            // positionTextDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.cbInclude);
            this.Controls.Add(this.btnSelect);
            this.Controls.Add(this.lblRotation);
            this.Controls.Add(this.lblPositionFontSize);
            this.Controls.Add(this.lblPositionFont);
            this.Controls.Add(this.btnRestore);
            this.Controls.Add(this.btnPositionPlace);
            this.Controls.Add(this.txtPositionIncrement);
            this.Controls.Add(this.txtPositionText);
            this.Controls.Add(this.btnPositionDelete);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "positionTextDisplay";
            this.Size = new System.Drawing.Size(300, 60);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Button btnPositionDelete;
        public System.Windows.Forms.TextBox txtPositionText;
        public System.Windows.Forms.TextBox txtPositionIncrement;
        public System.Windows.Forms.Button btnPositionPlace;
        public System.Windows.Forms.Button btnRestore;
        public System.Windows.Forms.Label lblPositionFont;
        public System.Windows.Forms.Label lblPositionFontSize;
        public System.Windows.Forms.Label lblRotation;
        public System.Windows.Forms.Button btnSelect;
        public System.Windows.Forms.CheckBox cbInclude;
    }
}
