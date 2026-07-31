using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeClock.Helpers;
using TimeClock.Models;
using TimeClock.Modul;
using System.Configuration;

namespace TimeClock.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult SummaryAccount()
        {
            return View();
        }

        public ActionResult GetListAccount() {
            try
            {
                using (var db = new TaskLogEntities())
                {
                    var accs = db.TblUsers.Select(x=> new { uid = x.Id, badge = x.badgeId, fname = x.fullName, title = x.title, level = x.levelPosition, active = x.isActive }).ToList();
                    return Json(new { flag = JsonResponseStandart.success, msg = "", data = accs }, JsonRequestBehavior.AllowGet);
                }
            }
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex?.InnerException?.Message, data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult Register(int id)
        {
            try
            {
                var acc = new TblUsers() { Id = id };
                if(id <= 0)
                {
                    ViewBag.Title = "Create New Account";
                    ViewBag.Mode = "ADD";
                    return PartialView("_AddEditAccount", acc);
                } 
                else
                {
                    using (var db = new TaskLogEntities())
                    {
                        acc = db.TblUsers.FirstOrDefault(x => x.Id == id);
                        if(acc != null)
                        {
                            ViewBag.Title = "Edit Account";
                            ViewBag.Mode = "EDIT";
                            return PartialView("_AddEditAccount", acc);
                        }
                        else
                        {
                            return Json(new { flag = JsonResponseStandart.failed, msg = "Cannot find account", data = "" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex?.InnerException?.Message, data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }
        
        public ActionResult SaveRegister(string badgeId, string fullName, string title, string password = "", int? Id = null)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { flag = JsonResponseStandart.error, msg = "Session expired. Please log in again." }, JsonRequestBehavior.AllowGet);
                }
                int currentUserId = (int)Session["Id"];
                string currentUserBadgeId = (string)Session["BadgeId"];

                string admBadgeId = ConfigurationManager.AppSettings["AdminBadgeID"];
                var adminBadgeId = admBadgeId.Split(',');
                if(!adminBadgeId.Contains(currentUserBadgeId)) 
                    return Json(new { flag = JsonResponseStandart.failed, msg = "You dont have previledge for this action.", data = "" });

                if (string.IsNullOrWhiteSpace(badgeId)) return Json(new { flag = JsonResponseStandart.failed, msg = "Badge ID cannot be empty.", data = "" });
                if (string.IsNullOrWhiteSpace(fullName)) return Json(new { flag = JsonResponseStandart.failed, msg = "Name cannot be empty.", data = "" });
                
                using (var db = new TaskLogEntities())
                {
                    var data = db.TblUsers.FirstOrDefault(x => x.Id == Id);
                    if(data != null) //update
                    {
                        data.fullName = fullName.Trim();
                        data.badgeId = badgeId.Trim();
                        data.username = badgeId;
                        data.title = title;

                    } else //add new
                    {
                        var user = new TblUsers();
                        user.fullName = fullName.Trim();
                        user.badgeId = badgeId.Trim();
                        user.username = badgeId;
                        user.title = title;
                        user.password = BCrypt.Net.BCrypt.HashPassword(password);
                        db.TblUsers.Add(user);
                    }
                    db.SaveChanges();
                }
                return Json(new { flag = JsonResponseStandart.success, msg = "Account saved!", data = "" });
            }
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult ActivateAccount(int id) { 
            try
            {
                using (var db = new TaskLogEntities())
                {
                    var acc = db.TblUsers.FirstOrDefault(x => x.Id == id);
                    if (acc != null)
                    {
                        acc.isActive = !acc.isActive;
                        db.SaveChanges();
                        return Json(new { flag = JsonResponseStandart.success, msg = $"Account for {acc.badgeId} - {acc.fullName}\nIs set to " + (acc.isActive ? "Activate" : "Not Active"), data = "" }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Cannot find the account.", data = "" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex?.InnerException?.Message, data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }
        
        [HttpGet]
        public ActionResult ResetPassword(int id) { 
            try
            {
                using (var db = new TaskLogEntities())
                {
                    var acc = db.TblUsers.FirstOrDefault(x => x.Id == id);
                    if (acc == null)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Cannot find the account.", data = "" }, JsonRequestBehavior.AllowGet);
                    
                    acc.password = BCrypt.Net.BCrypt.HashPassword(acc.username);
                    db.SaveChanges();
                    return Json(new { flag = JsonResponseStandart.success, msg = $"Account for {acc.badgeId} - {acc.fullName} password has been reset.", data = "" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex?.InnerException?.Message, data = ex.ToString() }, JsonRequestBehavior.AllowGet);
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

        #region Profil masing masing
        public ActionResult MyProfile()
        {
            var user = new TblUsers();
            using (var db = new TaskLogEntities())
            {
                int? id = (int)Session["Id"];
                if (id == null)
                {
                    TempData["code"] = JsonResponseStandart.error;
                    TempData["message"] = "Your session has expired, please log in again.";
                    return RedirectToAction("Index","Login");
                }

                user = db.TblUsers.FirstOrDefault(x=> x.Id == id);
                if(user == null)
                {
                    TempData["code"] = JsonResponseStandart.error;
                    TempData["message"] = "Your session has expired, please log in again.";
                }
            }
            ViewBag.User = user;
            return View();
        }

        public ActionResult GetRoleList() { 
            try
            {
                using (var db = new TaskLogEntities())
                {
                    int? id = (int)Session["Id"];
                    if (id == null)
                    {
                        TempData["code"] = JsonResponseStandart.error;
                        TempData["message"] = "Your session has expired, please log in again.";
                        return RedirectToAction("Index", "Login");
                    }

                    var user = db.TblRoles.Select(x=> x.roleName).ToList();
                    if (user.Count > 0)
                    {
                        return Json(new { flag = JsonResponseStandart.success, msg = "", data = user }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Roles is empty.", data = "" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = JsonResponseStandart.errorMsg(ex.InnerException?.Message), data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SaveProfile(int? id, string fullName, string badgeId, string title, string role)
        {
            if(id == null)
                return Json(new { flag = JsonResponseStandart.failed, msg = "Your profile is empty", data = "" });

            try
            {
                using (var db = new TaskLogEntities()) {
                    var user = db.TblUsers.FirstOrDefault(x => x.Id == id.Value);

                    if(user == null)
                        return Json(new { flag = JsonResponseStandart.error, msg = "Somehow your account is missing? Contact SCADA Team", data = "" });

                    var roleUser = db.TblRoles.FirstOrDefault(x => x.roleName == role);
                    if(roleUser == null)
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Role not recognize", data = "" }, JsonRequestBehavior.AllowGet);

                    user.fullName = fullName;
                    user.title = title;
                    user.role = roleUser.roleName;
                    user.levelPosition = roleUser.roleLevel.Value;
                    db.SaveChanges();
                    return Json(new { flag = JsonResponseStandart.success, msg = "Profile saved." });
                }
            }
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = JsonResponseStandart.errorMsg(ex.InnerException?.Message), data = ex.ToString() });
            }
        }

        [HttpPost]
        public ActionResult UpdatePassword(int? id, string oldPass, string newPass, string confirmPass)
        {
            if(id == null)
                return Json(new { flag = JsonResponseStandart.failed, msg = "Your profile is empty", data = "" });

            if (string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirmPass))
                return Json(new { flag = JsonResponseStandart.failed, msg = "Please provide the password.", data = "" });

            try
            {
                using (var db = new TaskLogEntities()) {
                    var user = db.TblUsers.FirstOrDefault(x => x.Id == id.Value);

                    if(user == null)
                        return Json(new { flag = JsonResponseStandart.error, msg = "Somehow your account is missing? Contact SCADA Team", data = "" });

                    if (!BCrypt.Net.BCrypt.Verify(oldPass, user.password))
                        return Json(new { flag = JsonResponseStandart.error, msg = "Your current password and your old password does not match!", data = "" });

                    user.password = BCrypt.Net.BCrypt.HashPassword(newPass);
                    db.SaveChanges();
                    return Json(new { flag = JsonResponseStandart.success, msg = "Password updated." });
                }
            }
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = JsonResponseStandart.errorMsg(ex.InnerException?.Message), data = ex.ToString() });
            }
        }
        #endregion
    }
}
