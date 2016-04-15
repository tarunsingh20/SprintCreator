using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Xml;
using SprintCreator.Common;
using STaRZ.TFSLibrary;

namespace SprintCreator.Windows
{
    /// <summary>
    /// Interaction logic for TFSSettings.xaml
    /// </summary>
    public partial class TFSSettings : Window
    {
        #region [Local Variables and Constants]

        ObservableCollection<string> GenericTaskCollection;

        bool TaskMovedUpOrDown = false;

        const int ClearTimer = 4000;

        #endregion

        #region [Constructors]

        public TFSSettings()
        {
            InitializeComponent();
            SetControls();
            btnLaunch.Content = "Launch";

            if (Helper.TFSConnectionSettings.ConnectAutomatically)
            {
                this.Hide();
                this.StartMainWindow();
                this.Close();
            }

            CheckGenericTasksXML();
        }

        public TFSSettings(string launchedFrom, Helper.ErrorType errorType = Helper.ErrorType.None)
        {
            string showErrorMessage = string.Empty;

            InitializeComponent();
            SetControls();

            switch (errorType)
            {
                case Helper.ErrorType.TFSWrapperError:
                    showErrorMessage = Helper.TFSSERVERNOTCONNECTEDERROR;
                    break;

                case Helper.ErrorType.TFSStoreError:
                    showErrorMessage = Helper.TFSWORKITEMSTOREINVALIDERROR;
                    break;

                case Helper.ErrorType.TFSProjectError:
                    showErrorMessage = Helper.TFSPROJECTINVALIDERROR;
                    break;
            }

            if (launchedFrom == "MainWindow")
            {
                btnLaunch.Content = "OK";
            }
            else
            {
                btnLaunch.Content = "Launch";
                ConnectionErrorMsg.Text = showErrorMessage;
                ConnectionErrorMsg.Visibility = System.Windows.Visibility.Visible;
            }

            CheckGenericTasksXML();
        }

        #endregion

        #region [Events]

        private void btnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (btnLaunch.Content.ToString().ToLower() != "ok")
            {
                if (UpdateOptions())
                {
                    this.Hide();

                    Helper.SettingsError = Helper.ErrorType.None;
                    //re-initialize the TfsWrapper as the settings/credentials might have changed
                    Helper.TfsWrapper = null;

                    ConnectionErrorMsg.Visibility = System.Windows.Visibility.Hidden;

                    this.StartMainWindow();

                    if (!chkSavePassword.IsChecked.Value)
                    {
                        Helper.TFSConnectionSettings.UserName = string.Empty;
                        Helper.TFSConnectionSettings.Password = string.Empty;
                        Helper.TFSConnectionSettings.SaveUserNameAndPassword = false;
                        Helper.TFSConnectionSettings.ConnectAutomatically = false;
                    }

                    Helper.SaveTFSConnectionSettingsToConfiguration();
                    this.Close();
                }

            }
            else
            {
                if (UpdateOptions())
                {
                    if (!chkSavePassword.IsChecked.Value)
                    {
                        Helper.TFSConnectionSettings.UserName = string.Empty;
                        Helper.TFSConnectionSettings.Password = string.Empty;
                        Helper.TFSConnectionSettings.SaveUserNameAndPassword = false;
                        Helper.TFSConnectionSettings.ConnectAutomatically = false;
                    }

                    Helper.SaveTFSConnectionSettingsToConfiguration();
                    this.Close();
                }
            }
        }

        private void ChkSavePassword_Changed(object sender, RoutedEventArgs e)
        {
            if (chkSavePassword.IsChecked == false)
            {
                chkConnectAuto.IsChecked = false;
                chkConnectAuto.IsEnabled = false;
                chkConnectAuto.IsTabStop = false;
            }
            else
            {
                chkConnectAuto.IsEnabled = true;
                chkConnectAuto.IsTabStop = true;
            }

        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            string serverName;
            string teamProjectName;

            try
            {
                bool connected = TFSWrapper.ConnectToTFS(out serverName, out teamProjectName);

                if (connected)
                {
                    if (!serverName.Equals(string.Empty))
                    {
                        txtTFSServer.Text = serverName;
                    }

                    if (!teamProjectName.Equals(string.Empty))
                    {
                        txtTFSProject.Text = teamProjectName;
                    }
                }
                else
                {
                    DialogBox.ShowError("Unable to connect TFS project !!!");
                }
            }
            catch (Exception exc)
            {
                Log.CaptureException(exc);
                Log.LogFileWrite();
            }
        }

