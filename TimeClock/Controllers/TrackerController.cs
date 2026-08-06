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
    public class TrackerController : Controller
    {
        // GET: Tracker
        public ActionResult Index()
        {
            ViewBag.BadgeId = Session["BadgeId"];
            ViewBag.FullName = Session["FullName"];
            ViewBag.Section = Session["Title"];
            return View();
        }

        // GET: Tracker/GetOpenTask
        // Returns the currently-running task for the logged-in user, or null.
        [HttpGet]
        public JsonResult GetOpenTask()
        {
            var badgeId = Session["BadgeId"] as string;

            using (var db = new TaskLogEntities())
            {
                var open = db.TblUserActivity
                    .Where(t => t.badgeId == badgeId && t.stopDatetime == null)
                    .OrderByDescending(t => t.startDatetime)
                    .FirstOrDefault();

                if (open == null)
                    return Json( new { flag = JsonResponseStandart.failed, msg = "There is no Open Task", data = open }, JsonRequestBehavior.AllowGet);

                return Json(new { flag = JsonResponseStandart.success, msg = "", data = open }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Tracker/GetTaskList  (dropdown source - the "other table")
        [HttpGet]
        public JsonResult GetTaskList()
        {
            var allowed = WorkingHoursHelper.IsWithinWorkingHours(DateTime.Now);
            using (var db = new TaskLogEntities())
            {
                var tasks = db.TblProjectSubmission
                    .OrderBy(t => t.unique_id)
                    .Select(t => new { Id = t.unique_id, taskTitle = t.project_title })
                    .ToList();

                if (!allowed)
                {
                    tasks = tasks.Where(x => "MEET,OPCA,OPCBM,OPCTB,TRAIN".Split(',').Contains(x.Id)).ToList();
                }

                return Json(new { flag = JsonResponseStandart.success, msg = "", data = tasks }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Tracker/GetJobTypeOptions
        // Tells the UI which JobType values are allowed right now (server clock, not client's).
        [HttpGet]
        public JsonResult GetJobTypeOptions()
        {
            var now = DateTime.Now;
            var allowed = WorkingHoursHelper.GetAllowedJobTypes(now);
            return Json(new { flag = JsonResponseStandart.success, msg = "", data = allowed }, JsonRequestBehavior.AllowGet);
        }

        // POST: Tracker/StartTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult StartTask(string taskId, string jobType, string section, string activity, string remarks)
        {
            try
            {

                var badgeId = Session["BadgeId"] as string;
                var fullName = Session["FullName"] as string;
                //var section = Session["Title"] as string;

                if (string.IsNullOrEmpty(badgeId))
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Session expired. Please log in again.", data = "" });

                if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(jobType))
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Please choose a task and job type.", data = "" });

                var now = DateTime.Now;

                // Rule 1: JobType must be allowed at this moment (server-authoritative,
                // never trust a value the client could have tampered with).
                var allowed = WorkingHoursHelper.GetAllowedJobTypes(now);
                if (!allowed.Contains(jobType))
                {
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Outside working hours / on weekends, only 'Operational' tasks can be started.", data = "" });
                }

                using (var db = new TaskLogEntities())
                {
                    // Rule 2: only one active task per user.
                    var alreadyRunning = db.TblUserActivity.Any(t =>
                        t.badgeId == badgeId && t.stopDatetime == null);

                    if (alreadyRunning)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "You already have a running task. Please stop it before starting a new one.", data = "" });

                    var task = db.TblProjectSubmission.FirstOrDefault(t => t.unique_id == taskId);
                    if (task == null)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Selected task no longer exists.", data = "" });

                    var log = new TblUserActivity
                    {
                        jobType = jobType,
                        badgeId = badgeId,
                        fullName = fullName,
                        section = section,
                        startDatetime = now,
                        stopDatetime = null,
                        duration = null,
                        taskId = task.unique_id,
                        taskTitle = task.project_title,
                        remarks = remarks,
                        activity = activity
                    };

                    db.TblUserActivity.Add(log);
                    db.SaveChanges();

                    return Json(new { flag = JsonResponseStandart.success, msg = "Task Started.", data = log });
                }
            } 
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = "Task failed to start.\nError messages: " + ex.Message, data = ex.ToString() });
            }
        }

        // POST: Tracker/StopTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult StopTask(int id)
        {
            var badgeId = Session["BadgeId"] as string;

            using (var db = new TaskLogEntities())
            {
                var log = db.TblUserActivity.FirstOrDefault(t =>
                    t.Id == id && t.badgeId == badgeId && t.stopDatetime== null);

                if (log == null)
                    return Json( new { flag = JsonResponseStandart.failed, msg = "Task not found or already stopped.", data = "" });

                var now = DateTime.Now;
                log.stopDatetime = now;
                log.duration = (int)Math.Round((now - log.startDatetime).Value.TotalSeconds);
                db.SaveChanges();

                return Json( new { flag = JsonResponseStandart.success, msg = "Activity stopped.", data = new { log.Id, log.duration, log.stopDatetime }});
            }
        }
        // POST: Tracker/StopTask
        [HttpPost]
        public JsonResult StopTaskMember(string badgeId)
        {
            try
            {
                var currentBadgeId = Session["BadgeId"] as string;
                if (string.IsNullOrWhiteSpace(currentBadgeId))
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Session expired, please login again.", data = "" });
                if (string.IsNullOrWhiteSpace(badgeId))
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Badge ID of the member cannot be empty.", data = "" });

                var membersBadgeIs = badgeId.Split(',');
                if (membersBadgeIs.Count() == 0)
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Badge ID of the member cannot be empty.", data = "" });

                using (var db = new TaskLogEntities())
                {
                    var superiorAccount = db.TblUsers.FirstOrDefault(x => x.badgeId == currentBadgeId);
                    if (superiorAccount == null)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Cannot find leader account.", data = "" });

                    var memberBadgeIdList = superiorAccount.subMemberBadgeIds?.Split(',').ToList();
                    if (memberBadgeIdList == null || memberBadgeIdList.Count == 0)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "You don't have any member.", data = "" });

                    var members = TeamController.GetSubordinateMember(superiorAccount.badgeId);
                    members.RemoveAt(0);

                    var resultList = new List<StopActionForMembers>();
                    foreach (var i in membersBadgeIs)
                    {
                        var user = db.TblUsers.FirstOrDefault(x => x.badgeId == i);
                        if (!memberBadgeIdList.Any(x => x == i))
                        {
                            resultList.Add(new StopActionForMembers { badgeId = i, fullName = user.fullName, success = false, msg = "This account is not your team member." });
                            continue;
                        }

                        var log = db.TblUserActivity.FirstOrDefault(t => t.badgeId == badgeId && t.stopDatetime == null);
                        if (log == null)
                        {
                            resultList.Add(new StopActionForMembers { badgeId = i, fullName = user.fullName, success = false, msg = "Task not found or already stopped." });
                            continue;
                        }

                        var now = DateTime.Now;
                        log.stopDatetime = now;
                        log.duration = (int)Math.Round((now - log.startDatetime).Value.TotalSeconds);
                        db.SaveChanges();

                        resultList.Add(new StopActionForMembers { badgeId = i, fullName = user.fullName, success = true, msg = "Activity stopped." });
                    }
                    return Json(new { flag = JsonResponseStandart.success, msg = string.Join("\n",resultList.Select(x=> x.badgeId + " - " + x.msg)), data = resultList });
                }
            }
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = $"{ex.Message} {ex.InnerException?.Message}", data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }

        }

        // GET: Tracker/GetHistory?page=1&pageSize=20
        [HttpGet]
        public JsonResult GetHistory(int page = 1, int pageSize = 20)
        {
            var badgeId = Session["BadgeId"] as string;

            using (var db = new TaskLogEntities())
            {
                var query = db.TblUserActivity
                    .Where(t => t.badgeId == badgeId)
                    .OrderByDescending(t => t.startDatetime);

                var total = query.Count();
                var items = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.Id,
                        t.taskTitle,
                        t.jobType,
                        t.startDatetime,
                        t.stopDatetime,
                        t.duration,
                        t.remarks,
                        t.activity
                    })
                    .ToList();

                if (total > 0) { 
                    return Json( new { flag = JsonResponseStandart.success, msg = "History fetched.", data = new { total, items } }, JsonRequestBehavior.AllowGet);
                } else
                {
                    return Json( new { flag = JsonResponseStandart.failed, msg = "History not found.", data = "" }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpGet]
        public ActionResult GetSection() {
            try
            {
                using (var db = new TaskLogEntities())
                {
                    var section = db.TblSectionActivity.Select(x=> x.section).Distinct().ToList();
                    if(section == null) 
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Activity not found.", data = "" }, JsonRequestBehavior.AllowGet);
                    
                    return Json(new { flag = JsonResponseStandart.success, msg = "", data = section }, JsonRequestBehavior.AllowGet);
                }
            } 
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = $"{ex.Message} {ex.InnerException?.Message}", data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetActivityType(string section) {
            try
            {
                if(string.IsNullOrWhiteSpace(section))
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Activity not found.", data = "" }, JsonRequestBehavior.AllowGet);

                using (var db = new TaskLogEntities())
                {
                    var act = db.TblSectionActivity.Where(x => x.section.ToLower() == section.ToLower()).Select(x=> x.activity).ToList();
                    if(act == null) 
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Activity not found.", data = "" }, JsonRequestBehavior.AllowGet);
                    
                    return Json(new { flag = JsonResponseStandart.success, msg = "", data = act }, JsonRequestBehavior.AllowGet);
                }
            } 
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = $"{ex.Message} {ex.InnerException?.Message}", data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }
        
        [HttpGet]
        public ActionResult GetProjectDetail(string unique_id) {
            try
            {
                using (var db = new TaskLogEntities())
                {
                    var project = db.TblProjectSubmission.FirstOrDefault(x => x.unique_id == unique_id);
                    if(project == null) 
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Project not found.", data = "" }, JsonRequestBehavior.AllowGet);
                    
                    return Json(new { flag = JsonResponseStandart.success, msg = "Project found.", data = project }, JsonRequestBehavior.AllowGet);
                }
            } 
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = $"{ex.Message} {ex.InnerException?.Message}", data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }


        // POST: Tracker/StartTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult StartTaskForMember(string memberBadgeId, string taskId, string jobType, string section, string activity, string remarks)
        {
            try
            {
                var now = DateTime.Now;
                var badgeId = Session["BadgeId"] as string;
                var fullName = Session["FullName"] as string;
                //var section = Session["Title"] as string;

                if (string.IsNullOrEmpty(badgeId))
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Session expired. Please log in again.", data = "" });


                if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(jobType))
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Please choose a task and job type.", data = "" });

                if(string.IsNullOrWhiteSpace(memberBadgeId))
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Please choose atleast one member.", data = "" });

                var members = memberBadgeId.Split(',');
                if(members.Count() == 0)
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Please choose atleast one member.", data = "" });

                // Rule 1: JobType must be allowed at this moment (server-authoritative,
                // never trust a value the client could have tampered with).
                var allowed = WorkingHoursHelper.GetAllowedJobTypes(now);
                if (!allowed.Contains(jobType))
                {
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Outside working hours / on weekends, only 'Operational' tasks can be started.", data = "" });
                }

                using (var db = new TaskLogEntities())
                {
                    var task = db.TblProjectSubmission.FirstOrDefault(t => t.unique_id == taskId);
                    if (task == null)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Selected task no longer exists.", data = "" });

                    var oMember = new List<TblUsers>();
                    foreach (var i in members)
                    {
                        var user = db.TblUsers.FirstOrDefault(x => x.badgeId == i);
                        if (user == null)
                            continue;

                        oMember.Add(user);

                        var alreadyRunning = db.TblUserActivity.FirstOrDefault(t =>t.badgeId == user.badgeId && t.stopDatetime == null);
                        if (alreadyRunning != null)
                        {
                            alreadyRunning.stopDatetime = now;
                            alreadyRunning.duration = (int)(now - alreadyRunning.startDatetime.Value).TotalSeconds;
                        }

                        var log = new TblUserActivity
                        {
                            jobType = jobType,
                            badgeId = user.badgeId,
                            fullName = user.fullName,
                            section = section,
                            startDatetime = now,
                            stopDatetime = null,
                            duration = null,
                            taskId = task.unique_id,
                            taskTitle = task.project_title,
                            remarks = remarks,
                            activity = activity
                        };

                        db.TblUserActivity.Add(log);
                    }
                    
                    db.SaveChanges();
                    var memberText = string.Join(", ", oMember.Select(x => x.badgeId + " - " + x.fullName));
                    return Json(new { flag = JsonResponseStandart.success, msg = $"Task Started for {memberText}.", data = "" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = "Task failed to start.\nError messages: " + ex.Message, data = ex.ToString() });
            }
        }

        [HttpGet]
        public JsonResult GetSummaryTimeToday()
        {
            try
            {
                var badgeId = Session["BadgeId"] as string;
                using (var db = new TaskLogEntities())
                {
                    var project = db.TblUserActivity.Where(x=> x.badgeId == badgeId && x.startDatetime >= DateTime.Today).Sum(x=> x.duration.Value);
                    if (project == null)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Project not found.", data = "" }, JsonRequestBehavior.AllowGet);

                    return Json(new { flag = JsonResponseStandart.success, msg = "Project found.", data = project }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = $"{ex.Message} {ex.InnerException?.Message}", data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}