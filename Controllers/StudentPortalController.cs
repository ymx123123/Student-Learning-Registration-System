using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagementSystem.Data;
using StudentGradeManagementSystem.Models;
using StudentGradeManagementSystem.Services;
using System.Linq;
using System.Threading.Tasks;

namespace StudentGradeManagementSystem.Controllers
{
    public class StudentPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GpaService _gpaService;

        public StudentPortalController(ApplicationDbContext context, GpaService gpaService)
        {
            _context = context;
            _gpaService = gpaService;
        }

        // 检查学生登录状态
        private bool IsStudentLoggedIn()
        {
            return HttpContext.Session.GetString("IsLoggedIn") == "true" && 
                   HttpContext.Session.GetString("UserRole") == "学生";
        }

        // 获取当前登录学生ID
        private async Task<int?> GetCurrentStudentId()
        {
            if (!IsStudentLoggedIn()) return null;
            
            var username = HttpContext.Session.GetString("Username");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user?.StudentID;
        }

        // 学生端主页
        public async Task<IActionResult> Index()
        {
            if (!IsStudentLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }

            var studentId = await GetCurrentStudentId();
            if (studentId == null)
            {
                TempData["ErrorMessage"] = "无法获取学生信息！";
                return RedirectToAction("Index", "Login");
            }

            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.StudentID == studentId);

            if (student == null)
            {
                TempData["ErrorMessage"] = "学生信息不存在！";
                return RedirectToAction("Index", "Login");
            }

            ViewBag.StudentName = student.StudentName;
            ViewBag.ClassName = student.Class?.ClassName ?? "未分配班级";
            
            return View();
        }

        // 选课页面
        public async Task<IActionResult> CourseSelection()
        {
            if (!IsStudentLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }

            var studentId = await GetCurrentStudentId();
            if (studentId == null)
            {
                TempData["ErrorMessage"] = "无法获取学生信息！";
                return RedirectToAction("Index");
            }

            // 获取所有已分配教师的可选课程
            var allCourses = await _context.Courses
                .Include(c => c.Teacher)
                .Where(c => c.TeacherID != null)
                .ToListAsync();

            // 获取学生已选课程
            var selectedCourses = await _context.StudentCourses
                .Where(sc => sc.StudentID == studentId)
                .Include(sc => sc.Course)
                .ThenInclude(c => c.Teacher)
                .ToListAsync();

            // 获取学生已选课程的课程名称（用于检查同名课程）
            var selectedCourseNames = selectedCourses.Select(sc => sc.Course.CourseName).ToList();

            ViewBag.AllCourses = allCourses;
            ViewBag.SelectedCourses = selectedCourses;
            ViewBag.SelectedCourseNames = selectedCourseNames;
            
            return View();
        }

        // 选课操作
        [HttpPost]
        public async Task<IActionResult> SelectCourse(int courseId)
        {
            if (!IsStudentLoggedIn())
            {
                return Json(new { success = false, message = "请先登录！" });
            }

            var studentId = await GetCurrentStudentId();
            if (studentId == null)
            {
                return Json(new { success = false, message = "无法获取学生信息！" });
            }

            try
            {
                // 检查课程是否存在且已分配教师
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    return Json(new { success = false, message = "课程不存在！" });
                }
                
                if (course.TeacherID == null)
                {
                    return Json(new { success = false, message = "该课程尚未分配教师，无法选择！" });
                }

                // 检查是否已经选择了同名课程
                var existingEnrollment = await _context.StudentCourses
                    .Include(sc => sc.Course)
                    .Where(sc => sc.StudentID == studentId && sc.Course.CourseName == course.CourseName)
                    .FirstOrDefaultAsync();

                if (existingEnrollment != null)
                {
                    return Json(new { success = false, message = $"您已经选择了课程《{course.CourseName}》，不能重复选择！" });
                }

                // 创建选课记录
                var studentCourse = new StudentCourse
                {
                    StudentID = studentId.Value,
                    CourseID = courseId
                };

                _context.StudentCourses.Add(studentCourse);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "选课成功！" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "选课失败：" + ex.Message });
            }
        }

        // 退选课程
        [HttpPost]
        public async Task<IActionResult> DropCourse(int enrollmentId)
        {
            if (!IsStudentLoggedIn())
            {
                return Json(new { success = false, message = "请先登录！" });
            }

            var studentId = await GetCurrentStudentId();
            if (studentId == null)
            {
                return Json(new { success = false, message = "无法获取学生信息！" });
            }

            try
            {
                var enrollment = await _context.StudentCourses
                    .FirstOrDefaultAsync(sc => sc.EnrollmentID == enrollmentId && sc.StudentID == studentId);

                if (enrollment == null)
                {
                    return Json(new { success = false, message = "选课记录不存在！" });
                }

                _context.StudentCourses.Remove(enrollment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "退选成功！" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "退选失败：" + ex.Message });
            }
        }

        // 查看成绩页面
        public async Task<IActionResult> ViewGrades()
        {
            if (!IsStudentLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }

            var studentId = await GetCurrentStudentId();
            if (studentId == null)
            {
                TempData["ErrorMessage"] = "无法获取学生信息！";
                return RedirectToAction("Index");
            }

            // 获取学生的所有成绩
            var grades = await _context.Grades
                .Where(g => g.StudentID == studentId)
                .Include(g => g.Course)
                .Include(g => g.Teacher)
                .OrderBy(g => g.Course.CourseName)
                .ToListAsync();

            // 获取学生信息
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.StudentID == studentId);

            // 获取学生的排名信息
            var (schoolRank, totalStudents, gpa) = await _gpaService.GetStudentGpaRankingAsync(studentId.Value);
            
            // 获取班级排名信息
            var classRankings = await _gpaService.GetClassGpaRankingAsync(student?.ClassID);
            var studentClassRanking = classRankings.FirstOrDefault(r => r.StudentId == studentId);
            
            // 创建排名信息视图模型
            var rankingInfo = new StudentGpaRankingViewModel
            {
                StudentId = studentId.Value,
                StudentName = student?.StudentName ?? "",
                ClassName = student?.Class?.ClassName ?? "未分班",
                Gpa = gpa,
                AverageGpa = gpa,
                SchoolRank = schoolRank,
                TotalStudents = totalStudents,
                ClassRank = studentClassRanking?.Rank ?? 0,
                ClassStudents = classRankings.Count,
                TotalCredits = grades.Sum(g => g.Course?.Credit ?? 0),
                PassedCourses = grades.Count(g => g.GradeValue >= 60),
                TotalCourses = grades.Count
            };
            
            ViewBag.RankingInfo = rankingInfo;

            return View(grades);
        }

        // 修改密码页面
        public IActionResult ChangePassword()
        {
            if (!IsStudentLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        // 修改密码处理
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (!IsStudentLoggedIn())
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