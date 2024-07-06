using System;
using System.Data;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace positionDisplayControl
{
    [Serializable]
    public partial class positionTextDisplay : UserControl
    {
        //Local Variables
        private int introtationdegrees;
        private Point pointplacement;
        private string workingSize;
        private Boolean bolPositionIncrement;

        //Constructor
        public positionTextDisplay() 
        {
            InitializeComponent();
        }

        //Properties
        public int RotationDegrees   
        {
            set
            {
                introtationdegrees = value;
            }
            get
            {
                return introtationdegrees;
            }
        }
        public Point PointPlacement  
        {
            set
            {
                pointplacement = value;
            }
            get
            {
                return pointplacement;
            }
        }
        public string WorkingSize    
        {
            set
            {
                workingSize = value;
            }
            get
            {
                return workingSize;
            }
        }
        public Color TextBackColor   
        {
            set
            {
                this.txtPositionText.BackColor = value;
            }
        }
    }
}