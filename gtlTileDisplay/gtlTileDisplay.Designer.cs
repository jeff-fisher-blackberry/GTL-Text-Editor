namespace gtlTileDisplay
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
            this.fullTile.Location = new System.Drawing.Point(4, 3);
            this.fullTile.Name = "fullTile";
            this.fullTile.Size = new System.Drawing.Size(78, 75);
            this.fullTile.TabIndex = 0;
            this.fullTile.TabStop = false;
            // 
            // halfTile
            // 
            this.halfTile.Location = new System.Drawing.Point(107, 3);
            this.halfTile.Name = "halfTile";
            this.halfTile.Size = new System.Drawing.Size(78, 75);
            this.halfTile.TabIndex = 1;
            this.halfTile.TabStop = false;
            // 
            // smallTile
            // 
            this.smallTile.Location = new System.Drawing.Point(210, 3);
            this.smallTile.Name = "smallTile";
            this.smallTile.Size = new System.Drawing.Size(78, 75);
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
            this.Size = new System.Drawing.Size(292, 82);
            ((System.ComponentModel.ISupportInitialize)(this.fullTile)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.halfTile)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.smallTile)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox fullTile;
        private System.Windows.Forms.PictureBox halfTile;
        private System.Windows.Forms.PictureBox smallTile;
    }
}
