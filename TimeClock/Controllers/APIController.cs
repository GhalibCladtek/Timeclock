using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeClock.Modul;
using TimeClock.Models;
using TimeClock.Helpers;

namespace TimeClock.Controllers
{
    public class APIController : Controller
    {
        public ApiService api = new ApiService();

        [AllowAnonymous]
        public ActionResult SyncProjects()
        {
            try
            {
                var submissionsData = api.GetSubmissions();

                if (submissionsData != null && submissionsData.Count > 0)
                {
                    using (var db = new TaskLogEntities())
                    {
                        using (var transaction = db.Database.BeginTransaction())
                        {
                            try
                            {
                                db.TblProjectSubmission.RemoveRange(db.TblProjectSubmission.ToList());
                                db.Database.ExecuteSqlCommand("update TblProjectSubmission set is_active=0");

                                var internalProj = db.TblInternalActivityOnly.ToList();

                                foreach (var i in internalProj)
                                {
                                    var newProj = new TblProjectSubmission
                                    {
                                        unique_id = i.code,
                                        project_title = i.title,
                                        requesting_department = i.reqDepartment,
                                        project_initiator = i.requestor,
                                        strategy_objective = i.objective,
                                        site_location = i.siteLoc,
                                        problem_statement = i.objective,
                                        status = i.status,
                                        status_summary = i.statusSummary,
                                        priority = i.priority,
                                        is_active = true
                                    };
                                    db.TblProjectSubmission.Add(newProj);
                                }

                                foreach (var i in submissionsData)
                                {
                                    if (i.status_summary == "Identification- Requested") 
                                        continue;
                                    // Insert New
                                    var newProj = new TblProjectSubmission
                                    {
                                        unique_id = i.unique_id,
                                        project_title = i.project_title,
                                        requesting_department = i.requesting_department,
                                        project_initiator = i.project_initiator,
                                        strategy_objective = i.strategy_objective,
                                        site_location = i.site_location,
                                        problem_statement = i.problem_statement,
                                        status = i.status,
                                        status_summary = i.status_summary,
                                        priority = i.priority,
                                        cumulative_percentage = i.cumulative_percentage,
                                        budget_planned = i.budget_planned,
                                        budget_actual = i.budget_actual,
                                        budget_remaining = i.budget_remaining,
                                        budget_status = i.budget_status,
                                        otd = i.otd,
                                        planned_start_date = i.planned_start_date,
                                        planned_completion_date = i.planned_completion_date,
                                        actual_start_date = i.actual_start_date,
                                        actual_completion_date = i.actual_completion_date,
                                        is_active = true
                                    };
                                    db.TblProjectSubmission.Add(newProj);
                                }

                                db.SaveChanges();
                                transaction.Commit();
                            }
                            catch (Exception)
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                }

                return Json(new { flag = JsonResponseStandart.success, msg = "Sync success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = $"{ex.Message}\n{ex.Message}", data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

        [AllowAnonymous]
        public ActionResult CheckRunningTask()
        {
            try
            {
                var taskLog = new List<TblUserActivity>();
                using (var db = new TaskLogEntities())
                {
                    var taskNotStopped = db.TblUserActivity.Where(x => x.stopDatetime == null).ToList();
                    if(taskNotStopped.Count == 0)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "There is no task running right now.", data = "" }, JsonRequestBehavior.AllowGet);

                    var now = DateTime.Now;
                    foreach(var i in taskNotStopped)
                    {
                        var duration = (now - i.startDatetime.Value).TotalSeconds;
                        if(duration > 12 * 60 * 60)
                        {
                            i.stopDatetime = now;
                            i.duration = (int)duration;
                            taskLog.Add(i);
                        }
                    }
                    db.SaveChanges();
                }

                var data = taskLog.Select(x => new { 
                    x.badgeId
                    , x.fullName
                    , x.jobType
                    , x.taskTitle
                    , start = x.startDatetime.HasValue ? x.startDatetime.Value.ToString("dd MMM yyyy HH:mm:ss") : "-"
                    , stop = x.stopDatetime.HasValue ? x.stopDatetime.Value.ToString("dd MMM yyyy HH:mm:ss")  : "-"
                    , x.activity
                    , x.remarks
                }).ToList();
                
                if(taskLog.Count == 0)
                    return Json(new { flag = JsonResponseStandart.success, msg = "Task checked successfully. But there is no task need to be stopped.", data = "[]" }, JsonRequestBehavior.AllowGet);
                
                return Json(new { flag = JsonResponseStandart.success, msg = "Task checked successfully", data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = $"{ex.Message}\n{ex.Message}", data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }
        
        [HttpGet]
        [AllowAnonymous]
        public ActionResult GetAllActivity(DashboardITRequest param)
        {
            try
            {
                if (param is null)
                    return Json(new
                    {
                        success = 0,
                        count = 0,
                        messages = "Please provide parameters",
                    }, JsonRequestBehavior.AllowGet);

                var paramList = new List<string>();
                var sqlParameters = new List<object>();

                if (param.year.HasValue)
                {
                    paramList.Add("YEAR(startDatetime) = @year");
                    sqlParameters.Add(new System.Data.SqlClient.SqlParameter("@year", param.year.Value));

                    if (param.month.HasValue)
                    {
                        paramList.Add("MONTH(startDatetime) = @month");
                        sqlParameters.Add(new System.Data.SqlClient.SqlParameter("@month", param.month.Value));
                    }
                }

                if (!string.IsNullOrWhiteSpace(param.projectID))
                {
                    paramList.Add("taskId = @projectID");
                    sqlParameters.Add(new System.Data.SqlClient.SqlParameter("@projectID", param.projectID));
                }

                if (!string.IsNullOrWhiteSpace(param.sitePlant))
                {
                    paramList.Add("sitePlant = @sitePlant");
                    sqlParameters.Add(new System.Data.SqlClient.SqlParameter("@sitePlant", param.sitePlant));
                }

                string paramString = paramList.Any() ? "WHERE " + string.Join(" AND ", paramList) : "";

                string query = $@"
            select WeekNumber, fullName, SUM(duration) duration, taskId, taskTitle, MAX(sitePlant) as sitePlant
            from (
                select 
                DATEPART(week, startDatetime) AS WeekNumber
                , fullName
                , startDatetime
                , isnull(stopDatetime, getdate()) stopDatetime
                , case when stopDatetime is null then DATEDIFF(SECOND, startDatetime, GETDATE()) else duration end as duration
                , taskId
                , taskTitle
                , sitePlant
                from TblUserActivity
                {paramString}
            ) a
            group by WeekNumber, fullName, taskId, taskTitle";

                DateTime? startDate = null;
                DateTime? finishDate = null;
                var result = new List<SummaryActivity>();
                using (var db = new TaskLogEntities())
                {
                    result = db.Database.SqlQuery<SummaryActivity>(query, sqlParameters.ToArray()).ToList();

                    if (param.year.HasValue)
                    {
                        if (param.month.HasValue)
                        {
                            // Start date: 1st day of the given month and year
                            startDate = new DateTime(param.year.Value, param.month.Value, 1);
                            // Finish date: Last day of that specific month and year
                            int daysInMonth = DateTime.DaysInMonth(param.year.Value, param.month.Value);
                            finishDate = new DateTime(param.year.Value, param.month.Value, daysInMonth);
                        }
                        else
                        {
                            startDate = new DateTime(param.year.Value, 1, 1);
                            finishDate = new DateTime(param.year.Value, 12, 31);
                        }
                    }
                    else
                    {
                        startDate = db.TblUserActivity.Select(x => x.startDatetime).OrderBy(x => x).FirstOrDefault();
                        finishDate = db.TblUserActivity.Select(x => x.startDatetime).OrderByDescending(x => x).FirstOrDefault();
                    }
                }

                var data = new List<SummaryActivityResult>();
                foreach (var i in result.Select(x => x.WeekNumber).Distinct())
                {
                    var dataPerWeek = result.Where(x => x.WeekNumber == i).ToList();
                    foreach (var j in dataPerWeek.Select(x => x.fullName).Distinct())
                    {
                        var dataPerName = dataPerWeek.Where(x => x.fullName == j).ToList();
                        var personWeekly = new SummaryActivityResult
                        {
                            WeekNumber = i,
                            EmployeeName = j,
                            TotalProject = dataPerName.Count,
                            PlannedHours = 40 * 60 * 60,
                            ActualHours = dataPerName.Sum(x => x.duration), //in seconds
                                                                            //detail = dataPerName.Select(x=> new SimpleActivity { ProjectID = x.taskId, ProjectName=x.taskTitle, Duration = x.duration, SiteLocation = x.sitePlant }).ToList()
                        };
                        personWeekly.Percentage = Math.Round((personWeekly.ActualHours * 1.0 / personWeekly.PlannedHours), 2, MidpointRounding.AwayFromZero);
                        data.Add(personWeekly);
                    }
                }

                return Json(new
                {
                    startDate = startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : null,
                    finishDate = finishDate.HasValue ? finishDate.Value.ToString("yyyy-MM-dd") : null,
                    success = 1,
                    count = data.Count,
                    messages = "Data retrieved",
                    data,
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = 0,
                    count = 0,
                    messages = ex.ToString(),
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public ActionResult GetProjectActivity(string unique_id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(unique_id))
                    return Json(new
                    {
                        success = 0,
                        count = 0,
                        messages = "Please provide unique id",
                    }, JsonRequestBehavior.AllowGet);

                var paramList = new List<string>();
                var sqlParameters = new List<object>();

                paramList.Add("taskId = @uid");
                sqlParameters.Add(new System.Data.SqlClient.SqlParameter("@uid", unique_id));

                string paramString = paramList.Any() ? "WHERE " + string.Join(" AND ", paramList) : "";

                string query = $@"
                select badgeId as str1, fullName as str2, taskId as str3, taskTitle as str4, SUM(duration) as int1 
                from (
	                select taskId, taskTitle
	                , badgeId
	                , fullName
	                , startDatetime
	                , ISNULL(stopDatetime, getdate()) stopDatetime
	                , case when stopDatetime is null then DATEDIFF(second, startDatetime, GETDATE()) else duration end as duration
	                from dbo.TblUserActivity a
                    {paramString}
                ) a
                group by badgeId, fullName, taskId, taskTitle";

                var result = new List<GenericModel>();
                using (var db = new TaskLogEntities())
                {
                    result = db.Database.SqlQuery<GenericModel>(query, sqlParameters.ToArray()).ToList();
                }

                if(result.Count == 0)
                {
                    return Json(new
                    {
                        success = 1,
                        count = 0,
                        messages = "Data retrieved but no data exist",
                        data = result,
                    }, JsonRequestBehavior.AllowGet);
                }

                var data = result.Select(x => new { Fullname = x.str2, UniqueId = x.str3, ProjectName = x.str4, Duration = x.int1 }).ToList();

                return Json(new
                {
                    success = 1,
                    count = data.Count,
                    messages = "Data retrieved",
                    data = data,
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = 0,
                    count = 0,
                    messages = ex.ToString(),
                }, JsonRequestBehavior.AllowGet);
            }
        }

        //[AllowAnonymous]
        //public ActionResult StopAllTask()
        //{
        //    try
        //    {
        //        var taskLog = new List<TblUserActivity>();
        //        using (var db = new TaskLogEntities())
        //        {
        //            var taskNotStopped = db.TblUserActivity.Where(x => x.stopDatetime == null).ToList();
        //            if(taskNotStopped.Count == 0)
        //                return Json(new { flag = JsonResponseStandart.failed, msg = "There is no task running right now.", data = "" }, JsonRequestBehavior.AllowGet);

        //            var now = DateTime.Now;
        //            foreach(var i in taskNotStopped)
        //            {
        //                var duration = (now - i.startDatetime.Value).TotalSeconds;
        //                i.stopDatetime = now;
        //                i.duration = (int)duration;
        //                taskLog.Add(i);
        //            }
        //            db.SaveChanges();
        //        }

        //        var data = taskLog.Select(x => new { 
        //            x.badgeId
        //            , x.fullName
        //            , x.jobType
        //            , x.taskTitle
        //            , start = x.startDatetime.HasValue ? x.startDatetime.Value.ToString("dd MMM yyyy HH:mm:ss") : "-"
        //            , stop = x.stopDatetime.HasValue ? x.stopDatetime.Value.ToString("dd MMM yyyy HH:mm:ss")  : "-"
        //            , x.activity
        //            , x.remarks
        //        }).ToList();
                
        //        if(taskLog.Count == 0)
        //            return Json(new { flag = JsonResponseStandart.success, msg = "Action completed, But there is no task need to be stopped.", data = "[]" }, JsonRequestBehavior.AllowGet);
                
        //        return Json(new { flag = JsonResponseStandart.success, msg = "All task stopped.", data }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { flag = JsonResponseStandart.error, msg = $"{ex.Message}\n{ex.Message}", data = ex.ToString() }, JsonRequestBehavior.AllowGet);
        //    }
        //}
    }
}
