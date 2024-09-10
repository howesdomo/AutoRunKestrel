using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client.Components
{
    public class ErrorContentTextBlock : System.Windows.Controls.TextBlock
    {
        public ErrorContentTextBlock()
        {
            this.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            this.Margin = new System.Windows.Thickness(5, 0, 0, 0);
            this.TextWrapping = System.Windows.TextWrapping.WrapWithOverflow;
            this.Foreground = System.Windows.Media.Brushes.White;
            this.Background = System.Windows.Media.Brushes.Red;
            this.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        }
    }
}
