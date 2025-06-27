using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using StudentGradeManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace StudentGradeManagementSystem.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 如果已经登录，根据角色跳转到对应页面
            if (HttpContext.Session.GetString("IsLoggedIn") == "true")
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                switch (userRole)
                {
                    case "管理员":
                        return RedirectToAction("Index", "Home");
                    case "学生":
                        return RedirectToAction("Index", "StudentPortal");
                    case "老师":
                        return RedirectToAction("Index", "TeacherPortal");
                    default:
                        return RedirectToAction("Index", "Home");
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // 从数据库验证用户名和密码
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                // 登录成功，设置Session
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetInt32("UserID", user.UserID);
                
                // 根据用户角色设置姓名到Session
                if (user.Role == "学生" && user.Student != null)
                {
                    HttpContext.Session.SetString("StudentName", user.Student.StudentName);
                }
                else if (user.Role == "老师" && user.Teacher != null)
                {
                    HttpContext.Session.SetString("TeacherName", user.Teacher.TeacherName);
                }
                
                // 根据用户角色跳转到不同页面
                switch (user.Role)
                {
                    case "管理员":
                        return RedirectToAction("Index", "Home");
                    case "学生":
                        return RedirectToAction("Index", "StudentPortal");
                    case "老师":
                        return RedirectToAction("Index", "TeacherPortal");
                    default:
                        return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                // 登录失败
                ViewBag.ErrorMessage = "用户名或密码错误！";
                return View("Index");
            }
        }

        public IActionResult Logout()
        {
            // 清除Session
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }

        // 修改密码页面
        public IActionResult ChangePassword()
        {
            // 检查是否已登录
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        // 修改密码处理
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            // 检查是否已登录
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
            {
                return RedirectToAction("Index", "Login");
            }

            // 验证输入
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "所有字段都不能为空！";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "新密码和确认密码不一致！";
                return View();
            }

            if (newPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "新密码长度不能少于6位！";
                return View();
            }

            try
            {
                // 获取当前用户
                var currentUsername = HttpContext.Session.GetString("Username");
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "用户不存在！";
                    return View();
                }

                // 验证当前密码
                if (user.Password != currentPassword)
                {
                    TempData["ErrorMessage"] = "当前密码错误！";
                    return View();
                }

                // 更新密码
                user.Password = newPassword;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "密码修改成功！";
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "修改密码时发生错误：" + ex.Message;
                return View();
            }
        }
    }
}