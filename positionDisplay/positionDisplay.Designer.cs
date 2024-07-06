namespace positionDisplay
{
    partial class UserControl1
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
            this.txtIncrement = new System.Windows.Forms.TextBox();
            this.txtText = new System.Windows.Forms.TextBox();
            this.btnLocation = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtIncrement
            // 
            this.txtIncrement.Location = new System.Drawing.Point(5, 4);
            this.txtIncrement.Name = "txtIncrement";
            this.txtIncrement.Size = new System.Drawing.Size(39, 20);
            this.txtIncrement.TabIndex = 0;
            // 
            // txtText
            // 
            this.txtText.Location = new System.Drawing.Point(50, 4);
            this.txtText.Name = "txtText";
            this.txtText.Size = new System.Drawing.Size(79, 20);
            this.txtText.TabIndex = 1;
            // 
            // btnLocation
            // 
            this.btnLocation.Location = new System.Drawing.Point(135, 3);
            this.btnLocation.Name = "btnLocation";
            this.btnLocation.Size = new System.Drawing.Size(48, 23);
            this.btnLocation.TabIndex = 2;
            this.btnLocation.Text = "00,00";
            this.btnLocation.UseVisualStyleBackColor = true;
            // 
            // UserControl1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnLocation);
            this.Controls.Add(this.txtText);
            this.Controls.Add(this.txtIncrement);
            this.Name = "UserControl1";
            this.Size = new System.Drawing.Size(186, 29);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtIncrement;
        private System.Windows.Forms.TextBox txtText;
        private System.Windows.Forms.Button btnLocation;
    }
}
