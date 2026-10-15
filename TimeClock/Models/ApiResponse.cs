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
        public string unique_id { get; set; }
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

    public class DashboardITRequest
    {
        public int? year { get; set; }
        public int? month { get; set; }
        public int? week { get; set; }
        public string dept { get; set; }
        public string sitePlant { get; set; }
        public string projectID { get; set; }
    }

    public class SummaryActivity
    {
        public int WeekNumber { get; set; }
        public int duration { get; set; }
        public string fullName { get; set; }
        public string taskId { get; set; }
        public string taskTitle { get; set; }
        public string sitePlant { get; set; }
    }

    public class SummaryActivityResult
    {
        public int WeekNumber { get; set; }
        public string EmployeeName { get; set; }
        public int TotalProject { get; set; }
        public int PlannedHours { get; set; }
        public int ActualHours { get; set; }
        public double Percentage { get; set; }
        public List<SimpleActivity> detail { get; set; }
    }

    public class SimpleActivity
    {
        public string ProjectID { get; set; }
        public string ProjectName { get; set; }
        public string SiteLocation { get; set; }
        public int Duration { get; set; }
    }

    public class GenericModel
    {
        public string str1 { get; set; }
        public string str2 { get; set; }
        public string str3 { get; set; }
        public string str4 { get; set; }
        public string str5 { get; set; }
        public int int1 { get; set; }
        public int int2 { get; set; }
        public int int3 { get; set; }
        public int int4 { get; set; }
        public int int5 { get; set; }
    }
}