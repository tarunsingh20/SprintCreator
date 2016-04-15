using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using SprintCreator.Common;
using SprintCreator.Progressbar;
using STaRZ.CryptoLibrary;
using STaRZ.TFSLibrary;
using STaRZ.WinAPI;

namespace SprintCreator.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        #region [Local Variables]

        int selectedIterationID = 0;
        int clearMessageDelay = 4000;
        int initialTaskCount = 0;
        int selectedTaskID = 0;
        int linkOpened = 0;
        int prevWorkItemID = 0;

        bool TaskEdited = false;
        bool TasksRetrieved = false;
        bool TaskLinkClicked = false;
        bool RowCompletelySelected = false;

        List<int> selectedPBIs = new List<int>();
        List<int> deletedTasks = new List<int>();
        List<PBITask> taskList;

        List<TFSBacklogItem> PBIList = new List<TFSBacklogItem>();
        List<TFSBacklogItem> PBIListForGrid = new List<TFSBacklogItem>();
        List<TFSBacklogItem> PrevPBIList = new List<TFSBacklogItem>();
        List<TFSBacklogItem> SelectedPBIList = new List<TFSBacklogItem>();

        #endregion

        #region [Constructor]
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region [Events]

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dictionary<int, string> Iterations = Helper.Iterations;

            cobIterations.ItemsSource = Iterations;

            cobIterations.SelectedValuePath = Helper.KEY;
            cobIterations.DisplayMemberPath = Helper.VALUE;

            cobIterations.SelectedIndex = 0;

            Helper.SelectedPBIs = null;

            cobIterations.Focus();

            btnSaveTasks.IsEnabled = true;
            btnCopyTasksTo.IsEnabled = true;
            btnAddGenericTasks.IsEnabled = true;
        }

        private void btnGetPBI_Click(object sender, RoutedEventArgs e)
        {
            CheckSaveTasks();

            btnGetPBI.IsEnabled = false;
            btnUndoMovePBIs.IsEnabled = false;

            tvPBIList.Items.Clear();

            dgPBIs.ItemsSource = null;
            dgTasks.ItemsSource = null;

            lblSelectedIteration.Content = "";
            lblNewIteration.Content = "";
            lblSelectedWorkItemID.Content = "";

            LoadWorkItems();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            TFSSettings settingsForm = new TFSSettings("MainWindow");
            settingsForm.ShowDialog();
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            About aboutForm = new About();
            aboutForm.ShowDialog();
        }

        private void cobIterations_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cobIterations.SelectedIndex > 0)
            {
                btnGetPBI.IsEnabled = true;
            }
            else
            {
                btnGetPBI.IsEnabled = false;
            }

            selectedIterationID = int.Parse(cobIterations.SelectedValue.ToString());
            Helper.SelectedSourceIteration = selectedIterationID;

            lblMessage.Content = "";

            btnMovePBIs.IsEnabled = false;
        }

        private void btnMovePBIs_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";

            PrevPBIList = null;

            GetSelectedPBIFromTreeView();

            Helper.SelectedPBIs = selectedPBIs;

            // if more than 1 PBIs are selected show an info msg else show an error
            if (selectedPBIs.Count > 0)
            {
                lblSelectedWorkItemID.Content = "";
                dgTasks.ItemsSource = null;
                PrevPBIList = PBIList;

                MovePBI newWindow = new MovePBI();
                if (newWindow.ShowDialog() == true)
                {
                    LoadWorkItems();

                    ProgressDialog.Execute(this, Helper.WORKITEMS_MOVED, (bw) => { PopulateGridWithLatestIteration(); });

                    dgPBIs.ItemsSource = null;
                    dgPBIs.ItemsSource = PBIListForGrid;

                    btnUndoMovePBIs.IsEnabled = true;

                    lblNewIteration.Content = Helper.NewIteration;
                    Helper.NewIteration = "";
                }
            }
            else
            {
                DialogBox.ShowError(Helper.SELECT_A_PBI);
            }
        }

        private void btnUndoMovePBIs_Click(object sender, RoutedEventArgs e)
        {
            if (dgPBIs.SelectedItems.Count == 0)
            {
                DialogBox.ShowError(Helper.SELECT_A_GRID_ITEM);
            }
            else if (lblSelectedIteration.Content.Equals(lblNewIteration.Content))
            {
                DialogBox.ShowError(Helper.SELECT_DIFFERENT_ITERATION);
            }
            else
            {
                List<int> PBIIDList = new List<int>();

                foreach (var pbiRow in dgPBIs.SelectedItems)
                {
                    TFSBacklogItem pbi = (TFSBacklogItem)pbiRow;
                    PBIIDList.Add(pbi.WorkItemID);
                }

                string selectedItems = CreateStringFromListOfInt(PBIIDList);

                if (cobIterations.SelectedIndex == 0)
                {
                    DialogBox.ShowError(Helper.NO_PREV_ITERATION);
                }
                else
                {
                    if (DialogBox.Show(Helper.MOVEBACK_CONFIRMATION + Environment.NewLine + selectedItems) == true)
                    {
                        string newIterationPath = cobIterations.SelectedItem.ToString();
                        bool success = false;

                        ProgressDialog.Execute(this, Helper.PBI_MOVING, (bw) => { success = Helper.TfsWrapper.MoveSelectedPBIsToNewIteration(PBIIDList, newIterationPath); });

                        if (success)
                        {
                            selectedPBIs = null;
                            Helper.SelectedPBIs = null;

                            //Refresh the Work Item Tree
                            LoadWorkItems();

                            //Refresh the Work Item Grid
                            PBIListForGrid.RemoveAll(p => PBIIDList.Contains(p.WorkItemID));
                            dgPBIs.ItemsSource = null;
                            dgPBIs.ItemsSource = PBIListForGrid;

                            dgTasks.ItemsSource = null;
                            lblSelectedWorkItemID.Content = "";

                            if (dgPBIs.Items.Count == 0)
                            {
                                btnUndoMovePBIs.IsEnabled = false;
                                lblNewIteration.Content = "";
                            }
                        }
                        else
                        {
                            btnUndoMovePBIs.IsEnabled = true;
                            DialogBox.ShowError(Helper.CANT_MOVE_PBI_ERROR);
                        }
                    }
                }
            }
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            DialogBox.ShowInfo(Helper.TASKGRID_INFO);
        }

        private void dgTasks_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (dgTasks.SelectedItems.Count == 1)
            {
                if (e.Key == Key.Delete)
                {
                    try
                    {
                        PBITask task = (PBITask)dgTasks.SelectedItem;

                        if (task.WorkItemID != 0)
                        {
                            deletedTasks.Add(task.WorkItemID);
                        }
                    }
                    catch
                    {
                        // Suppressing Invalid cast exception when the user selects the empty row in the grid and presses delete button.
                    }
                }
            }
            else if (dgTasks.SelectedItems.Count > 1)
            {
                if (e.Key == Key.Delete)
                {
                    foreach (var selectedRow in dgTasks.SelectedItems)
                    {
                        try
                        {
                            PBITask task = (PBITask)selectedRow;

                            if (task.WorkItemID != 0)
                            {
                                deletedTasks.Add(task.WorkItemID);
                            }
                        }
                        catch
                        {
                            // Suppressing Invalid cast exception when the user selects the empty row in the grid and presses delete button.
                        }
                    }
                }
            }
        }

        private void btnHelpGrid_Click(object sender, RoutedEventArgs e)
        {
            DialogBox.ShowInfo(Helper.PBIGRID_INFO);
        }

        private void dgTasks_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            TaskEdited = true;
        }

        private void btnLoadIteration_Click(object sender, RoutedEventArgs e)
        {
            CheckSaveTasks();

            //Load a new Iteration
            LoadIteration loadIteration = new LoadIteration();
            if (loadIteration.ShowDialog() == true)
            {
                ProgressDialog.Execute(this, Helper.PBI_RETRIEVING, (bw) => { PopulateGridWithLatestIteration(); });

                lblSelectedWorkItemID.Content = "";
                dgTasks.ItemsSource = null;

                dgPBIs.ItemsSource = null;
                dgPBIs.ItemsSource = PBIListForGrid;

                lblNewIteration.Content = Helper.NewIteration;
                Helper.NewIteration = "";

                if (PBIListForGrid.Count == 0)
                {
                    DialogBox.ShowInfo(Helper.NO_PBI);
                    lblNewIteration.Content = "";
                }
            }

            TasksRetrieved = false;
        }

        private async void dgPBIs_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dgPBIs.SelectedItems.Count > 1)
            {
                CheckSaveTasks();
                dgTasks.ItemsSource = null;
                lblTasksInfo.Content = "";
                lblSelectedWorkItemID.Content = "";
            }
            else
            {
                if (dgPBIs.SelectedIndex >= 0)
                {
                    CheckSaveTasks();

                    btnUndoMovePBIs.IsEnabled = true;
                    lblSelectedWorkItemID.Content = "";
                    lblTasksInfo.Content = "";

                    deletedTasks = new List<int>();
                    dgTasks.ItemsSource = null;

                    if (dgPBIs.CurrentColumn != null)
                    {
                        RowCompletelySelected = true;

                        //If the user clicks on Work Item ID cell, open the work item in TFS Web
                        TFSBacklogItem selectedItem = (TFSBacklogItem)dgPBIs.SelectedItem;

                        prevWorkItemID = selectedItem.WorkItemID;

                        if (dgPBIs.CurrentColumn.Header.ToString().Equals("ID"))
                        {
                            string urlString = string.Empty;
                            bool ieWindowOpen = false;
                            int id = selectedItem.WorkItemID;

                            TasksRetrieved = false;

                            linkOpened = id;

                            await Task.Run(() => OpenInTFSWeb(ref urlString, ref ieWindowOpen, id));
                        }
                        else
                        {
                            await RetrieveTasks();
                            dgTasks.SelectedIndex = -1;
                        }
                    }
                }
            }
        }

        private async void dgPBIs_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dgPBIs.CurrentCell.Column != null)
            {
                if (dgPBIs.CurrentCell.Column.Header.ToString().Equals("ID"))
                {
                    if (dgPBIs.SelectedItem != null)
                    {
                        TFSBacklogItem selectedItem = (TFSBacklogItem)dgPBIs.SelectedItem;

                        string urlString = string.Empty;
                        bool ieWindowOpen = false;
                        int id = selectedItem.WorkItemID;

                        if (linkOpened != id)
                        {
                            linkOpened = id;
                            await Task.Run(() => OpenInTFSWeb(ref urlString, ref ieWindowOpen, id));
                        }
                    }
                    else
                    {
                        RowCompletelySelected = false;
                    }
                }
                else if (RowCompletelySelected)
                {
                    if (!TasksRetrieved)
                    {
                        await RetrieveTasks();
                        dgTasks.SelectedIndex = -1;
                    }
                }
            }
            else
            {
                RowCompletelySelected = false;
            }
        }

        private async void btnSaveTasks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (taskList != null)
                {
                    bool confirmSaving = false;

                    if (deletedTasks.Count == 1)
                    {
                        if (DialogBox.Show(Helper.DELETE_TASKS_CONFIRMATION + Environment.NewLine + deletedTasks[0].ToString()) == true)
                        {
                            confirmSaving = true;
                        }
                        else
                        {
                            confirmSaving = false;
                        }
                    }
                    else if (deletedTasks.Count > 1)
                    {
                        //   taskList.FindAll(x => x.WorkItemTitle.Trim().Equals(string.Empty)).ForEach(x => deletedTasks.Add(x.WorkItemID));                        
                        string taskIds = CreateStringFromListOfInt(deletedTasks);

                        if (DialogBox.Show(Helper.DELETE_TASKS_CONFIRMATION + Environment.NewLine + taskIds, Helper.CONFIRM) == true)
                        {
                            confirmSaving = true;
                        }
                        else
                        {
                            confirmSaving = false;
                        }
                    }
                    else if (dgTasks.Items.Count - 1 > initialTaskCount)
                    {
                        confirmSaving = true;
                    }

                    if (confirmSaving || TaskEdited)
                    {
                        deletedTasks = new List<int>();

                        TFSBacklogItem pbi = (TFSBacklogItem)dgPBIs.SelectedItem;

                        if (taskList.FindAll(t => t.WorkItemTitle == null).Count > 0)
                        {
                            DialogBox.ShowError(Helper.EMPTY_TASK);
                        }
                        else
                        {
                            taskList.RemoveAll(x => x.WorkItemTitle == null);
                            taskList.RemoveAll(x => x.WorkItemTitle.Trim().Equals(string.Empty));

                            ProgressDialog.Execute(this, Helper.TASKS_SAVING, (bw) => { Helper.TfsWrapper.SaveTasks(taskList, pbi.WorkItemID); });

                            await RetrieveTasks();
                        }

                        TaskEdited = false;
                    }
                    else
                    {
                        DialogBox.ShowInfo(Helper.NO_TASKS_TO_SAVE);
                    }
                }
                else
                {
                    DialogBox.ShowInfo(Helper.NO_TASKS_TO_SAVE);
                }
            }
            catch (Exception exc)
            {
                Log.CaptureException(exc);
                Log.LogFileWrite();
            }
        }

        private async void btnAddGenericTasks_Click(object sender, RoutedEventArgs e)
        {
            if (dgPBIs.SelectedItems.Count > 0)
            {
                List<TFSBacklogItem> PBIListToCreateTasks = new List<TFSBacklogItem>();
                List<int> pbiIDsList = new List<int>();

                foreach (var pbiRow in dgPBIs.SelectedItems)
                {
                    TFSBacklogItem pbi = (TFSBacklogItem)pbiRow;
                    PBIListToCreateTasks.Add(pbi);

                    pbiIDsList.Add(pbi.WorkItemID);
                }

                string pbiIds = CreateStringFromListOfInt(pbiIDsList);

                if (DialogBox.Show(Helper.GENERIC_TASKS_CONFIRMATION + Environment.NewLine + pbiIds) == true)
                {
                    // Add generic tasks from Settings screen to the selected items in dgPBIs grid.
                    ObservableCollection<string> genericTaskTitles = Helper.GetGenericTasksFromXML();

                    if (genericTaskTitles.Count == 0)
                    {
                        DialogBox.ShowError(Helper.NO_GENERIC_TASKS);
                    }
                    else
                    {
                        ProgressDialog.Execute(this, Helper.GENERIC_TASKS_ADDING, (bw) => { Helper.TfsWrapper.GenerateGenericTasksForPBI(PBIListToCreateTasks, genericTaskTitles); });

                        ProgressDialog.Execute(this, Helper.GENERIC_TASKS_ADDED, (bw) => { PopulateGridWithLatestIteration(); });

                        dgPBIs.ItemsSource = null;
                        dgPBIs.ItemsSource = PBIListForGrid;

                        if (PBIListToCreateTasks.Count == 1)
                        {
                            int index = -1;
                            foreach (TFSBacklogItem pbi in PBIListToCreateTasks)
                            {
                                index = PBIListForGrid.FindIndex(x => x.WorkItemID == pbi.WorkItemID);
                            }

                            dgPBIs.SelectedIndex = index;
                            await RetrieveTasks();
                            dgTasks.SelectedIndex = -1;
                        }
                        else
                        {
                            lblSelectedWorkItemID.Content = "";
                            dgTasks.ItemsSource = null;
                        }
                    }
                }
            }
            else
            {
                DialogBox.ShowError(Helper.SELECT_A_GRID_ITEM);
            }
        }

        private async void btnCopyTasksTo_Click(object sender, RoutedEventArgs e)
        {
            bool emptyRowSelected = false;
            int NumTasksSelected = dgTasks.SelectedItems.Count;

            if (dgTasks.ItemsSource != null)
            {
                if (NumTasksSelected > 0)
                {
                    Helper.PBITaskListoCopy = new List<PBITask>();

                    bool CopyingDummyTask = false;

                    foreach (var selectedTask in dgTasks.SelectedItems)
                    {
                        try
                        {
                            PBITask task = (PBITask)selectedTask;

                            if (task.WorkItemID != 0)
                            {
                                Helper.PBITaskListoCopy.Add(task);
                            }
                            else
                            {
                                CopyingDummyTask = true;
                            }
                        }
                        catch
                        {
                            // Suppressing Invalid cast exception when the user selects the empty row in the grid and presses 'Copy Tasks To' button.
                            emptyRowSelected = true;
                        }
                    }

                    if (emptyRowSelected && NumTasksSelected == 1)
                    {
                        DialogBox.ShowError(Helper.EMPTY_TASK_ERROR);
                    }
                    else if (CopyingDummyTask)
                    {
                        DialogBox.ShowError(Helper.COPYING_DUMMY_TASK);
                    }
                    else
                    {
                        Helper.PBIListForGrid = PBIListForGrid;

                        Helper.NewIteration = lblNewIteration.Content.ToString();

                        CopyTasksToSprintIteration newWindow = new CopyTasksToSprintIteration();
                        newWindow.ShowDialog();

                        if (newWindow.DialogResult == true)
                        {
                            lblTasksInfo.Content = "Tasks copied successfully..";
                            lblTasksInfo.Foreground = Brushes.Green;

                            dgTasks.SelectedIndex = -1;

                            await Task.Delay(clearMessageDelay);
                            lblTasksInfo.Content = "";
                        }
                    }
                }
                else
                {
                    DialogBox.ShowError(Helper.SELECT_A_TASK);
                }
            }
            else
            {
                DialogBox.ShowError(Helper.NO_TASKS_TO_COPY);
            }
        }

        private async void dgTasks_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            TaskLinkClicked = false;
            selectedTaskID = 0;

            if (dgTasks.ItemsSource != null)
            {
                if (dgTasks.Items.Count > 1)
                {
                    if (dgTasks.SelectedItems.Count == 1)
                    {
                        if (dgTasks.CurrentColumn != null)
                        {
                            try
                            {
                                PBITask task = (PBITask)dgTasks.SelectedItem;

                                if (dgTasks.CurrentColumn.Header.ToString().Equals("ID"))
                                {
                                    if (!TaskLinkClicked)
                                    {
                                        string urlString = string.Empty;
                                        bool ieWindowOpen = false;
                                        int id = task.WorkItemID;

                                        if (id != 0)
                                        {
                                            TaskLinkClicked = true;
                                            await Task.Run(() => OpenInTFSWeb(ref urlString, ref ieWindowOpen, id));
                                        }
                                    }
                                }
                                else
                                {
                                    TaskLinkClicked = false;
                                    selectedTaskID = task.WorkItemID;
                                }
                            }
                            catch
                            {
                                // Suppressing Invalid cast exception when the user selects the empty row
                            }
                        }
                    }
                }
            }
        }

        private async void dgTasks_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dgTasks.CurrentColumn != null)
            {
                if (dgTasks.CurrentColumn.Header.ToString().Equals("ID"))
                {
                    if (!TaskLinkClicked)
                    {
                        string urlString = string.Empty;
                        bool ieWindowOpen = false;

                        if (selectedTaskID != 0)
                        {
                            TaskLinkClicked = true;
                            await Task.Run(() => OpenInTFSWeb(ref urlString, ref ieWindowOpen, selectedTaskID));
                        }
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CheckSaveTasks();
        }

        #endregion

        #region [Private methods]

        private bool CheckSaveTasks()
        {
            bool continueFurther = true;

            try
            {
                if (taskList != null)
                {
                    bool confirmSaving = false;

                    if (TaskEdited)
                    {
                        confirmSaving = true;
                    }

                    if (deletedTasks.Count >= 1)
                    {
                        confirmSaving = true;
                    }
                    else if (dgTasks.Items.Count - 1 > initialTaskCount)
                    {
                        confirmSaving = true;
                    }

                    if (confirmSaving)
                    {
                        if (DialogBox.Show(Helper.UNSAVED_CHANGES))
                        {
                            deletedTasks = new List<int>();

                            TFSBacklogItem pbi = (TFSBacklogItem)dgPBIs.SelectedItem;

                            taskList.RemoveAll(x => x.WorkItemTitle == null);
                            taskList.RemoveAll(x => x.WorkItemTitle.Trim().Equals(string.Empty));

                            ProgressDialog.Execute(this, Helper.TASKS_SAVING, (bw) => { Helper.TfsWrapper.SaveTasks(taskList, prevWorkItemID); });

                            continueFurther = true;
                        }
                        else
                        {
                            continueFurther = false;
                        }

                        TaskEdited = false;
                    }
                }
            }
            catch (Exception exc)
            {
                Log.CaptureException(exc);
                Log.LogFileWrite();
            }

            return continueFurther;
        }

        private async void LoadWorkItems()
        {
            try
            {
                if (selectedIterationID > 0)
                {
                    // Get the all PBIs based on the selected Iteration ID             
                    ProgressDialog.Execute(this, Helper.PBI_RETRIEVING, (bw) => { PBIList = Helper.TfsWrapper.GeWorkItemsForTreeView(selectedIterationID); });

                    GeneratePBITree(PBIList);

                    if (PBIList.Count > 0)
                    {
                        btnMovePBIs.IsEnabled = true;

                        lblSelectedIteration.Content = Helper.TfsWrapper.GetFullIterationPath(cobIterations.SelectedItem.ToString());
                    }
                    else
                    {
                        lblMessage.Content = Helper.NO_PBI;
                        lblMessage.Foreground = Brushes.IndianRed;

                        btnMovePBIs.IsEnabled = false;

                        await Task.Delay(clearMessageDelay);
                        lblMessage.Content = "";
                    }

                    btnGetPBI.IsEnabled = true;
                }
            }
            catch (Exception exc)
            {
                Log.CaptureException(exc);
                Log.LogFileWrite();
            }
        }

        private void GeneratePBITree(List<TFSBacklogItem> PBIList)
        {
            tvPBIList.Items.Clear();

            foreach (TFSBacklogItem pbi in PBIList)
            {
                TreeViewItem newItem = CreateNewTreeViewItem(pbi);

                tvPBIList.Items.Add(newItem);

                newItem.IsExpanded = true;

                if (pbi.ChildPBI.Count > 0)
                {
                    foreach (TFSBacklogItem pbiChild in pbi.ChildPBI)
                    {
                        TreeViewItem newChildItem = CreateNewTreeViewItem(pbiChild);

                        newItem.Items.Add(newChildItem);
                    }
                }
            }
        }

        private TreeViewItem CreateNewTreeViewItem(TFSBacklogItem pbi)
        {
            TreeViewItem newItem;
            Label PBILabel;

            if (pbi.TypeName.Equals("Product Backlog Item"))
            {
                PBILabel = new Label()
                {
                    Background = Brushes.Green,
                    Height = 13,
                    Width = 5,
                    ToolTip = TFSConstants.WorkItemTypes.PBI
                };
            }
            else
            {
                // if ItemType is a Bug
                PBILabel = new Label()
                {
                    Background = Brushes.BlueViolet,
                    Height = 13,
                    Width = 5,
                    ToolTip = TFSConstants.WorkItemTypes.Bug
                };
            }

            newItem = new TreeViewItem()
            {
                Header = new CheckBox()
                {
                    Content = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children = 
                            {
                                PBILabel,
                                new Label()
                                {
                                    Content = pbi.WorkItemID + " : " + pbi.WorkItemTitle
                                }
                            }
                    }
                }
            };

            return newItem;
        }

        private void GetSelectedPBIFromTreeView()
        {
            Helper.SelectedPBIs = null;
            selectedPBIs = new List<int>();

            foreach (TreeViewItem t in tvPBIList.Items)
            {
                CheckBox cb = t.Header as CheckBox;
                if (cb.IsChecked == true)
                {
                    AddTreeDataToSelectedPBIs(cb);
                }

                if (t.HasItems)
                {
                    foreach (TreeViewItem item in t.Items)
                    {
                        CheckBox chbox = item.Header as CheckBox;
                        if (chbox.IsChecked == true)
                        {
                            AddTreeDataToSelectedPBIs(chbox);
                        }
                    }
                }
            }
        }

        private void AddTreeDataToSelectedPBIs(CheckBox cb)
        {
            StackPanel treeNodeStackPanel = (StackPanel)cb.Content;
            UIElementCollection collection = treeNodeStackPanel.Children;
            Label label = (Label)collection[1];
            string treenodeContent = label.Content.ToString();

            selectedPBIs.Add(GetWorkItemIDFromPBINode(treenodeContent));
        }

        private int GetWorkItemIDFromPBINode(string p)
        {
            try
            {
                string[] split = p.Split(':');

                if (split.Length > 0)
                {
                    return int.Parse(split[0].Trim());
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private void PopulateGridWithLatestIteration()
        {
            List<TFSBacklogItem> NewPBIList = Helper.TfsWrapper.GeWorkItemsForGrid(Helper.NewIterationID);

            PBIListForGrid = new List<TFSBacklogItem>();

            foreach (TFSBacklogItem pbi in NewPBIList)
            {
                PBIListForGrid.Add(pbi);
            }
        }

        private async Task RetrieveTasks()
        {
            TFSBacklogItem pbi = (TFSBacklogItem)dgPBIs.SelectedItem;

            taskList = null;

            ProgressDialog.Execute(this, Helper.TASKS_RETRIEVING, (bw) => { taskList = Helper.TfsWrapper.GetTasksFromPBI(pbi.WorkItemID); });

            TasksRetrieved = true;

            lblSelectedWorkItemID.Content = pbi.WorkItemID.ToString();

            TaskEdited = false;

            if (taskList.Count > 0)
            {
                dgTasks.ItemsSource = taskList;
                initialTaskCount = taskList.Count;
            }
            else
            {
                lblTasksInfo.Content = Helper.NO_TASKS_IN_PBI;
                lblTasksInfo.Foreground = Brushes.IndianRed;

                // Assigning an empty list of items to display an empty row in the Grid for adding new Task.
                dgTasks.ItemsSource = taskList;

                await Task.Delay(clearMessageDelay);
                lblTasksInfo.Content = "";
            }
        }

        private string CreateStringFromListOfInt(List<int> ListOfIntergers)
        {
            string taskIds = "";

            foreach (int taskID in ListOfIntergers)
            {
                taskIds += taskID + "   ";
            }

            return taskIds;
        }

        private void OpenInTFSWeb(ref string urlString, ref bool ieWindowOpen, int id)
        {
            ieWindowOpen = IsTFSWebAccessWindowOpen();
            urlString = Helper.TfsWrapper.GetWorkItemUrl(id);
            Process.Start(urlString);

            // Check if IE has already opened with TFS web access. This can be possible when the credentials were cached in a previous login.
            ieWindowOpen = IsTFSWebAccessWindowOpen();

            // IE is not open, Windows Security dialog will open prompting for username & password, we need to handle it.            
            if (ieWindowOpen == false)
            {
                WinAPI.HandleWindowsSecurityDialog(Helper.TFSConnectionSettings.Domain, Helper.TFSConnectionSettings.UserName, CryptoLibrary.ToSecureString(Helper.TFSConnectionSettings.Password));
            }
        }

        private bool IsTFSWebAccessWindowOpen()
        {
            bool ret = false;

            Process[] ieProcess = Process.GetProcessesByName(Helper.ieProcessName);

            if (ieProcess.Length > 0)
            {
                foreach (Process process in ieProcess)
                {
                    if (process.MainWindowTitle.Contains("Bug #") ||
                        process.MainWindowTitle.Contains("Task #") ||
                        process.MainWindowTitle.Contains("Test Case #") ||
                        process.MainWindowTitle.Contains("Product Backlog Item #"))
                    {
                        // Probably an IE window has already been launched from within MyBugger
                        ret = true;
                        break;
                    }
                }
            }

            return ret;
        }

        #endregion
    }
}
