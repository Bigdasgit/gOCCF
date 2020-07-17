using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCCFRecSys
{
    public class Edge
    {
        public double Message_Like = 1d;
        public double Message_Dislike = 1d;

        public double PreviousMessage_Like = 1d;
        public double PreviousMessage_Dislike = 1d;

        bool isPositive;

        public void SetLabel(bool isPositive)
        {
            if (isPositive == true)
                this.isPositive = true;
            else
                this.isPositive = false;
        }

        public bool GetLabel()
        {
            return isPositive;
        }
    }
}
