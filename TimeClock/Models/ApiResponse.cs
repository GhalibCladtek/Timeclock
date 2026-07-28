using System;
using System.Collections.Generic;

namespace TimeClock.Models
{
    public class ApiResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int count { get; set; }
        public List<Submission> data { get; set; }
    }

    public class Submission
    {
        public int id { get; set; }
        public string project_title { get; set; }
        public string requesting_department { get; set; }
        public string project_initiator { get; set; }
        public string strategy_objective { get; set; }
        public string site_location { get; set; }
        public string problem_statement { get; set; }
        public string status { get; set; }
        public string status_summary { get; set; }
        public string priority { get; set; }
        public string cumulative_percentage { get; set; }
        public string budget_planned { get; set; }
        public string budget_actual { get; set; }
        public string budget_remaining { get; set; }
        public string budget_status { get; set; }
        public string otd { get; set; }
        public DateTime? planned_start_date { get; set; }
        public DateTime? planned_completion_date { get; set; }
        public DateTime? actual_start_date { get; set; }
        public DateTime? actual_completion_date { get; set; }
    }
}