        private void btnAddTask_Click(object sender, RoutedEventArgs e)
        {
            string newTaskTitle = txtNewTask.Text.Trim();

            txtNewTask.Text = "";
            txtNewTask.Focus();

            if (!newTaskTitle.Equals(string.Empty))
            {
                GenericTaskCollection.Add(newTaskTitle);

                lstGenericTasks.ItemsSource = null;
                lstGenericTasks.ItemsSource = GenericTaskCollection;

                RefreshGenericTasksXML();
            }
            else
            {
                ShowErrorMessage("Please add the task description to create a new task..!");
            }
        }

        private void btnRemoveTask_Click(object sender, RoutedEventArgs e)
        {
            txtNewTask.Text = "";
            txtNewTask.Focus();

            if (lstGenericTasks.SelectedIndex >= 0)
            {
                List<string> selectedItems = new List<string>();

                foreach (object selection in lstGenericTasks.SelectedItems)
                {
                    selectedItems.Add(selection.ToString());
                }

                foreach (string item in selectedItems)
                {
                    GenericTaskCollection.Remove(item);
                }

                RefreshGenericTasksXML();

                lstGenericTasks.SelectedIndex = -1;
            }
            else
            {
                ShowErrorMessage("Please select a task to delete..!");
            }
        }

        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (lstGenericTasks.SelectedIndex == 0)
            {
                ShowErrorMessage("Can't move this task Up..!");
            }
            else if (lstGenericTasks.SelectedIndex > 0)
            {
                int selectedTaskIndex = lstGenericTasks.SelectedIndex;

                GenericTaskCollection.Move(selectedTaskIndex, selectedTaskIndex - 1);

                lstGenericTasks.ItemsSource = null;

                lstGenericTasks.ItemsSource = GenericTaskCollection;

                lstGenericTasks.SelectedIndex = selectedTaskIndex - 1;

                TaskMovedUpOrDown = true;
            }
            else
            {
                ShowErrorMessage("Select a task from the Grid to move up..!");

            }
        }

        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (lstGenericTasks.Items.Count == lstGenericTasks.SelectedIndex + 1)
            {
                ShowErrorMessage("Can't move this task down..!");
            }
            else if (lstGenericTasks.SelectedIndex >= 0)
            {
                int selectedTaskIndex = lstGenericTasks.SelectedIndex;

                GenericTaskCollection.Move(selectedTaskIndex, selectedTaskIndex + 1);

                lstGenericTasks.ItemsSource = null;

                lstGenericTasks.ItemsSource = GenericTaskCollection;

                lstGenericTasks.SelectedIndex = selectedTaskIndex + 1;

                TaskMovedUpOrDown = true;
            }
            else
            {
                ShowErrorMessage("Select a task from the Grid to move down..!");
            }
        }

        private void TFSSettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // On closing the Settings window, saving the tasks list to GeneriTasks.XML file.

            if (lstGenericTasks.Items.Count > 0)
            {
                if (TaskMovedUpOrDown)
                {
                    RefreshGenericTasksXML();
                    TaskMovedUpOrDown = false;
                }
            }
        }

        #endregion

        #region [Private Methods]

        private void SetControls()
        {
            txtTFSServer.Text = Helper.TFSConnectionSettings.TfsServer;
            txtTFSProject.Text = Helper.TFSConnectionSettings.TfsProject;
            if (Helper.TFSConnectionSettings.Domain.Length == 0)
            {
                Helper.TFSConnectionSettings.Domain = Helper.DOMAIN;
            }
            txtUsername.Text = Helper.TFSConnectionSettings.Domain + @"\" + Helper.TFSConnectionSettings.UserName;
            txtPassword.Password = Helper.TFSConnectionSettings.Password;
            chkSavePassword.IsChecked = Helper.TFSConnectionSettings.SaveUserNameAndPassword;
            chkConnectAuto.IsChecked = Helper.TFSConnectionSettings.ConnectAutomatically;
            if (Helper.TFSConnectionSettings.DefectWorkItemType.ToLower() == "defect")
            {
                cobDefectType.SelectedIndex = 1;
            }
            else
            {
                cobDefectType.SelectedIndex = 0;
            }

            ConnectionErrorMsg.Visibility = System.Windows.Visibility.Hidden;

        }

        public bool UpdateOptions()
        {
            bool fieldSpecified = true;
            Helper.TFSConnectionSettings.TfsServer = txtTFSServer.Text.Trim();
            Helper.TFSConnectionSettings.TfsProject = txtTFSProject.Text.Trim();
            if (txtUsername.Text.Trim().Contains('\\'))
            {
                string[] credential = txtUsername.Text.Trim().Split('\\');
                Helper.TFSConnectionSettings.Domain = credential[0];
                Helper.TFSConnectionSettings.UserName = credential[1];
            }
            else
            {
                Helper.TFSConnectionSettings.Domain = Helper.DOMAIN;
                Helper.TFSConnectionSettings.UserName = txtUsername.Text.Trim();
            }
            Helper.TFSConnectionSettings.Password = txtPassword.Password;
            Helper.TFSConnectionSettings.SaveUserNameAndPassword = chkSavePassword.IsChecked != null ? chkSavePassword.IsChecked.Value : false;
            Helper.TFSConnectionSettings.ConnectAutomatically = chkConnectAuto.IsChecked != null ? chkConnectAuto.IsChecked.Value : false;
            Helper.TFSConnectionSettings.DefectWorkItemType = cobDefectType.Text;

            if (Helper.TFSConnectionSettings.TfsServer.Length == 0 ||
                Helper.TFSConnectionSettings.TfsProject.Length == 0 ||
                Helper.TFSConnectionSettings.UserName.Length == 0 ||
                Helper.TFSConnectionSettings.Password.Length == 0)
            {
                ConnectionErrorMsg.Text = "Please check if TFS Server URL/TFS Project/Username/password are specified.";
                ConnectionErrorMsg.Visibility = System.Windows.Visibility.Visible;
                fieldSpecified = false;
            }

            return fieldSpecified;
        }

        private void StartMainWindow()
        {
            if (SplashStartup.StartSplash())
            {
                MainWindow mainForm = new MainWindow();
                mainForm.Show();
                this.Close();
            }
        }

        private void CheckGenericTasksXML()
        {
            GenericTaskCollection = Helper.GetGenericTasksFromXML();

            lstGenericTasks.ItemsSource = GenericTaskCollection;
        }

        private void RefreshGenericTasksXML()
        {
            try
            {
                XmlDocument AllTasks = new XmlDocument();
                string xmlDocPath = Helper.GetGenericTasksXMLPath();
                AllTasks.Load(xmlDocPath);

                AllTasks.ChildNodes[1].InnerXml = "";

                if (GenericTaskCollection.Count > 0)
                {
                    foreach (string task in GenericTaskCollection)
                    {
                        XmlElement newTask = AllTasks.CreateElement(TFSConstants.WorkItemTypes.Task);
                        newTask.SetAttribute("Title", task);

                        AllTasks.ChildNodes[1].AppendChild(newTask);
                    }
                }

                AllTasks.Save(xmlDocPath);

                AllTasks = null;
                GC.Collect();
            }
            catch (Exception exc)
            {
                Log.CaptureException(exc);
                Log.LogFileWrite();
            }
        }

        private async void ShowErrorMessage(string message)
        {
            lblMessage.Content = message;
            lblMessage.Foreground = Brushes.IndianRed;

            await Task.Delay(ClearTimer);
            lblMessage.Content = "";
        }

        #endregion
    }
}
