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
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult TaskSummary()
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
                    var taskViews = new List<TaskView>();
                    var task = db.TblTaskBySuperior.Where(x => x.ownerId == currentUserId && x.deleted == false).ToList();
                    if (task.Count > 0) 
                    {
                        foreach(var i in task)
                        {
                            var mainTask = new TaskView
                            {
                                mainTask = i,
                                submainTask = db.TblTaskByIndividual.Where(x=> x.deleted == false && x.IdTBS == i.Id && x.canceled == false).ToList()
                            };
                            taskViews.Add(mainTask);
                        }
                    }
                    return PartialView("_TaskSummary", taskViews);
                }

            } catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = "" });
            }
        }
        [HttpGet]
        public ActionResult CreateEditMainTask(int id)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { flag = JsonResponseStandart.error, msg = "Session expired. Please log in again." }, JsonRequestBehavior.AllowGet);
                }
                int currentUserId = (int)Session["Id"];
                if(id  <= 0 ) return PartialView("_AddEditTask", new TblTaskBySuperior());

                using (var db = new TaskLogEntities())
                {
                    var task = db.TblTaskBySuperior.FirstOrDefault(x => x.Id == id && x.deleted == false);
                    if(task != null)
                    {
                        if(task.ownerId != currentUserId) return PartialView("_AddEditTask", task);
                        return PartialView("_AddEditTask", task);
                    } else
                    {
                        return Json(new { flag = JsonResponseStandart.failed, msg = "Task not found." }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = "" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult CreateEditMainTask(TblTaskBySuperior task, List<string> workers)
        {
            try
            {
                if(task != null)
                {
                    if (Session["Id"] == null)
                    {
                        return Json(new { flag = JsonResponseStandart.error, msg = "Session expired. Please log in again." });
                    }
                    int currentUserId = (int)Session["Id"];

                    using (var db = new TaskLogEntities())
                    {
                        workers.Sort();
                        if (task.Id <= 0) //create new task
                        {
                            using (var transaction = db.Database.BeginTransaction())
                            {
                                try
                                {
                                    task.ownerId = currentUserId;
                                    task.ownerBadgeId = (string)Session["BadgeId"];
                                    task.ownerFullName = (string)Session["FullName"];
                                    task.createdAt = DateTime.Now;
                                    task.createdBy = (string)Session["BadgeId"];
                                    task.workersId = string.Join(", ", workers);
                                    task.status = "open";
                                    db.TblTaskBySuperior.Add(task);
                                    db.SaveChanges();

                                    foreach (var person in workers)
                                    {
                                        string badgeId = person.Split('-')[0].Trim();
                                        var user = db.TblUsers.FirstOrDefault(x => x.badgeId == badgeId);
                                        if (user != null)
                                        {
                                            var subTask = new TblTaskByIndividual();
                                            subTask.IdTBS = task.Id;
                                            subTask.ownerId = user.Id;
                                            subTask.ownerBadgeId = user.badgeId;
                                            subTask.ownerFullName = user.fullName;
                                            subTask.createdAt = DateTime.Now;
                                            subTask.createdBy = (string)Session["BadgeId"];
                                            subTask.currentStatus = "open";
                                            db.TblTaskByIndividual.Add(subTask);
                                        }
                                    }
                                    db.SaveChanges();
                                    transaction.Commit();
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    return Json(new { flag = JsonResponseStandart.error, msg = "Create task failed.\n" + ex.Message, data = ex.ToString() });
                                }
                            }
                        } 
                        else //edit existing task
                        {
                            using (var transaction = db.Database.BeginTransaction())
                            {
                                try
                                {
                                    var data = db.TblTaskBySuperior.FirstOrDefault(x => x.deleted == false && x.Id == task.Id);
                                    if (data == null) return Json(new { flag = JsonResponseStandart.failed, msg = "Task not found." });
                                    if (data.ownerId != currentUserId) return Json(new { flag = JsonResponseStandart.failed, msg = "This task isn't yours. Cannot edit other people task." });
                                    data.title = task.title;
                                    data.description = task.description;
                                    data.dueDate = task.dueDate;
                                    data.updateAt = DateTime.Now;
                                    data.updateBy = (string)Session["BadgeId"];
                                    data.workersId = string.Join(", ", workers);
                                    db.SaveChanges();

                                    List<int> workerIds = new List<int>();
                                    foreach (var person in workers)
                                    {
                                        string badgeId = person.Split('-')[0].Trim();
                                        var user = db.TblUsers.FirstOrDefault(x => x.badgeId == badgeId);
                                        if (user != null)
                                        {
                                            workerIds.Add(user.Id);
                                            var currentSubTask = db.TblTaskByIndividual.FirstOrDefault(x => x.IdTBS == data.Id && x.ownerId == user.Id && x.deleted == false);
                                            if(currentSubTask == null)
                                            {
                                                var subTask = new TblTaskByIndividual();
                                                subTask.IdTBS = task.Id;
                                                subTask.ownerId = user.Id;
                                                subTask.ownerBadgeId = user.badgeId;
                                                subTask.ownerFullName = user.fullName;
                                                subTask.createdAt = DateTime.Now;
                                                subTask.createdBy = (string)Session["BadgeId"];
                                                subTask.currentStatus = "open";
                                                db.TblTaskByIndividual.Add(subTask);
                                            } else
                                            {
                                                currentSubTask.ownerId = user.Id;
                                                currentSubTask.ownerBadgeId = user.badgeId;
                                                currentSubTask.ownerFullName = user.fullName;
                                                currentSubTask.canceled = false;
                                            }
                                            db.SaveChanges();
                                        }
                                    }

                                    var canceledTask = db.TblTaskByIndividual.Where(x => x.IdTBS == data.Id && !workerIds.Contains(x.ownerId) && x.deleted == false).ToList();
                                    if(canceledTask.Count > 0)
                                    {
                                        foreach(var tsk in canceledTask)
                                        {
                                            tsk.canceled = true;
                                            tsk.updateAt = DateTime.Now;
                                            tsk.updateBy = (string)Session["BadgeId"];
                                            db.SaveChanges();
                                        }
                                    }

                                    db.SaveChanges();
                                    transaction.Commit();
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    return Json(new { flag = JsonResponseStandart.error, msg = "Create task failed.\n" + ex.Message, data = ex.ToString() });
                                }
                            }
                        }
                        return Json(new { flag = JsonResponseStandart.success, msg = "Task created.", data = "" });
                    }
                } else
                {
                    return Json(new { flag = JsonResponseStandart.failed, msg = "Create task failed.", data = "" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = ex.ToString() });
            }
        }
        [HttpPost]
        public ActionResult DeleteMainTask(int id)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { flag = JsonResponseStandart.error, msg = "Session expired. Please log in again." });
                }
                int currentUserId = (int)Session["Id"];
                int currentUserLevel = (int)Session["LevelPos"];

                using (var db = new TaskLogEntities())
                {
                    var task = db.TblTaskBySuperior.FirstOrDefault(x => x.Id == id && x.deleted == false);
                    if(task != null)
                    {
                        if (task.ownerId != currentUserId) return Json(new { flag = JsonResponseStandart.error, msg = "This task doesn't belong to you.", data = "" });
                        using (var transaction = db.Database.BeginTransaction())
                        {
                            try
                            {


                                // Parameters to protect against SQL Injection
                                var pId = new SqlParameter("@id", id);
                                var pUser = new SqlParameter("@User", (string)Session["BadgeId"]);

                                // 2. Execute First Query
                                string sqlSuperior = @"
                                UPDATE TblTaskBySuperior 
                                SET deleted = 1, deletedAt = GETDATE(), deletedBy = @User 
                                WHERE Id = @id";

                                db.Database.ExecuteSqlCommand(sqlSuperior, pId, pUser);

                                // Parameters can only be used once per query, so we must clone them for the second query
                                var pId2 = new SqlParameter("@id", id);
                                var pUser2 = new SqlParameter("@User", (string)Session["BadgeId"]);

                                string sqlIndividual = @"
                                UPDATE TblTaskByIndividual 
                                SET deleted = 1, deletedAt = GETDATE(), deletedBy = @User 
                                WHERE IdTBS = @id";

                                db.Database.ExecuteSqlCommand(sqlIndividual, pId2, pUser2);

                                transaction.Commit();

                                return Json(new { flag = JsonResponseStandart.success, msg = "Task deleted successfully!" });
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                return Json(new { flag = JsonResponseStandart.failed, msg = "Failed to delete task. Changes reverted.\n"+ ex.Message, data = ex.ToString() });
                            }
                        }
                    } else
                    {
                        return Json(new { flag = JsonResponseStandart.error, msg = "Task not found.", data = "" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = ex.Message, data = "" });
            }
        }
        [HttpGet]
        public ActionResult GetWorkerList() {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { flag = JsonResponseStandart.error, msg = "Session expired. Please log in again." }, JsonRequestBehavior.AllowGet);
                }
                int currentUserId = (int)Session["Id"];
                int currentUserLevel = (int)Session["LevelPos"];

                using (var db = new TaskLogEntities())
                {
                    var users = db.TblUsers.Where(x => x.levelPosition <= currentUserLevel).ToList();
                    if (users.Count > 0)
                    {
                        return Json(new { flag = JsonResponseStandart.success , msg = "Workers list loaded.", data = users }, JsonRequestBehavior.AllowGet);
                    } else
                    {
                        return Json(new { flag = JsonResponseStandart.error, msg = "No subordinate found.", data = "" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { flag = JsonResponseStandart.error, msg = "Fetch failed.\n" + ex.Message, data = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

        #region unused

        //[HttpGet]
        //public ActionResult MyTask() {

        //    if (Session["Id"] == null)
        //    {
        //        return Json(new { flag = JsonResponseStandart.error, msg = "Session expired. Please log in again." }, JsonRequestBehavior.AllowGet);
        //    }
        //    int currentUserId = (int)Session["Id"];

        //    using (var db = new TaskLogEntities())
        //    {

        //        var user = db.TblUsers.FirstOrDefault(x=> x.Id == currentUserId);
        //        var superiorTask = db.TblTaskBySuperior.Where(x => x.deleted == false && x.ownerId == currentUserId).ToList();
        //        var personalTask = db.TblTaskByIndividual.Where(x => x.deleted == false && x.ownerId == currentUserId).ToList();

        //        //query to database, then stored it into List<Models.TaskView> here
        //        string sqlQuery = @"
        //                SELECT 
        //                    a.Id, a.ownerId, a.ownerFullName, a.title, a.description, a.dueDate, a.status, 
        //                    a.remarks, a.attachment, a.workersId, a.createdAt, a.createdBy,

        //                    b.Id as IdTaskIndividual, b.IdTBS, b.ownerId as ownerIdTaskIndividual, b.ownerFullName ownerFullNameTaskIndividual,
        //                    b.startDate, b.finishDate, b.currentStatus, 
        //                    b.remarks as remarksTaskIndividual, 
        //                    b.remarksWhenCanceled, b.remarksWhenPaused, 
        //                    b.attachment as attachmentTaskIndividual
        //                FROM TblTaskBySuperior a
        //                LEFT JOIN TblTaskByIndividual b ON a.Id = b.IdTBS
        //                WHERE a.deleted = 0 AND (b.deleted = 0 OR b.Id IS NULL)
        //                AND b.assignedWorkerId = @WorkerId";

        //        var sqlParams = new[] {
        //            new SqlParameter("@WorkerId", currentUserId),
        //        };
        //        List<TaskView> taskViews = db.Database.SqlQuery<TaskView>(sqlQuery, currentUserId).ToList();

        //    }
        //    return Json(new { flag = JsonResponseStandart.success, msg = "Success!" }, JsonRequestBehavior.AllowGet);
        //}
        #endregion
    }
}