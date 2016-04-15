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
using STaRZ.TFSLibrary;

namespace SprintCreator.Windows
{
    /// <summary>
    /// Interaction logic for CopyTasksToSprintIteration.xaml
    /// </summary>
    public partial class CopyTasksToSprintIteration : Window
    {
        #region [Constructor]

        public CopyTasksToSprintIteration()
        {
            InitializeComponent();
        }

        #endregion

        #region [Events]

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Helper.PBIListForGrid.Count > 0)
            {
                dgPBIs.ItemsSource = Helper.PBIListForGrid;

                lblNewIteration.Content = Helper.NewIteration;
            }
        }

        private async void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgPBIs.SelectedItems.Count == 0)
                {
                    lblMessage.Content = "Please select at least one item from the Grid..!";
                    lblMessage.Foreground = Brushes.IndianRed;

                    await Task.Delay(3000);
                    lblMessage.Content = "";
                }
                else
                {
                    btnCopy.IsEnabled = false;
                    btnCancel.IsEnabled = false;

                    lblMessage.Content = "Please wait while the tasks are being copied to selected PBIs..";
                    lblMessage.Foreground = Brushes.Black;

                    List<TFSBacklogItem> selectedPBIsListFromGrid = new List<TFSBacklogItem>();

                    foreach (var selectedPBI in dgPBIs.SelectedItems)
                    {
                        selectedPBIsListFromGrid.Add((TFSBacklogItem)selectedPBI);
                    }

                    Helper.TasksCopiedTo = new List<int>();
                    foreach (TFSBacklogItem item in selectedPBIsListFromGrid)
                    {
                        Helper.TasksCopiedTo.Add(item.WorkItemID);
                    }

                    await Task.Run(() => Helper.TfsWrapper.CopyTasksToSelectedWorkItems(selectedPBIsListFromGrid, Helper.PBITaskListoCopy));

                    this.DialogResult = true;

                    this.Close();
                }
            }
            catch (Exception exc)
            {
                Log.CaptureException(exc);
                Log.LogFileWrite();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}
