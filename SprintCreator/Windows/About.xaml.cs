using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SprintCreator.Windows
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        #region [Constructor]

        public About()
        {
            InitializeComponent();
        }

        #endregion

        #region [Events]

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("mailto:tarunsingh20@gmail.com; sudhakarreddy.pr@hotmail.com; zubair.m.ahmed@hotmail.com; rajendrosahu@gmail.com?Subject=Feedback on SprintCreator version 1.0");            
            this.Close();
        }

        private void CloseForm(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}
