using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace STaRZ.TFSLibrary
{
    public class TFSWrapper
    {
        #region [Local Variables]

        TfsTeamProjectCollection tfsTeamProjectCollection = null;
        WorkItemStore workItemStore = null;
        Project project = null;

        List<WorkItem> workItemList = null;
        WorkItemType taskWIType = null;
        WorkItemLinkTypeEnd linkTypeEnd = null;

        IEnumerable<WorkItemLinkInfo> links, taskLinks;

        const string PLEASE_SELECT = "Please select an Iteration from the list..";

        string tfsProject = string.Empty;
        string currentProjectName = string.Empty;

        #endregion

        #region [Properties]

        public bool TfsProjectExist
        {
            get;
            set;
        }

        public bool WorkItemStoreFound
        {
            get;
            set;
        }

        public bool TfsServerAuthenticated
        {
            get;
            set;
        }

        #endregion

        #region [Constructor]

        public TFSWrapper(Uri tfsUri, NetworkCredential credential, string projectName)
        {
            try
            {
                tfsTeamProjectCollection = new TfsTeamProjectCollection(tfsUri, credential);
                tfsTeamProjectCollection.EnsureAuthenticated();
                TfsServerAuthenticated = tfsTeamProjectCollection.HasAuthenticated;
            }
            catch (Exception)
            {
                TfsServerAuthenticated = false;
            }

            currentProjectName = projectName;

            if (TfsServerAuthenticated == true)
            {
                workItemStore = (WorkItemStore)tfsTeamProjectCollection.GetService(typeof(WorkItemStore));

                WorkItemStoreFound = (workItemStore != null);

                if (WorkItemStoreFound)
                {
                    if (workItemStore.Projects.Contains(projectName))
                    {
                        TfsProjectExist = true;

                        project = workItemStore.Projects[projectName];

                        taskWIType = project.WorkItemTypes[TFSConstants.WorkItemTypes.Task];

                        linkTypeEnd = workItemStore.WorkItemLinkTypes.LinkTypeEnds[TFSConstants.LinkTypeEnds.Child];
                    }
                }
            }
        }

        #endregion

        #region [Public Methods]

        public string GetWorkItemUrl(int workItemID)
        {
            string portal = string.Empty;
            try
            {
                TswaClientHyperlinkService service = tfsTeamProjectCollection.GetService<TswaClientHyperlinkService>();
                Uri uri = service.GetWorkItemEditorUrl(workItemID);
                portal = uri.AbsoluteUri;
            }
            catch
            {
                throw;
            }
            return portal;
        }

        public static bool ConnectToTFS(out string serverName, out string teamProjectName)
        {
            TfsTeamProjectCollection projectCollection;
            ITestManagementTeamProject teamProject = null;

            serverName = string.Empty;
            teamProjectName = string.Empty;

            try
            {
                TeamProjectPicker tpp = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false);
                DialogResult dlgRes = tpp.ShowDialog();

                if (dlgRes == System.Windows.Forms.DialogResult.OK)
                {
                    if (tpp.SelectedTeamProjectCollection != null)
                    {
                        projectCollection = tpp.SelectedTeamProjectCollection;

                        //string projectName = tpp.SelectedProjects[0].Name;               // Uncomment for VS 2012/2013 & comment for VS 2010
                        string projectName = tpp.SelectedProjects.GetValue(0).ToString();  // Uncomment for VS 2010 & comment for VS 2012/2013

                        teamProject = projectCollection.GetService<ITestManagementService>().GetTeamProject(projectName);
                        serverName = projectCollection.Uri.ToString();

                        //teamProjectName = teamProject.TeamProjectName;  // Uncomment for VS 2012/2013 & comment for VS 2010
                        teamProjectName = teamProject.WitProject.Name;    // Uncomment for VS 2010 & comment for VS 2012/2013
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Dictionary<int, string> GetIterations()
        {
            var iterationDict = new Dictionary<int, string>();

            if (project != null)
            {
                string projectName = currentProjectName + "\\";

                iterationDict.Add(0, PLEASE_SELECT);
                foreach (Node node in project.IterationRootNodes)
                {
                    iterationDict.Add(node.Id, node.Path.Replace(projectName, ""));

                    if (node.HasChildNodes)
                    {
                        AddChildNodes(iterationDict, node, projectName);
                    }
                }
            }
            return iterationDict;
        }

        public List<TFSBacklogItem> GeWorkItemsForGrid(int iterationId)
        {
            var wiCollection = GetWorkItemCollectionForSprintIteration(iterationId);

            // Casting workitem collection to list of workitems and then converting each work item to TFSBacklogItem - forming a list of TFSBacklogItem.
            List<TFSBacklogItem> TFSBacklogItemList = wiCollection.Cast<WorkItem>()
                                                     .ToList()
                                                     .ConvertAll(x => new TFSBacklogItem
                                                     {
                                                         WorkItemID = x.Id,
                                                         WorkItemTitle = x.Title,
                                                         TypeName = x.Type.Name,
                                                         State = x.State,
                                                         BacklogPriority = x.Fields[TFSConstants.Fields.BacklogPriority] != null && x.Fields[TFSConstants.Fields.BacklogPriority].Value != null ? x.Fields[TFSConstants.Fields.BacklogPriority].Value.ToString() : "",
                                                         IterationPath = x.IterationPath,
                                                         AreaPath = x.AreaPath,
                                                     });

            return TFSBacklogItemList;
        }

        public List<TFSBacklogItem> GeWorkItemsForTreeView(int iterationId)
        {
            WorkItemCollection wiCollection = GetWorkItemCollectionFromIteration(iterationId);

            // Casting WorkItemCollection to list of WorkItems
            List<WorkItem> wiList = wiCollection.Cast<WorkItem>().ToList();

            List<TFSBacklogItem> TFSBacklogItemList = new List<TFSBacklogItem>();

            // Getting list of parent backlog items for creating the parent nodes in the tree view
            List<WorkItemLinkInfo> parents = links.ToList<WorkItemLinkInfo>().FindAll(x => x.SourceId == 0);

            foreach (WorkItemLinkInfo link in parents)
            {
                // For each parent link, find out the workitem from the list of workitems.
                WorkItem workItem = wiList.Find(e => e.Id == link.TargetId);

                // Create a TFSBacklogItem out of the current work item.
                TFSBacklogItem pbi = CreatePBIFromWorkItem(workItem);

                // Find out all the child links for the current parent work item.
                List<WorkItemLinkInfo> childs = links.ToList<WorkItemLinkInfo>().FindAll(x => x.SourceId == link.TargetId);

                foreach (WorkItemLinkInfo chlink in childs)
                {
                    // For each child link, find out the corresponding work item from the work items list.
                    workItem = wiList.Find(e => e.Id == chlink.TargetId);

                    // Add all the child work items to the parent pbi, after converting them to PBI
                    pbi.ChildPBI.Add(CreatePBIFromWorkItem(workItem));
                }

                // Add the final parent backlog item to the TFSBacklogItemList
                TFSBacklogItemList.Add(pbi);
            }

            return TFSBacklogItemList;
        }

        public List<PBITask> GetTasksFromPBI(int WorkItemID)
        {
            List<PBITask> TaskList = new List<PBITask>();

            var wiCollection = GetTaskWICollectionForWorkItem(WorkItemID);

            // Casting workitem collection to list of WorkItem objects and then converting them to List of PBITask objects and adding them to TaskList.
            TaskList.AddRange(wiCollection.Cast<WorkItem>()
                                          .ToList()
                                          .ConvertAll(x => new PBITask
                                          {
                                              WorkItemID = x.Id,
                                              WorkItemTitle = x.Title,
                                              TypeName = TFSConstants.WorkItemTypes.Task,
                                              IterationPath = x.IterationPath,
                                              AreaPath = x.AreaPath,
                                              State = x.State,
                                              RemainingWork = x.Fields[TFSConstants.Fields.RemainingWork].Value == null ? 0 : double.Parse(x.Fields[TFSConstants.Fields.RemainingWork].Value.ToString())
                                          }));

            return TaskList;
        }

        public string GetFullIterationPath(string selectedIteration)
        {
            if (!selectedIteration.Equals(string.Empty))
            {
                string[] split = selectedIteration.Replace(']', ' ').Replace('[', ' ').Trim().Split(',');

                if (split.Length == 2)
                {
                    return currentProjectName + "\\" + split[1].Trim();
                }
            }

            return "";
        }

        public bool MoveSelectedPBIsToNewIteration(List<int> selectedPBIs, string newIterationPath)
        {
            try
            {
                string[] split = newIterationPath.Replace(']', ' ').Replace('[', ' ').Trim().Split(',');

                if (split.Length == 2)
                {
                    string newIterationCompletePath = currentProjectName + "\\" + split[1].Trim();
                    int newIterationID = int.Parse(split[0]);

                    List<WorkItem> wiList = new List<WorkItem>();
                    int movedItemsCount = 0;

                    foreach (int workItemID in selectedPBIs)
                    {
                        WorkItemCollection wiCollection = GetWorkItemAndChildTasks(workItemID);

                        wiList.AddRange(wiCollection.Cast<WorkItem>().ToList());
                    }

                    foreach (WorkItem workItem in wiList)
                    {
                        workItem.PartialOpen();
                        workItem.IterationPath = newIterationCompletePath;

                        ArrayList list = workItem.Validate();

                        if (list.Count == 0)
                        {
                            movedItemsCount++;
                        }
                    }

                    if (movedItemsCount > 0)
                    {
                        workItemStore.BatchSave(wiList.ToArray());
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public void SaveTasks(List<PBITask> taskList, int workItemID)
        {
            WorkItemCollection initialTaskCollection = GetTaskWICollectionForWorkItem(workItemID);

            List<WorkItem> initialTaskCollectionList = initialTaskCollection.Cast<WorkItem>().ToList();

            int initialTaskCount = initialTaskCollection.Count;
            int finalTaskCount = taskList.Count;
            int newTasksCount = 0;

            List<int> finalTasks = new List<int>();

            workItemList = new List<WorkItem>();

            WorkItem pbiWorkItem = null;

            if (taskList.FindAll(x => x.WorkItemID == 0).Count > 0)
            {
                pbiWorkItem = workItemStore.GetWorkItem(workItemID);
            }

            foreach (PBITask taskFromGrid in taskList)
            {
                if (taskFromGrid.WorkItemTitle != null)
                {
                    // Create New Task if the WorkItemID = 0
                    if (taskFromGrid.WorkItemID == 0)
                    {
                        if (pbiWorkItem != null)
                        {
                            newTasksCount++;
                            CreateNewTaskUnderWorkItem(taskFromGrid, pbiWorkItem);
                        }
                    }
                    else
                    {
                        // Final tasks are the old tasks which are still existing in the current PBI.
                        finalTasks.Add(taskFromGrid.WorkItemID);

                        // Get the current task work item; check and save work item if there are any changes in the title & remaining hours
                        WorkItem workItemFromTFS = initialTaskCollectionList.Find(x => x.Id == taskFromGrid.WorkItemID);

                        bool save = false;

                        if (workItemFromTFS.Fields[TFSConstants.Fields.RemainingWork].Value != null)
                        {
                            save = !workItemFromTFS.Title.Equals(taskFromGrid.WorkItemTitle) || (taskFromGrid.RemainingWork != (double)workItemFromTFS.Fields[TFSConstants.Fields.RemainingWork].Value) ? true : false;
                        }
                        else
                        {
                            save = !workItemFromTFS.Title.Equals(taskFromGrid.WorkItemTitle) ? true : false;
                        }

                        if (save)
                        {
                            workItemFromTFS.PartialOpen();

                            workItemFromTFS.Title = taskFromGrid.WorkItemTitle;
                            workItemFromTFS.Fields[TFSConstants.Fields.RemainingWork].Value = taskFromGrid.RemainingWork;

                            workItemList.Add(workItemFromTFS);
                        }
                    }
                }
            }

            if ((newTasksCount > 0) && (pbiWorkItem != null))
            {
                workItemList.Add(pbiWorkItem);
            }

            // If there are any deleted tasks, change their state to removed
            if (finalTaskCount - newTasksCount != initialTaskCount)
            {
                foreach (WorkItem item in initialTaskCollection)
                {
                    if (!finalTasks.Contains(item.Id))
                    {
                        item.PartialOpen(); ;

                        item.State = TFSConstants.States.Removed;

                        workItemList.Add(item);
                    }
                }
            }

            //Saving Add, Edit & Remove changes as a Batch save.
            workItemStore.BatchSave(workItemList.ToArray());
        }

        public void CopyTasksToSelectedWorkItems(List<TFSBacklogItem> PBIsList, List<PBITask> TasksList)
        {
            List<WorkItem> wiList = new List<WorkItem>();

            List<int> workitemIDList = new List<int>();

            workitemIDList.AddRange(PBIsList.Select<TFSBacklogItem, int>(x => x.WorkItemID));

            string linkQueryText = @"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [Microsoft.VSTS.Common.BacklogPriority] FROM WorkItems";

            var wiCollection = workItemStore.Query(workitemIDList.ToArray(), linkQueryText);

            foreach (WorkItem pbiWorkItem in wiCollection)
            {
                List<WorkItem> workItemList = new List<WorkItem>();

                workItemList.AddRange(TasksList.ConvertAll<WorkItem>(x =>
                {
                    WorkItem newTask = new WorkItem(taskWIType);

                    newTask.Title = x.WorkItemTitle;
                    newTask.IterationPath = x.IterationPath;
                    newTask.AreaPath = x.AreaPath;
                    newTask.Fields[TFSConstants.Fields.RemainingWork].Value = x.RemainingWork;

                    return newTask;
                }));

                workItemStore.BatchSave(workItemList.ToArray());

                List<int> newIDList = workItemList.Select<WorkItem, int>(x => x.Id).ToList();

                foreach (int id in newIDList)
                {
                    pbiWorkItem.Links.Add(new RelatedLink(linkTypeEnd, id));
                }

                wiList.Add(pbiWorkItem);
            }

            workItemStore.BatchSave(wiList.ToArray());
        }

        public void GenerateGenericTasksForPBI(List<TFSBacklogItem> PBIListToCreateTasks, ObservableCollection<string> genericTaskTitles)
        {
            List<WorkItem> wiList = new List<WorkItem>();

            List<int> workitemIDList = new List<int>();

            workitemIDList.AddRange(PBIListToCreateTasks.Select<TFSBacklogItem, int>(x => x.WorkItemID));

            string linkQueryText = @"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [Microsoft.VSTS.Common.BacklogPriority] FROM WorkItems";

            var wiCollection = workItemStore.Query(workitemIDList.ToArray(), linkQueryText);

            foreach (WorkItem pbiWorkItem in wiCollection)
            {
                if (pbiWorkItem != null)
                {
                    WorkItemLinkTypeEnd linkTypeEnd = workItemStore.WorkItemLinkTypes.LinkTypeEnds[TFSConstants.LinkTypeEnds.Child];

                    foreach (string taskTitle in genericTaskTitles)
                    {
                        RelatedLink relatedLink = new RelatedLink(linkTypeEnd, CreateNewGenericTask(taskTitle, pbiWorkItem.IterationPath, pbiWorkItem.AreaPath));

                        pbiWorkItem.Links.Add(relatedLink);
                    }

                    wiList.Add(pbiWorkItem);
                }
            }

            workItemStore.BatchSave(wiList.ToArray());
        }

        #endregion

        #region [Private Methods]

        private void AddChildNodes(Dictionary<int, string> dict, Node node, string projectName)
        {
            foreach (Node childNode in node.ChildNodes)
            {
                if (!dict.ContainsKey(childNode.Id))
                {
                    dict.Add(childNode.Id, childNode.Path.Replace(projectName, "    "));
                    AddChildNodes(dict, childNode, projectName);
                }
            }
        }

        private TFSBacklogItem CreatePBIFromWorkItem(WorkItem item)
        {
            TFSBacklogItem pbiItem = new TFSBacklogItem();

            pbiItem.WorkItemID = item.Id;
            pbiItem.WorkItemTitle = item.Title;
            pbiItem.TypeName = item.Type.Name;
            pbiItem.IterationPath = item.IterationPath;
            pbiItem.AreaPath = item.AreaPath;
            pbiItem.State = item.State;

            var backlogPriorityField = item.Fields[TFSConstants.Fields.BacklogPriority];
            pbiItem.BacklogPriority = backlogPriorityField != null && backlogPriorityField.Value != null ? backlogPriorityField.Value.ToString() : "";

            return pbiItem;
        }

        private WorkItemCollection GetWorkItemCollectionFromIteration(int iterationId)
        {
            // Query to retrieve parent PBIs and Bugs along with their child PBIs and Bugs - from the selected Iteration
            string linkQueryText = string.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [Microsoft.VSTS.Common.BacklogPriority] " +
                                                    "FROM WorkItemLinks WHERE ([Source].[System.TeamProject] = '{0}'  AND  [Source].[System.IterationId] = {1} " +
                                                    " AND ( [Source].[System.WorkItemType] = 'Bug'  OR  [Source].[System.WorkItemType] = 'Product Backlog Item' ) AND [Source].[System.State] <> 'Removed' AND [Source].[System.State] <> 'Done') " +
                                                    " And ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward') And " +
                                                    " (( [Target].[System.WorkItemType] = 'Bug'  OR  [Target].[System.WorkItemType] = 'Product Backlog Item' ) AND  [Target].[System.State] <> 'Removed' AND [Target].[System.State] <> 'Done' " +
                                                    " AND  [Target].[System.IterationId] = {1}) " +
                                                    "ORDER BY [System.WorkItemType] desc, [Microsoft.VSTS.Common.BacklogPriority] asc  mode(Recursive)", currentProjectName, iterationId);
            

            var query = new Query(workItemStore, linkQueryText);
            links = query.RunLinkQuery().ToList();

            List<int> workitemIDList = new List<int>();

            // From each WorkItemLinkInfo object, select the TargetID and create workitemIDList
            workitemIDList.AddRange(links.Select<WorkItemLinkInfo, int>((x, y) => x.TargetId));

            linkQueryText = @"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [Microsoft.VSTS.Common.BacklogPriority] FROM WorkItems";

            var wiCollection = workItemStore.Query(workitemIDList.ToArray(), linkQueryText);
            return wiCollection;
        }

        private WorkItemCollection GetWorkItemCollectionForSprintIteration(int iterationId)
        {
            // Query to retrieve parent PBIs and Bugs along with their child PBIs and Bugs - from the selected Iteration
            string linkQueryText = string.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [Microsoft.VSTS.Common.BacklogPriority] " +
                                                    "FROM WorkItemLinks WHERE ([Source].[System.TeamProject] = '{0}'  AND  [Source].[System.IterationId] = {1} " +
                                                    " AND ( [Source].[System.WorkItemType] = 'Bug'  OR  [Source].[System.WorkItemType] = 'Product Backlog Item' ) AND [Source].[System.State] <> 'Removed') " +
                                                    " And ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward') And " +
                                                    " (( [Target].[System.WorkItemType] = 'Bug'  OR  [Target].[System.WorkItemType] = 'Product Backlog Item' ) AND  [Target].[System.State] <> 'Removed' " +
                                                    " AND  [Target].[System.IterationId] = {1}) " +
                                                    "ORDER BY [System.WorkItemType] desc, [Microsoft.VSTS.Common.BacklogPriority] asc  mode(Recursive)", currentProjectName, iterationId);

            var query = new Query(workItemStore, linkQueryText);
            links = query.RunLinkQuery().ToList();

            List<int> workitemIDList = new List<int>();

            // From each WorkItemLinkInfo object, select the TargetID and create workitemIDList
            workitemIDList.AddRange(links.Select<WorkItemLinkInfo, int>((x, y) => x.TargetId));

            linkQueryText = @"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [Microsoft.VSTS.Common.BacklogPriority] FROM WorkItems";

            var wiCollection = workItemStore.Query(workitemIDList.ToArray(), linkQueryText);
            return wiCollection;
        }

        private WorkItemCollection GetTaskWICollectionForWorkItem(int workItemID)
        {
            // This quer returns a workitem as well as it's child tasks as a WorkItemLinkInfo collection
            string linkQueryText = string.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [Microsoft.VSTS.Scheduling.RemainingWork] " +
                                                    "FROM WorkItemLinks" + " WHERE ([Source].[System.TeamProject] = '{0}'  AND  [Source].[System.Id] = {1}) And " +
                                                    "([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward') And ([Target].[System.WorkItemType] = 'Task' " +
                                                    " AND  [Target].[System.State] <> 'Removed') ORDER BY [System.Id] mode(MayContain)", currentProjectName, workItemID);

            var query = new Query(workItemStore, linkQueryText);
            taskLinks = query.RunLinkQuery().ToList();

            List<int> taskLinkIDList = new List<int>();

            // Foreach WorkItemLinkInfo in taskLinks, select the TargetId where SourceId != 0  i.e, we are selecting all the child items & creating a taskLinkIDList.
            taskLinkIDList.AddRange(taskLinks.Select<WorkItemLinkInfo, int>(x =>
            {
                if (x.SourceId != 0)
                {
                    return x.TargetId;
                }
                else
                {
                    return 0;
                }
            }).Where(x => x != 0));

            linkQueryText = @"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [Microsoft.VSTS.Scheduling.RemainingWork] FROM WorkItems";

            var wiCollection = workItemStore.Query(taskLinkIDList.ToArray(), linkQueryText);
            return wiCollection;
        }

        private WorkItemCollection GetWorkItemAndChildTasks(int workItemID)
        {
            string linkQueryText = string.Format(@"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [Microsoft.VSTS.Scheduling.RemainingWork] " +
            "FROM WorkItemLinks" + " WHERE ([Source].[System.TeamProject] = '{0}'  AND  [Source].[System.Id] = {1}) And " +
            "([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward') And ([Target].[System.WorkItemType] = 'Task' " +
            " AND  [Target].[System.State] <> 'Removed') ORDER BY [System.Id] mode(MayContain)", currentProjectName, workItemID);

            var q = new Query(workItemStore, linkQueryText);
            taskLinks = q.RunLinkQuery().ToList();

            List<int> taskLinkIDList = new List<int>();

            // Adding all TargetIds from taskLinks to taskLinkIDList.
            taskLinkIDList.AddRange(taskLinks.Select<WorkItemLinkInfo, int>(x => x.TargetId));

            linkQueryText = @"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [Microsoft.VSTS.Scheduling.RemainingWork] FROM WorkItems";

            var wiCollection = workItemStore.Query(taskLinkIDList.ToArray(), linkQueryText);

            return wiCollection;
        }

        private void CreateNewTaskUnderWorkItem(PBITask task, WorkItem pbiWorkItem)
        {
            if (pbiWorkItem != null)
            {
                if (!task.WorkItemTitle.Trim().Equals(string.Empty))
                {
                    WorkItem workItem = new WorkItem(taskWIType);

                    workItem.Title = task.WorkItemTitle;
                    workItem.IterationPath = pbiWorkItem.IterationPath;
                    workItem.AreaPath = pbiWorkItem.AreaPath;
                    workItem.Fields[TFSConstants.Fields.RemainingWork].Value = task.RemainingWork;

                    workItem.Save();

                    // Link this new task to the PBI Work Item as a child
                    pbiWorkItem.Links.Add(new RelatedLink(linkTypeEnd, workItem.Id));
                }
            }
        }

        private int CreateNewGenericTask(string taskTitle, string iterationPath, string areaPath)
        {
            WorkItem workItem = new WorkItem(taskWIType);

            workItem.Title = taskTitle;
            workItem.IterationPath = iterationPath;
            workItem.AreaPath = areaPath;
            workItem.Fields[TFSConstants.Fields.RemainingWork].Value = 0;

            workItem.Save();

            return workItem.Id;
        }

        #endregion
    }
}
