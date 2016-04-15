using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STaRZ.TFSLibrary
{
    public class TFSBacklogItem
    {
        public int WorkItemID { get; set; }
        public string WorkItemTitle { get; set; }
        public string TypeName { get; set; }
        public string State { get; set; }
        public string BacklogPriority { get; set; }
        public string IterationPath { get; set; }
        public string AreaPath { get; set; }

        public List<TFSBacklogItem> ChildPBI = new List<TFSBacklogItem>();
        public List<PBITask> ChildTasks = new List<PBITask>();
    }

    public class PBITask
    {
        public int WorkItemID { get; set; }
        public string WorkItemTitle { get; set; }
        public string TypeName { get; set; }
        public string State { get; set; }
        public string IterationPath { get; set; }
        public string AreaPath { get; set; }

        [System.ComponentModel.DefaultValue(0)]
        public double RemainingWork { get; set; }
    }
}
