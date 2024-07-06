namespace gtlDisplay
{
    partial class gtlTileDisplay
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
            this.fullTile = new System.Windows.Forms.PictureBox();
            this.halfTile = new System.Windows.Forms.PictureBox();
            this.smallTile = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.fullTile)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.halfTile)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.smallTile)).BeginInit();
            this.SuspendLayout();
            // 
            // fullTile
            // 
            this.fullTile.Location = new System.Drawing.Point(3, 0);
            this.fullTile.Name = "fullTile";
            this.fullTile.Size = new System.Drawing.Size(83, 79);
            this.fullTile.TabIndex = 0;
            this.fullTile.TabStop = false;
            // 
            // halfTile
            // 
            this.halfTile.Location = new System.Drawing.Point(92, 0);
            this.halfTile.Name = "halfTile";
            this.halfTile.Size = new System.Drawing.Size(83, 79);
            this.halfTile.TabIndex = 1;
            this.halfTile.TabStop = false;
            // 
            // smallTile
            // 
            this.smallTile.Location = new System.Drawing.Point(181, 0);
            this.smallTile.Name = "smallTile";
            this.smallTile.Size = new System.Drawing.Size(15, 15);
            this.smallTile.TabIndex = 2;
            this.smallTile.TabStop = false;
            // 
            // gtlTileDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.smallTile);
            this.Controls.Add(this.halfTile);
            this.Controls.Add(this.fullTile);
            this.Name = "gtlTileDisplay";
            this.Size = new System.Drawing.Size(294, 81);
            ((System.ComponentModel.ISupportInitialize)(this.fullTile)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.halfTile)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.smallTile)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.PictureBox fullTile;
        public System.Windows.Forms.PictureBox halfTile;
        public System.Windows.Forms.PictureBox smallTile;
    }
}
