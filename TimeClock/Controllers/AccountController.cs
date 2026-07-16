using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeClock.Helpers;

namespace TimeClock.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Edit(string user)
        {
            using (var db = new Models.TaskLogEntities())
            { 
                var data = db.TblUsers.FirstOrDefault(u => u.username == user);
                if(data != null)
                {
                    return PartialView("_EditAccount", data);
                } else
                {
                    return Json(new { flag = JsonResponseStandart.failed, msg = "User not found. Try to refresh the page first, then try again.", data = "" });
                }
            }

        }

        [HttpPost]
        public ActionResult Edit(Models.TblUsers user)
        {
            try
            {
                if(user != null)
                {
                    using (var db = new Models.TaskLogEntities())
                    {
                        var data = db.TblUsers.FirstOrDefault(x => x.username == user.username);
                        if(data != null)
                        {
                            if(string.IsNullOrWhiteSpace(user.badgeId)) return Json(new { flag = JsonResponseStandart.failed, msg = "Badge ID cannot be empty.", data = "" });
                            if(string.IsNullOrWhiteSpace(user.fullName)) return Json(new { flag = JsonResponseStandart.failed, msg = "Name cannot be empty.", data = "" });
                            data.fullName = user.fullName.Trim();
                            data.badgeId = user.badgeId.Trim();
                            data.title = user.title;
                            if (!string.IsNullOrWhiteSpace(user.password))
                            {
                                data.password = BCrypt.Net.BCrypt.HashPassword(user.password);
                            }
                            db.SaveChanges();

                            Session["Id"] = data.Id;
                            Session["Username"] = data.username;
                            Session["BadgeId"] = data.badgeId;
                            Session["FullName"] = data.fullName;
                            Session["Title"] = data.title;
                            Session["LevelPos"] = data.levelPosition;

                            return Json(new { flag = JsonResponseStandart.success, msg = "Profile updated!", data = "" });
                        } else
                        {
                            return Json(new { flag = JsonResponseStandart.failed, msg = "User cannot be found.", data = "" });
                        }
                    }
                } else
                {
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Failed saving profile.", data = "" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = ex.ToString() });
            }
        }
    }
}
