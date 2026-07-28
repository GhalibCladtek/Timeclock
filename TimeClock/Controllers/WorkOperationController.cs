using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeClock.Models;
using TimeClock.Helpers;

namespace TimeClock.Controllers
{
    public class WorkOperationController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { flag = JsonResponseStandart.error, msg = "Session expired. Please log in again." }, JsonRequestBehavior.AllowGet);
                }
                int currentUserId = (int)Session["Id"];

                using (var db = new TaskLogEntities())
                {
                    var allIssue = db.TblWorkOperational.Where(x => !x.dlt).ToList();
                    var open = allIssue.Where(x => x.status == "open").ToList();
                    var inProgress = allIssue.Where(x => x.status == "in progress").ToList();
                    var closed = allIssue.Where(x => x.status == "closed").ToList();

                    ViewBag.OpenCount = open.Count;
                    ViewBag.Overdue = closed.Where(x=> x.actionFinish.Value < x.actionFinishActual.Value).Count();
                    ViewBag.AvgResolve = closed.Count == 0 ? "N/A" : DataToString.FormatTimeClean(closed.Sum(x=> x.durationActual.Value) / closed.Count);
                    ViewBag.SolvedInTime = closed.Count == 0 ? "N/A" : (closed.Where(x => x.actionFinish.Value >= x.actionFinishActual.Value).Count() / closed.Count() * 100.0).ToString() + "%";

                    ViewBag.IssueOpen = open.Count();
                    ViewBag.IssueOpen = open.Count();
                    ViewBag.IssueClosed = closed.Count();

                    return View();
                }
            }
            catch(Exception ex)
            {
                TempData["code"] = JsonResponseStandart.failed;
                TempData["message"] = ex.Message;
                return View();
            }
        }
    }
}