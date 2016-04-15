using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using STaRZ.CryptoLibrary;
using STaRZ.TFSLibrary;

namespace SprintCreator.Common
{
    public class Helper
    {
        #region [Public Constants]

        public const string DOMAIN = "xxxx";
        public const string TFSSERVERNOTCONNECTEDERROR = "Unable to connect to TFS Server. Please check if the credentials, TFS server URL and project are correct.";
        public const string TFSPROJECTINVALIDERROR = "Could not find the TFS project specified on the server, please verify the TFS project name and try again.";
        public const string TFSWORKITEMSTOREINVALIDERROR = "Could not retrieve the work item store on the TFS server. Verify that you have installed the correct version of SprintCreator.";

        public const string KEY = "Key";
        public const string VALUE = "Value";

        public const string ieProcessName = "iexplore";

        public const string ITERATIONS_MOVING = "Please wait while the items are being moved to the selected iteration...";
        public const string SELECT_DIFFERENT_ITERATION = "Selected work items are in the same iteration. Please select a different one..!!";
        public const string PBI_SELETED = " work items were selected.";
        public const string PBI_RETRIEVING = "Please wait while the work items are being retrieved...";
        public const string NO_PBI = "There are no work Items in this iteration..!";
        public const string WORKITEMS_MOVED = "Selected work item(s) moved successfully.. Refreshing the latest iteration..";
        public const string SELECT_A_PBI = "Please select at least one work item from the tree view..!";
        public const string SELECT_A_GRID_ITEM = "Please select at least one item in the Grid..!";
        public const string SELECT_A_TASK = "Please select at least one task from the Tasks Grid..";
        public const string TASKS_COPIED = "Selected tasks have been successfully copied.. Refreshing sprint iteration Grid please wait..";
        public const string GENERIC_TASKS_ADDED = "Generic Tasks have been added successfully. Refreshing the Sprint iteration. Please wait..!";
        public const string GENERIC_TASKS_ADDING = "Please wait while the Generic tasks are being added to the selected work items...";
        public const string GENERIC_TASKS_CONFIRMATION = "Are you sure you want to add generic tasks to the following work items? ";
        public const string NO_TASKS_TO_SAVE = "No changes are saved..";
        public const string TASKS_SAVING = "Please wait while the tasks are being saved..";
        public const string MOVEBACK_CONFIRMATION = "Are you sure you want to move the following items back to the previous iteration?";
        public const string PBI_MOVING = "Please wait while the work items are being moved to the previous iteration..";
        public const string CANT_MOVE_PBI_ERROR = "Could not move the work items to previous iteration. Please try again..";
        public const string TASKS_RETRIEVING = "Please wait while the tasks are being retrieved..";
        public const string NO_TASKS_IN_PBI = "There are no tasks under the selected work item..!";
        public const string DELETE_TASKS_CONFIRMATION = "Are you sure you want to delete the following tasks?";
        public const string CONFIRM = "Confirmation ?";
        public const string NO_PREV_ITERATION = "There is no previous iteration selected. Please select an iteration from the iterations dropdown.";
        public const string EMPTY_TASK_ERROR = "You cannot copy an empty task. Please try again..";
        public const string NO_TASKS_TO_COPY = "There are no tasks to be copied.";
        public const string SELECT_ONLY_ONE = "Please select only 1 work item to open it in TFS web.";
        public const string NO_WORKITEMS = "There are no work items in the grid.";
        public const string COPYING_DUMMY_TASK = "Cannot copy the selected tasks. Please click Save to create the new tasks and then try to copy..!";
        public const string EMPTY_TASK = "There are some empty tasks in the grid. Please delete them and retry saving..!";
        public const string UNSAVED_CHANGES = "There are some unsaved changes in the Tasks grid. Do you want to save them before proceeding?";

        public static string NO_GENERIC_TASKS = "There are no Generic Tasks added in the application." +
                                               Environment.NewLine +
                                               "Please goto TFS Settings -> Generic Tasks tab to add them..!";

        public static string PBIGRID_INFO = "To load work items from an iteration click 'Load an iteration' button." +
                                            Environment.NewLine +
                                            "Select single/multiple work items in the grid and click on 'Add Generic Tasks' button to create Generic Tasks under them." +
                                            Environment.NewLine +
                                            "Select a work item in the grid to see the Child tasks." +
                                            Environment.NewLine +
                                            "To open a work item in TFS Web, click on the Workitem ID." +
                                            Environment.NewLine +
                                            "To add/update Generic Tasks goto 'TFS Settings' -> 'Generic Tasks' tab.";

        public static string TASKGRID_INFO = "To add a new Task, double click the empty row in the table." +
                                            Environment.NewLine +
                                            "To delete an existing Task, select a row and press 'Delete' in the keyboard." +
                                            Environment.NewLine +
                                            "To update an existing Task, double click on Title or Remaining hours." +
                                            Environment.NewLine +
                                            "Click the Task ID to open the task in TFS Web.";

