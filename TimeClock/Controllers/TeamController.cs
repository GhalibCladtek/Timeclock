using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeClock.Helpers;
using TimeClock.Models;

namespace TimeClock.Controllers
{
    public class TeamController : Controller
    {
        // GET: Team
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult StartTaskMember()
        {
            var userId = (int?)Session["Id"];

            if (!userId.HasValue)
                return Json(new { flag = JsonResponseStandart.failed, msg = "Session expired. Please log in again.", data = "" }, JsonRequestBehavior.AllowGet);

            using (var db = new TaskLogEntities())
            {
                var user = db.TblUsers.FirstOrDefault(x=> x.Id == userId);
                if (user == null)
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Invalid user or account has been deleted.", data = "" }, JsonRequestBehavior.AllowGet);

                var membersBadgeId = user.subMemberBadgeIds;

                if(string.IsNullOrWhiteSpace(membersBadgeId))
                    return Json(new { flag = JsonResponseStandart.failed, msg = "You dont have any members", data = "" }, JsonRequestBehavior.AllowGet);

                var oMembersBadgeID = membersBadgeId.Split(',').Where(x=> !string.IsNullOrWhiteSpace(x)).ToList();
                if(oMembersBadgeID.Count == 0)
                    return Json(new { flag = JsonResponseStandart.failed, msg = "You dont have any members", data = "" }, JsonRequestBehavior.AllowGet);

                var memberList = new List<TblUsers>();
                foreach(var i in oMembersBadgeID)
                {
                    var member = GetSubordinateMember(i);
                    memberList.AddRange(member);
                }

                var oMember = memberList.Select(x=> new SelectListItem { Text = x.badgeId + " - " + x.fullName, Value = x.badgeId }).ToList();
                ViewBag.MemberList = oMember;
                return PartialView("_ModalStartTaskForMember");
            }

        }

        [HttpGet]
        public ActionResult GetMemberList()
        {
            var userId = (int?)Session["Id"];

            if (!userId.HasValue)
                return Json(new { flag = JsonResponseStandart.failed, msg = "Session expired. Please log in again.", data = "" }, JsonRequestBehavior.AllowGet);
            try
            {
                using (var db = new TaskLogEntities())
                {
                    var user = db.TblUsers.FirstOrDefault(x => x.Id == userId);
                    if (user == null)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Invalid user or account has been deleted.", data = "" }, JsonRequestBehavior.AllowGet);

                    var membersBadgeId = user.subMemberBadgeIds;

                    if (string.IsNullOrWhiteSpace(membersBadgeId))
                        return Json(new { flag = JsonResponseStandart.failed, msg = "You dont have any members", data = "" }, JsonRequestBehavior.AllowGet);

                    var oMembersBadgeID = membersBadgeId.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                    if (oMembersBadgeID.Count == 0)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "You dont have any members", data = "" }, JsonRequestBehavior.AllowGet);

                    var memberList = new List<TblUsers>();
                    foreach (var i in oMembersBadgeID)
                    {
                        var member = GetSubordinateMember(i);
                        memberList.AddRange(member);
                    }

                    var oMember = memberList.Select(x => new SelectListItem { Text = x.badgeId + " - " + x.fullName, Value = x.badgeId }).ToList();

                    var lastTaskMember = new List<Members>();
                    foreach (var i in memberList)
                    {
                        var task = db.TblUserActivity.Where(x => x.badgeId == i.badgeId && x.startDatetime >= DateTime.Today).ToList();

                        lastTaskMember.Add(new Members
                        {
                            badgeId = i.badgeId,
                            fullName = i.fullName,
                            duration = task.Sum(x => x.duration.HasValue ? x.duration.Value : (int)(DateTime.Now - x.startDatetime.Value).TotalSeconds),
                        }); ;
                    }
                    return Json(new { flag = JsonResponseStandart.success, msg = "", data = lastTaskMember }, JsonRequestBehavior.AllowGet);
                }
            }
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.failed, msg = ex.InnerException?.Message, data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetMemberTaskDetail(string badgeId)
        {
            var userId = (int?)Session["Id"];

            if (!userId.HasValue)
                return Json(new { flag = JsonResponseStandart.failed, msg = "Session expired. Please log in again.", data = "" }, JsonRequestBehavior.AllowGet);
            
            if (string.IsNullOrWhiteSpace(badgeId))
                return Json(new { flag = JsonResponseStandart.failed, msg = "Please fill the badge Id first.", data = "" }, JsonRequestBehavior.AllowGet);
            
            try
            {
                using (var db = new TaskLogEntities())
                {
                    var task = db.TblUserActivity.Where(x => x.badgeId == badgeId && x.startDatetime >= DateTime.Today).OrderByDescending(x => x.Id).ToList();
                    return Json(new { flag = JsonResponseStandart.success, msg = "", data = task }, JsonRequestBehavior.AllowGet);
                }
            }
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.failed, msg = ex.InnerException?.Message, data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

        public static List<TblUsers> GetSubordinateMember(string badgeId)
        {
            var members = new List<TblUsers>();
            if (string.IsNullOrWhiteSpace(badgeId))
                return members;

            using (var db = new TaskLogEntities())
            {
                var user = db.TblUsers.FirstOrDefault(x => x.badgeId == badgeId);
                if (user == null)
                    return members;

                members.Add(user);
                // Load all active users once to avoid repeated DB round trips
                var allUsers = db.TblUsers.Where(x => x.isActive).ToList();

                var userByBadge = allUsers
                    .Where(u => !string.IsNullOrWhiteSpace(u.badgeId))
                    .ToDictionary(u => u.badgeId, u => u);

                var visited = new HashSet<string>();
                var queue = new Queue<string>();

                // seed queue with direct subordinates of the starting user
                EnqueueSubBadges(user.subMemberBadgeIds, queue, visited);

                while (queue.Count > 0)
                {
                    var currentBadge = queue.Dequeue();

                    if (!userByBadge.TryGetValue(currentBadge, out var currentUser))
                        continue; // badge referenced but no matching user row, skip

                    members.Add(currentUser);

                    // enqueue this user's own subordinates
                    EnqueueSubBadges(currentUser.subMemberBadgeIds, queue, visited);
                }
            }

            return members;
        }

        public static void EnqueueSubBadges(string subMemberBadgeIds, Queue<string> queue, HashSet<string> visited)
        {
            if (string.IsNullOrWhiteSpace(subMemberBadgeIds))
                return;

            var badges = subMemberBadgeIds
                .Split(',')
                .Select(b => b.Trim())
                .Where(b => !string.IsNullOrEmpty(b));

            foreach (var b in badges)
            {
                if (visited.Add(b)) // returns false if already present, prevents cycles/duplicates
                {
                    queue.Enqueue(b);
                }
            }
        }
    }
}