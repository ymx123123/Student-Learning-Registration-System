using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagementSystem.Data;
using StudentGradeManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace StudentGradeManagementSystem.Controllers
{
    public class GradeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GradeController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool CheckLogin()
        {
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
            {
                return false;
            }
            return true;
        }

        // 显示所有成绩信息
        public async Task<IActionResult> Index(int? studentId, int? courseId)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            var grades = _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Course)
                .Include(g => g.Teacher)
                .AsQueryable();
            if (studentId.HasValue)
            {
                grades = grades.Where(g => g.StudentID == studentId.Value);
            }
            if (courseId.HasValue)
            {
                grades = grades.Where(g => g.CourseID == courseId.Value);
            }
            return View(await grades.ToListAsync());
        }

        // 显示添加成绩页面
        public IActionResult Create(int? studentId)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            
            ViewBag.StudentId = studentId;
            ViewBag.AvailableCourses = new List<object>();
            ViewBag.StudentName = "";
            
            if (studentId.HasValue)
            {
                // 获取学生信息
                var student = _context.Students.FirstOrDefault(s => s.StudentID == studentId.Value);
                if (student != null)
                {
                    ViewBag.StudentName = student.StudentName;
                    
                    // 获取该学生选修的课程
                    var studentCourses = _context.StudentCourses
                        .Include(sc => sc.Course)
                        .ThenInclude(c => c.Teacher)
                        .Where(sc => sc.StudentID == studentId.Value)
                        .ToList();
                    
                    // 获取该学生已有成绩的课程ID
                    var existingGradeCourseIds = _context.Grades
                        .Where(g => g.StudentID == studentId.Value)
                        .Select(g => g.CourseID)
                        .ToList();
                    
                    // 筛选出还没有成绩的课程
                    var availableCourses = studentCourses
                        .Where(sc => !existingGradeCourseIds.Contains(sc.CourseID))
                        .Select(sc => new {
                            CourseID = sc.CourseID,
                            CourseName = sc.Course.CourseName,
                            TeacherID = sc.Course.TeacherID,
                            TeacherName = sc.Course.Teacher.TeacherName
                        })
                        .ToList();
                    
                    ViewBag.AvailableCourses = availableCourses.Cast<dynamic>().ToList();
                }
            }
            
            return View();
        }

        // 处理添加成绩请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int studentId, int courseId, int teacherId, decimal gradeValue)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            
            // 检查该学生该课程是否已有成绩
            var existingGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.StudentID == studentId && g.CourseID == courseId);
            
            if (existingGrade != null)
            {
                ModelState.AddModelError("", "该学生该课程已有成绩记录");
                return RedirectToAction("Create", new { studentId = studentId });
            }
            
            // 验证成绩范围
            if (gradeValue < 0 || gradeValue > 100)
            {
                ModelState.AddModelError("", "成绩必须在0-100之间");
                return RedirectToAction("Create", new { studentId = studentId });
            }
            
            var grade = new Grade
            {
                StudentID = studentId,
                CourseID = courseId,
                TeacherID = teacherId,
                GradeValue = gradeValue
            };
            
            _context.Add(grade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // 显示编辑成绩页面
        public async Task<IActionResult> Edit(int? id)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            
            if (id == null || _context.Grades == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Course)
                .Include(g => g.Teacher)
                .FirstOrDefaultAsync(g => g.GradeID == id);
                
            if (grade == null)
            {
                return NotFound();
            }
            
            return View(grade);
        }

        // 处理编辑成绩请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal gradeValue)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            
            // 验证成绩范围
            if (gradeValue < 0 || gradeValue > 100)
            {
                ModelState.AddModelError("", "成绩必须在0-100之间");
                return RedirectToAction("Edit", new { id = id });
            }
            
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null)
            {
                return NotFound();
            }
            
            // 只更新成绩值
            grade.GradeValue = gradeValue;
            
            try
            {
                _context.Update(grade);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GradeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            
            return RedirectToAction(nameof(Index));
        }

        // 显示删除成绩页面
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Grades == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .FirstOrDefaultAsync(m => m.GradeID == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // 处理删除成绩请求
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Grades == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Grades'  is null.");
            }
            
            var grade = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Course)
                .FirstOrDefaultAsync(g => g.GradeID == id);
                
            if (grade == null)
            {
                return NotFound();
            }
            
            try
            {
                _context.Grades.Remove(grade);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "成绩记录删除成功！";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"删除成绩记录时发生错误：{ex.Message}";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }

        private bool GradeExists(int id)
        {
            return (_context.Grades?.Any(e => e.GradeID == id)).GetValueOrDefault();
        }
    }
}