        #endregion

        #region [Enumerations]

        public enum ErrorType
        {
            None,
            TFSWrapperError,
            TFSStoreError,
            TFSProjectError
        }

        #endregion

        #region [Options]

        static ErrorType settingsError;
        public static ErrorType SettingsError
        {
            get { return settingsError; }
            set { settingsError = value; }
        }

        static TFSConnectionSettings tfsConSettings;
        public static TFSConnectionSettings TFSConnectionSettings
        {
            get
            {
                if (tfsConSettings == null)
                {
                    GetTFSConnectionSettingsFromConfiguration();
                }
                return tfsConSettings;
            }
            set
            {
                tfsConSettings = value;
            }
        }

        public static void GetTFSConnectionSettingsFromConfiguration()
        {
            Configuration settingConfigurations = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            AppSettingsSection appSettingsSection = settingConfigurations.AppSettings;
            TFSConnectionSettings = new TFSConnectionSettings();

            if (appSettingsSection.Settings.AllKeys.Contains("UserName"))
            {
                TFSConnectionSettings.UserName = appSettingsSection.Settings["UserName"].Value;
            }

            if (appSettingsSection.Settings.AllKeys.Contains("Domain"))
            {
                TFSConnectionSettings.Domain = appSettingsSection.Settings["Domain"].Value;
            }
            else
            {
                TFSConnectionSettings.Domain = Helper.DOMAIN;
            }

            if (appSettingsSection.Settings.AllKeys.Contains("Password"))
            {
                TFSConnectionSettings.Password = CryptoLibrary.ToInsecureString(CryptoLibrary.DecryptString(appSettingsSection.Settings["Password"].Value));
            }

            if (appSettingsSection.Settings.AllKeys.Contains("TFSServer"))
            {
                TFSConnectionSettings.TfsServer = appSettingsSection.Settings["TFSServer"].Value;
            }


            if (appSettingsSection.Settings.AllKeys.Contains("TFSProject"))
            {
                TFSConnectionSettings.TfsProject = appSettingsSection.Settings["TFSProject"].Value;
            }

            if (appSettingsSection.Settings.AllKeys.Contains("DefectWorkItemType"))
            {
                TFSConnectionSettings.DefectWorkItemType = appSettingsSection.Settings["DefectWorkItemType"].Value;
            }

            if (appSettingsSection.Settings.AllKeys.Contains("SaveUserNameAndPassword"))
            {
                TFSConnectionSettings.SaveUserNameAndPassword = (appSettingsSection.Settings["SaveUserNameAndPassword"].Value.ToLower() == "true");
            }
            else
            {
                TFSConnectionSettings.SaveUserNameAndPassword = false;
            }

            if (appSettingsSection.Settings.AllKeys.Contains("AutoConnect"))
            {
                TFSConnectionSettings.ConnectAutomatically = (appSettingsSection.Settings["AutoConnect"].Value.ToLower() == "true");
            }
            else
            {
                TFSConnectionSettings.ConnectAutomatically = false;
            }

        }

