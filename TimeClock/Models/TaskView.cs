using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeClock.Models
{
    public class TaskView
    {
        public TblTaskBySuperior mainTask { get; set; }
        public List<TblTaskByIndividual> submainTask { get; set; }
    }

    public class Workers
    {
        public string BadgeId { get; set; }
        public string FullName { get; set; }
        public List<TblTaskByIndividual> taskList { get; set; }
    }

    public class ProjectTaskDetail {
        public string title { get; set; }
        public string description { get; set; }
        public string assignBy { get; set; }
        public DateTime? dueDate { get; set; }
        public int Id { get; set; }
        public Nullable<int> IdTBS { get; set; }
        public int ownerId { get; set; }
        public string ownerBadgeId { get; set; }
        public string ownerFullName { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? finishDate { get; set; }
        public string currentStatus { get; set; }
        public string remarks { get; set; }
        public string remarksWhenCanceled { get; set; }
        public string remarksWhenPaused { get; set; }
        public string attachment { get; set; }
        public bool canceled { get; set; }
    }

    public static class StatusTask {
        public const string Open = "open";
        public const string OnGoing = "on going";
        public const string Paused = "paused";
        public const string Finish = "finished";
    }
}