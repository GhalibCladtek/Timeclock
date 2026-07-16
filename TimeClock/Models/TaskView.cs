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
}