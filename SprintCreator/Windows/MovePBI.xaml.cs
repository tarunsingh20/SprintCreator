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
    /// Interaction logic for MovePBI.xaml
    /// </summary>
    public partial class MovePBI : Window
    {
        #region [Variables]

        public List<int> selectedPBIs = new List<int>();

        #endregion

        #region [Constructors]

        public MovePBI()
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

            selectedPBIs = Helper.SelectedPBIs;

            lblMessage.Content = selectedPBIs.Count.ToString() + Helper.PBI_SELETED;

            cobMoveToIterations.Focus();
        }

        private void cobMoveToIterations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cobMoveToIterations.SelectedIndex > 0)
            {
                btnMovePBI.IsEnabled = true;
            }
            else
            {
                btnMovePBI.IsEnabled = false;
            }
        }

        private async void btnMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Helper.SelectedSourceIteration == int.Parse(cobMoveToIterations.SelectedValue.ToString()))
                {
                    lblMessage.Content = Helper.SELECT_DIFFERENT_ITERATION;
                    lblMessage.Foreground = Brushes.IndianRed;
                }
                else
                {
                    btnMovePBI.IsEnabled = false;

                    selectedPBIs = Helper.SelectedPBIs;

                    string newIterationPath = cobMoveToIterations.SelectedItem.ToString();

                    lblMessage.Content = Helper.ITERATIONS_MOVING;
                    lblMessage.Foreground = Brushes.Black;

                    bool success = await Task.Run<bool>(() => Helper.TfsWrapper.MoveSelectedPBIsToNewIteration(selectedPBIs, newIterationPath));

                    if (success)
                    {
                        this.DialogResult = true;

                        Helper.NewIteration = Helper.TfsWrapper.GetFullIterationPath(newIterationPath);
                        Helper.NewIterationID = int.Parse(cobMoveToIterations.SelectedValue.ToString());
                    }
                    else
                    {
                        this.DialogResult = false;

                        btnMovePBI.IsEnabled = true;
                    }

                    this.Close();
                }
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
