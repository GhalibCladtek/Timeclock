using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeClock.Helpers;
using TimeClock.Models;

namespace TimeClock.Controllers
{
    public class TaskController : Controller
    {
        [HttpPost]
        public ActionResult ActionSubTask(int id, string command)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(command)) return Json(new { flag = JsonResponseStandart.failed, msg = "Your command doesn't recognize by system", data = "" });
                int currentUserId = (int)Session["Id"];
                using (var db = new TaskLogEntities())
                {
                    var task = db.TblTaskByIndividual.FirstOrDefault(x => x.Id == id && !x.deleted);
                    if (task != null)
                    {
                        var mainTask = db.TblTaskBySuperior.FirstOrDefault(x => x.Id == task.IdTBS && !x.deleted);
                        if (task.ownerId != currentUserId) return Json(new { flag = JsonResponseStandart.failed, msg = "This task doesn't belong to you.", data = "" });
                        if(command == "start")
                        {
                            task.startDate = DateTime.Now;
                            task.updateAt = DateTime.Now;
                            task.updateBy = (string)Session["BadgeId"];
                            task.currentStatus = StatusTask.OnGoing;

                        } else if(command == "pause")
                        {
                            task.updateAt = DateTime.Now;
                            task.updateBy = (string)Session["BadgeId"];
                            task.currentStatus = StatusTask.Paused;

                        } else if(command == "finish")
                        {
                            task.finishDate = DateTime.Now;
                            task.updateAt = DateTime.Now;
                            task.updateBy = (string)Session["BadgeId"];
                            task.currentStatus = StatusTask.Finish;

                        } else if (command == "continue")
                        {
                            task.updateAt = DateTime.Now;
                            task.updateBy = (string)Session["BadgeId"];
                            task.currentStatus = StatusTask.OnGoing;
                        } else if (command == "see")
                        {
                            return EditTask(id);
                        }
                        else
                        {
                            return Json(new { flag = JsonResponseStandart.failed, msg = "Your command doesn't recognize by system.\nYour command was: " + command, data = "" });
                        }
                        db.SaveChanges();

                        if (mainTask != null)
                        {
                            var allSubTask = db.TblTaskByIndividual.Where(x => !x.deleted && !x.canceled && x.IdTBS == mainTask.Id).Select(x => x.currentStatus).ToList();
                            if(allSubTask.Count > 0)
                            {
                                if(allSubTask.Distinct().Count() == 1)
                                {
                                    mainTask.status = allSubTask.FirstOrDefault();
                                } else if (allSubTask.Contains(StatusTask.OnGoing))
                                {
                                    mainTask.status = StatusTask.OnGoing;
                                }
                            }
                        }

                        db.SaveChanges();
                        return Json(new { flag = JsonResponseStandart.success, msg = $"Task updated to {command}", data = "" });
                    }
                    else
                    {
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Task not found.", data = "" });
                    }
                }
            }
            catch(Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = ex.ToString() });
            }
        }

        [HttpGet]
        public ActionResult EditTask(int id)
        {

            try
            {
                if (id <= 0) return Json(new { flag = JsonResponseStandart.failed, msg = "Invalid task." }, JsonRequestBehavior.AllowGet);
                else
                {
                    using (var db = new TaskLogEntities())
                    {
                        var data = db.TblTaskByIndividual.FirstOrDefault(x => x.Id == id);
                        if (data == null) return Json(new { flag = JsonResponseStandart.failed, msg = "Task not found!" }, JsonRequestBehavior.AllowGet);

                        var project = db.TblTaskBySuperior.FirstOrDefault(x => x.Id == data.IdTBS);
                        if (project == null) return Json(new { flag = JsonResponseStandart.failed, msg = "Project for this task is not found!" }, JsonRequestBehavior.AllowGet);

                        var result = new ProjectTaskDetail()
                        {
                            title = project.title,
                            description = project.description,
                            assignBy = project.ownerBadgeId,
                            dueDate = project.dueDate,
                            Id = data.Id,
                            IdTBS = data.IdTBS,
                            ownerId = data.ownerId,
                            ownerBadgeId = data.ownerBadgeId,
                            ownerFullName = data.ownerFullName,
                            startDate = data.startDate,
                            finishDate = data.finishDate,
                            currentStatus = data.currentStatus,
                            remarks = data.remarks,
                            remarksWhenCanceled = data.remarksWhenCanceled,
                            remarksWhenPaused = data.remarksWhenPaused,
                            attachment = data.attachment,
                            canceled = data.canceled
                        };

                        return PartialView("_EditTask", result);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }
        
        [HttpPost]
        public ActionResult EditTask(ProjectTaskDetail task, HttpPostedFileBase attachment)
        {
            if (task == null) return Json(new { flag = JsonResponseStandart.failed, msg = "Task cannot be found!" }, JsonRequestBehavior.AllowGet);
            
            try
            {
                if (task.Id <= 0) return Json(new { flag = JsonResponseStandart.failed, msg = "Invalid task." });
                using (var db = new TaskLogEntities())
                {
                    var data = db.TblTaskByIndividual.FirstOrDefault(x => x.Id == task.Id);
                    if (data == null) return Json(new { flag = JsonResponseStandart.failed, msg = "Task not found!" });

                    // --- NEW UPLOAD LOGIC STARTS HERE ---
                    if (attachment != null && attachment.ContentLength > 0)
                    {
                        // 1. Get extension and generate a secure name
                        string fileExtension = System.IO.Path.GetExtension(attachment.FileName);
                        string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;

                        // 2. Define the path
                        string uploadFolderPath = Server.MapPath("~/App_Data/Attachments/");
                        if (!System.IO.Directory.Exists(uploadFolderPath))
                        {
                            System.IO.Directory.CreateDirectory(uploadFolderPath);
                        }

                        // 3. Save the file to the server
                        string finalSavePath = System.IO.Path.Combine(uploadFolderPath, uniqueFileName);
                        attachment.SaveAs(finalSavePath);

                        // 4. Update the database record with the NEW file name
                        data.attachment = uniqueFileName;
                    }
                    // Note: If 'attachment' is null, we do NOTHING to data.attachment. 
                    // This ensures the old file isn't accidentally deleted if the user just updates the remarks.
                    // --- NEW UPLOAD LOGIC ENDS HERE ---

                    data.remarks = task.remarks;
                    data.updateAt = DateTime.Now;
                    data.updateBy = (string)Session["BadgeId"];

                    db.SaveChanges();

                    return Json(new { flag = JsonResponseStandart.success, msg = "Task updated." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = ex.ToString() });
            }
        }
    }
}
