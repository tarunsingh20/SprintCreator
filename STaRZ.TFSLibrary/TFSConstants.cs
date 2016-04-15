using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STaRZ.TFSLibrary
{
    public class TFSConstants
    {
        public static class WorkItemTypes
        {
            public const string Task = "Task";
            public const string Bug = "Bug";
            public const string PBI = "Product Backlog Item";
        }

        public static class MessageTypes
        {
            public const string Error = "Error";
            public const string Information = "Information";
            public const string Warning = "Warning";
        }

        public static class Fields
        {
            public const string State = "State";
            public const string Title = "Title";
            public const string RemainingWork = "Remaining Work";
            public const string BacklogPriority = "Backlog Priority";
        }

        public static class States
        {
            public const string Removed = "Removed";
        }

        public static class LinkTypeEnds
        {
            public const string Child = "Child";
        }

    }
}
