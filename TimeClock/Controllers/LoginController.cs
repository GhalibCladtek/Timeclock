using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeClock.Helpers;

namespace TimeClock.Controllers
{
    public class LoginController : Controller
    {
        // GET: Login
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Index(string ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(string user, string pass, bool rememberMe = false, string rUrl = "")
        {
            try
            {
                using (var db = new Models.TaskLogEntities())
                {
                    var data = db.TblUsers.FirstOrDefault(u => u.username == user);

                    if (data != null)
                    {
                        if (BCrypt.Net.BCrypt.Verify(pass, data.password))
                        {
                            if(!data.isActive) return Json(new { flag = JsonResponseStandart.failed, msg = "Your account is not active. Please contact SCADA Team.", data = "" });
                            Session["Id"] = data.Id;
                            Session["Username"] = data.username;
                            Session["BadgeId"] = data.badgeId;
                            Session["FullName"] = data.fullName;
                            Session["Title"] = data.title;
                            Session["LevelPos"] = data.levelPosition;

                            if (rememberMe)
                            {
                                // ENCRYPT THE USERNAME BEFORE SAVING TO COOKIE!
                                string encryptedUsername = TimeClock.Helpers.CookieSecurity.Encrypt(data.username);

                                HttpCookie authCookie = new HttpCookie("AuthCookie", encryptedUsername);
                                authCookie.Expires = DateTime.Now.AddDays(30);

                                // Add the HttpOnly flag for extra security (prevents JavaScript/XSS from reading the cookie)
                                authCookie.HttpOnly = true;

                                Response.Cookies.Add(authCookie);
                            }

                            TempData["message"] = "Login success!";
                            TempData["code"] = "success";
                            return Json(new { flag = JsonResponseStandart.success, msg = "Login success!", url = !string.IsNullOrEmpty(rUrl) && Url.IsLocalUrl(rUrl) ? rUrl : Url.Action("Index", "Tracker") });
                        }
                        else
                        {
                            return Json(new { flag = JsonResponseStandart.failed, msg = "Password does not match!", data = "" });
                        }
                    }
                    else
                    {
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Account not found!", data = "" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = ex.ToString() });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Logout()
        {
            try
            {
                Session.Abandon();

                if (Request.Cookies["ASP.NET_SessionId"] != null)
                {
                    Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId") { Expires = DateTime.Now.AddDays(-1) });
                }

                if (Request.Cookies["AuthCookie"] != null)
                {
                    Response.Cookies.Add(new HttpCookie("AuthCookie") { Expires = DateTime.Now.AddDays(-1) });
                }

                TempData["message"] = "Logout success!";
                TempData["code"] = "success";
                return Json(new { flag = JsonResponseStandart.success, msg = "Logout success!", url = Url.Action("Index") });
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = ex.ToString() });
            }
        }
    }
}