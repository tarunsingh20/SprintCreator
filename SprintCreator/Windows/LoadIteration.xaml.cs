using System;
using System.Collections.Generic;
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
using SprintCreator.Common;

namespace SprintCreator.Windows
{
    /// <summary>
    /// Interaction logic for LoadIteration.xaml
    /// </summary>
    public partial class LoadIteration : Window
    {
        #region [Constructors]

        public LoadIteration()
        {
            InitializeComponent();
        }

        #endregion

        #region [Events]

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dictionary<int, string> Iterations = Helper.Iterations;

            cobMoveToIterations.ItemsSource = Iterations;

            cobMoveToIterations.SelectedValuePath = Helper.KEY;
            cobMoveToIterations.DisplayMemberPath = Helper.VALUE;

            cobMoveToIterations.SelectedIndex = 0;

            cobMoveToIterations.Focus();
        }

        private void cobMoveToIterations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cobMoveToIterations.SelectedIndex > 0)
            {
                btnLoadIteration.IsEnabled = true;
            }
            else
            {
                btnLoadIteration.IsEnabled = false;
            }
        }

        private void btnLoadIteration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newIterationPath = cobMoveToIterations.SelectedItem.ToString();

                Helper.NewIteration = Helper.TfsWrapper.GetFullIterationPath(newIterationPath);
                Helper.NewIterationID = int.Parse(cobMoveToIterations.SelectedValue.ToString());

                this.DialogResult = true;
            }
            catch (Exception exc)
            {
                Log.CaptureException(exc);
                Log.LogFileWrite();
            }
        }

        #endregion
    }
}