        public static void SaveTFSConnectionSettingsToConfiguration()
        {
            //write the values to config file
            Configuration settingConfigurations = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            AppSettingsSection appSettingsSection = settingConfigurations.AppSettings;

            if (appSettingsSection.Settings.AllKeys.Contains("Domain"))
            {
                appSettingsSection.Settings["Domain"].Value = TFSConnectionSettings.Domain;
            }
            else
            {
                appSettingsSection.Settings.Add("Domain", TFSConnectionSettings.Domain);
            }

            if (appSettingsSection.Settings.AllKeys.Contains("UserName"))
            {
                appSettingsSection.Settings["UserName"].Value = TFSConnectionSettings.UserName;
            }
            else
            {
                appSettingsSection.Settings.Add("UserName", TFSConnectionSettings.UserName);
            }

            if (appSettingsSection.Settings.AllKeys.Contains("Password"))
            {
                if (TFSConnectionSettings.Password.Length > 0)
                {
                    appSettingsSection.Settings["Password"].Value = CryptoLibrary.EncryptString(CryptoLibrary.ToSecureString(TFSConnectionSettings.Password));
                }
                else
                {
                    appSettingsSection.Settings["Password"].Value = string.Empty;
                }
            }
            else
            {
                if (TFSConnectionSettings.Password.Length > 0)
                {
                    appSettingsSection.Settings.Add("Password", CryptoLibrary.EncryptString(CryptoLibrary.ToSecureString(TFSConnectionSettings.Password)));
                }
                else
                {
                    appSettingsSection.Settings.Add("Password", string.Empty);
                }
            }

            if (appSettingsSection.Settings.AllKeys.Contains("TFSServer"))
            {
                appSettingsSection.Settings["TFSServer"].Value = TFSConnectionSettings.TfsServer;
            }
            else
            {
                appSettingsSection.Settings.Add("TFSServer", TFSConnectionSettings.TfsServer);
            }

            if (appSettingsSection.Settings.AllKeys.Contains("TFSProject"))
            {
                appSettingsSection.Settings["TFSProject"].Value = TFSConnectionSettings.TfsProject;
            }
            else
            {
                appSettingsSection.Settings.Add("TFSProject", TFSConnectionSettings.TfsProject);
            }

            if (appSettingsSection.Settings.AllKeys.Contains("DefectWorkItemType"))
            {
                appSettingsSection.Settings["DefectWorkItemType"].Value = TFSConnectionSettings.DefectWorkItemType;
            }
            else
            {
                appSettingsSection.Settings.Add("DefectWorkItemType", TFSConnectionSettings.DefectWorkItemType);
            }


            if (appSettingsSection.Settings.AllKeys.Contains("SaveUserNameAndPassword"))
            {
                appSettingsSection.Settings["SaveUserNameAndPassword"].Value = TFSConnectionSettings.SaveUserNameAndPassword ? "true" : "false";
            }
            else
            {
                appSettingsSection.Settings.Add("SaveUserNameAndPassword", TFSConnectionSettings.SaveUserNameAndPassword ? "true" : "false");
            }

            if (appSettingsSection.Settings.AllKeys.Contains("AutoConnect"))
            {
                appSettingsSection.Settings["AutoConnect"].Value = TFSConnectionSettings.ConnectAutomatically ? "true" : "false";
            }
            else
            {
                appSettingsSection.Settings.Add("AutoConnect", TFSConnectionSettings.ConnectAutomatically ? "true" : "false");
            }
            settingConfigurations.Save();
        }

        public static ObservableCollection<string> GetGenericTasksFromXML()
        {
            ObservableCollection<string> GenericTaskCollection = new ObservableCollection<string>();

            XmlDocument AllTasks = new XmlDocument();
            string xmlDocPath = GetGenericTasksXMLPath();

            if (!File.Exists(xmlDocPath))
            {
                AllTasks.LoadXml("<?xml version=\"1.0\"?><GenericTasks></GenericTasks>");

                XmlTextWriter writer = new XmlTextWriter("GenericTasks.xml", null);
                writer.Formatting = Formatting.Indented;
                AllTasks.Save(writer);

                writer = null;
            }
            else
            {
                AllTasks.Load(xmlDocPath);

                XmlNodeList taskList = AllTasks.ChildNodes[1].ChildNodes;

                foreach (XmlNode task in taskList)
                {
                    string taskTitle = task.Attributes["Title"].Value.Trim();

                    if (!taskTitle.Equals(string.Empty))
                    {
                        GenericTaskCollection.Add(taskTitle);
                    }
                }
            }

            AllTasks = null;
            GC.Collect();

            return GenericTaskCollection;
        }

        public static string GetGenericTasksXMLPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "GenericTasks.xml";
        }

        #endregion

        #region [TFSWrapper]

        static TFSWrapper tfsWrapper;

        public static TFSWrapper TfsWrapper
        {
            get
            {
                if (tfsWrapper == null)
                {
                    InitializeTfsWrapper();
                }

                return tfsWrapper;
            }
            set
            {
                tfsWrapper = value;
            }
        }

        public static void InitializeTfsWrapper()
        {
            tfsWrapper = new TFSWrapper(new Uri(Helper.TFSConnectionSettings.TfsServer),
                                        new System.Net.NetworkCredential(Helper.TFSConnectionSettings.UserName, Helper.TFSConnectionSettings.Password, Helper.TFSConnectionSettings.Domain), Helper.TFSConnectionSettings.TfsProject);

        }

        #endregion

        #region [Public Properties]

        static Dictionary<int, string> iterations;
        public static Dictionary<int, string> Iterations
        {
            get
            {
                if (iterations == null)
                {
                    iterations = TfsWrapper.GetIterations();
                }

                return iterations;
            }
            set
            {
                iterations = value;
            }
        }

        static List<int> selectedPBIs;
        public static List<int> SelectedPBIs
        {
            get
            {
                return selectedPBIs;
            }
            set
            {
                selectedPBIs = value;
            }
        }

        static string newIteration;
        public static string NewIteration
        {
            get { return newIteration; }
            set { newIteration = value; }
        }

        static int newIterationID;
        public static int NewIterationID
        {
            get { return newIterationID; }
            set { newIterationID = value; }
        }

        public static List<TFSBacklogItem> PBIListForGrid { get; set; }

        public static List<PBITask> PBITaskListoCopy { get; set; }

        public static List<int> TasksCopiedTo { get; set; }

        public static int SelectedSourceIteration { get; set; }

        #endregion
    }
}
