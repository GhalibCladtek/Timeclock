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
        public ActionResult SyncProjectList()
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
                                var existingDbProjects = db.TblProjectSubmission.ToDictionary(x => x.unique_id);
                                var apiProjectIds = new HashSet<string>(submissionsData.Select(x => x.unique_id));

                                var projectsToDelete = existingDbProjects.Values.Where(local => !apiProjectIds.Contains(local.unique_id)).ToList();

                                if (projectsToDelete.Any())
                                {
                                    db.TblProjectSubmission.RemoveRange(projectsToDelete);
                                }

                                var idTemp = 0;
                                foreach (var i in submissionsData)
                                {
                                    if (i.status_summary == "Identification- Requested") continue;
                                    idTemp++;
                                    if (existingDbProjects.TryGetValue(i.unique_id, out var project))
                                    {
                                        // Update Existing
                                        project.project_title = i.project_title;
                                        project.requesting_department = i.requesting_department;
                                        project.project_initiator = i.project_initiator;
                                        project.strategy_objective = i.strategy_objective;
                                        project.site_location = i.site_location;
                                        project.problem_statement = i.problem_statement;
                                        project.status = i.status;
                                        project.status_summary = i.status_summary;
                                        project.priority = i.priority;
                                        project.cumulative_percentage = i.cumulative_percentage;
                                        project.budget_planned = i.budget_planned;
                                        project.budget_actual = i.budget_actual;
                                        project.budget_remaining = i.budget_remaining;
                                        project.budget_status = i.budget_status;
                                        project.otd = i.otd;
                                        project.planned_start_date = i.planned_start_date;
                                        project.planned_completion_date = i.planned_completion_date;
                                        project.actual_start_date = i.actual_start_date;
                                        project.actual_completion_date = i.actual_completion_date;
                                    }
                                    else
                                    {
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
                                            actual_completion_date = i.actual_completion_date
                                        };

                                        db.TblProjectSubmission.Add(newProj);
                                    }
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
                return Json(new { flag = JsonResponseStandart.error, msg = $"{ex.Message}\n{ex.InnerException?.Message}", data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

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
                                        actual_completion_date = i.actual_completion_date
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
                return Json(new { flag = JsonResponseStandart.error, msg = $"{ex.Message}\n{ex.InnerException?.Message}", data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
