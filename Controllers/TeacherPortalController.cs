using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagementSystem.Data;
using StudentGradeManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace StudentGradeManagementSystem.Controllers
{
    public class TeacherPortalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeacherPortalController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 检查教师登录状态
        private bool IsTeacherLoggedIn()
        {
            return HttpContext.Session.GetString("IsLoggedIn") == "true" && 
                   HttpContext.Session.GetString("UserRole") == "老师";
        }

        // 获取当前登录教师ID
        private async Task<int?> GetCurrentTeacherId()
        {
            if (!IsTeacherLoggedIn()) return null;
            
            var username = HttpContext.Session.GetString("Username");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user?.TeacherID;
        }

        // 教师端主页
        public async Task<IActionResult> Index()
        {
            if (!IsTeacherLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }

            var teacherId = await GetCurrentTeacherId();
            if (teacherId == null)
            {
                TempData["ErrorMessage"] = "无法获取教师信息！";
                return RedirectToAction("Index", "Login");
            }

            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "教师信息不存在！";
                return RedirectToAction("Index", "Login");
            }

            ViewBag.TeacherName = teacher.TeacherName;
            
            return View();
        }

        // 查看教授课程和学生
        public async Task<IActionResult> MyCourses()
        {
            if (!IsTeacherLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }

            var teacherId = await GetCurrentTeacherId();
            if (teacherId == null)
            {
                TempData["ErrorMessage"] = "无法获取教师信息！";
                return RedirectToAction("Index");
            }

            // 获取教师教授的所有课程及选课学生
            var courses = await _context.Courses
                .Where(c => c.TeacherID == teacherId)
                .Include(c => c.StudentCourses)
                .ThenInclude(sc => sc.Student)
                .ThenInclude(s => s.Class)
                .ToListAsync();

            return View(courses);
        }

        // 成绩管理页面
        public async Task<IActionResult> GradeManagement()
        {
            if (!IsTeacherLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }

            var teacherId = await GetCurrentTeacherId();
            if (teacherId == null)
            {
                TempData["ErrorMessage"] = "无法获取教师信息！";
                return RedirectToAction("Index");
            }

            // 获取教师教授的课程
            var courses = await _context.Courses
                .Where(c => c.TeacherID == teacherId)
                .ToListAsync();

            ViewBag.Courses = courses;
            
            return View();
        }

        // 获取指定课程的学生列表（AJAX）
        [HttpGet]
        public async Task<IActionResult> GetCourseStudents(int courseId)
        {
            if (!IsTeacherLoggedIn())
            {
                return Json(new { success = false, message = "请先登录！" });
            }

            var teacherId = await GetCurrentTeacherId();
            if (teacherId == null)
            {
                return Json(new { success = false, message = "无法获取教师信息！" });
            }

            try
            {
                // 调试日志：输出查询参数
                Console.WriteLine($"[DEBUG] GetCourseStudents - CourseID: {courseId}, TeacherID: {teacherId}");
                
                // 验证课程是否属于当前教师
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TeacherID == teacherId);

                if (course == null)
                {
                    Console.WriteLine($"[DEBUG] Course not found or no permission - CourseID: {courseId}, TeacherID: {teacherId}");
                    return Json(new { success = false, message = "课程不存在或您没有权限访问！" });
                }

                Console.WriteLine($"[DEBUG] Course found - CourseName: {course.CourseName}");

                // 获取选课学生及其成绩
                var students = await _context.StudentCourses
                    .Where(sc => sc.CourseID == courseId)
                    .Include(sc => sc.Student)
                    .ThenInclude(s => s.Class)
                    .Select(sc => new
                    {
                        StudentID = sc.Student.StudentID,
                        StudentName = sc.Student.StudentName,
                        ClassName = sc.Student.Class.ClassName,
                        HasGrade = _context.Grades.Any(g => g.StudentID == sc.Student.StudentID && g.CourseID == courseId && g.TeacherID == teacherId)
                    })
                    .ToListAsync();

                // 调试日志：输出查询结果
                Console.WriteLine($"[DEBUG] Found {students.Count} students for course {courseId}");
                foreach (var student in students)
                {
                    Console.WriteLine($"[DEBUG] Student - ID: {student.StudentID}, Name: {student.StudentName}, Class: {student.ClassName}, HasGrade: {student.HasGrade}");
                }

                return Json(new { success = true, students = students });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "获取学生列表失败：" + ex.Message });
            }
        }

        // 录入成绩
        [HttpPost]
        public async Task<IActionResult> AddGrade(int studentId, int courseId, decimal gradeValue)
        {
            if (!IsTeacherLoggedIn())
            {
                return Json(new { success = false, message = "请先登录！" });
            }

            var teacherId = await GetCurrentTeacherId();
            if (teacherId == null)
            {
                return Json(new { success = false, message = "无法获取教师信息！" });
            }

            try
            {
                // 验证课程是否属于当前教师
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TeacherID == teacherId);

                if (course == null)
                {
                    return Json(new { success = false, message = "课程不存在或您没有权限访问！" });
                }

                // 验证学生是否选择了该课程
                var enrollment = await _context.StudentCourses
                    .FirstOrDefaultAsync(sc => sc.StudentID == studentId && sc.CourseID == courseId);

                if (enrollment == null)
                {
                    return Json(new { success = false, message = "该学生未选择此课程！" });
                }

                // 检查是否已经有成绩记录
                var existingGrade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.StudentID == studentId && g.CourseID == courseId && g.TeacherID == teacherId);

                if (existingGrade != null)
                {
                    return Json(new { success = false, message = "该学生已有成绩记录，不能重复录入！" });
                }

                // 验证成绩范围
                if (gradeValue < 0 || gradeValue > 100)
                {
                    return Json(new { success = false, message = "成绩必须在0-100之间！" });
                }

                // 创建成绩记录
                var grade = new Grade
                {
                    StudentID = studentId,
                    CourseID = courseId,
                    TeacherID = teacherId.Value,
                    GradeValue = gradeValue
                };

                _context.Grades.Add(grade);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "成绩录入成功！" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "录入成绩失败：" + ex.Message });
            }
        }

        // 查看已录入的成绩
        public async Task<IActionResult> ViewGrades()
        {
            if (!IsTeacherLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }

            var teacherId = await GetCurrentTeacherId();
            if (teacherId == null)
            {
                TempData["ErrorMessage"] = "无法获取教师信息！";
                return RedirectToAction("Index");
            }

            // 获取教师录入的所有成绩
            var grades = await _context.Grades
                .Where(g => g.TeacherID == teacherId)
                .Include(g => g.Student)
                .ThenInclude(s => s.Class)
                .Include(g => g.Course)
                .OrderBy(g => g.Course.CourseName)
                .ThenBy(g => g.Student.StudentName)
                .ToListAsync();

            return View(grades);
        }

        // 修改密码页面
        public IActionResult ChangePassword()
        {
            if (!IsTeacherLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        // 修改密码处理
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (!IsTeacherLoggedIn())
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

        // 退出登录
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}