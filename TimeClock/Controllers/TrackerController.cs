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
            using (var db = new TaskLogEntities())
            {
                var tasks = db.TblProjectSubmission
                    .OrderBy(t => t.id)
                    .Select(t => new { Id = t.id, taskTitle = t.project_title })
                    .ToList();

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
        public JsonResult StartTask(int taskId, string jobType, string section, string remarks)
        {
            try
            {

                var badgeId = Session["BadgeId"] as string;
                var fullName = Session["FullName"] as string;
                //var section = Session["Title"] as string;

                if (string.IsNullOrEmpty(badgeId))
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Session expired. Please log in again.", data = "" });

                if (taskId <= 0 || string.IsNullOrWhiteSpace(jobType))
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

                    var task = db.TblProjectSubmission.FirstOrDefault(t => t.id == taskId);
                    //if (task == null)
                    //    return Json( new { flag = JsonResponseStandart.failed, msg = "Selected task no longer exists.", data = "" });

                    var log = new TblUserActivity
                    {
                        jobType = jobType,
                        badgeId = badgeId,
                        fullName = fullName,
                        section = section,
                        startDatetime = now,
                        stopDatetime = null,
                        duration = null,
                        taskId = task.id,
                        taskTitle = task.project_title,
                        remarks = remarks
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
                        t.remarks
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

    }
}