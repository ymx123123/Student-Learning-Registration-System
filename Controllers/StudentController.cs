using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagementSystem.Data;
using StudentGradeManagementSystem.Models;
using Microsoft.AspNetCore.Http;

namespace StudentGradeManagementSystem.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
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

        // 显示所有学生信息
        public async Task<IActionResult> Index(string searchString, int? classId, int? studentId)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            var students = from s in _context.Students.Include(s => s.Class) select s;
            
            // 按姓名搜索
            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.StudentName.Contains(searchString));
            }
            
            // 按学生ID搜索
            if (studentId.HasValue)
            {
                students = students.Where(s => s.StudentID == studentId.Value);
            }
            
            // 按班级搜索
            if (classId.HasValue && classId.Value > 0)
            {
                students = students.Where(s => s.ClassID == classId.Value);
            }
            
            // 传递班级列表到视图
            ViewBag.Classes = await _context.Classes.ToListAsync();
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedStudentId = studentId;
            
            return View(await students.ToListAsync());
        }

        // 显示添加学生页面
        public IActionResult Create()
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Classes = _context.Classes.ToList();
            return View();
        }

        // 处理添加学生请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentID,StudentName,Gender,BirthDate,ClassID")] Student student)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Classes = _context.Classes.ToList();
            return View(student);
        }

        // 显示编辑学生页面
        public async Task<IActionResult> Edit(int? id)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            if (id == null || _context.Students == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            ViewBag.Classes = _context.Classes.ToList();
            return View(student);
        }

        // 处理编辑学生请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudentID,StudentName,Gender,BirthDate,ClassID")] Student student)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            if (id != student.StudentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentID))
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
            ViewBag.Classes = _context.Classes.ToList();
            return View(student);
        }

        // 显示删除学生确认页面
        public async Task<IActionResult> Delete(int? id)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            if (id == null || _context.Students == null)
            {
                return NotFound();
            }

            var student = await _context.Students
               .FirstOrDefaultAsync(m => m.StudentID == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // 处理删除学生请求
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool deleteRelatedRecords = false)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            if (_context.Students == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Students'  is null.");
            }
            
            // 检查是否有关联的成绩记录、选课记录和用户账户
            var hasGrades = await _context.Grades.AnyAsync(g => g.StudentID == id);
            var hasEnrollments = await _context.StudentCourses.AnyAsync(sc => sc.StudentID == id);
            var hasUserAccount = await _context.Users.AnyAsync(u => u.StudentID == id);
            
            // 如果有关联记录且未选择级联删除，则返回错误信息
            if ((hasGrades || hasEnrollments || hasUserAccount) && !deleteRelatedRecords)
            {
                TempData["ErrorMessage"] = "该学生有关联的成绩记录、选课记录或用户账户，无法直接删除。请先删除相关记录，或勾选\"同时删除关联记录\"选项。";
                return RedirectToAction(nameof(Delete), new { id });
            }
            
            // 如果选择了级联删除，则先删除关联记录
            if (deleteRelatedRecords)
            {
                // 删除成绩记录
                var grades = await _context.Grades.Where(g => g.StudentID == id).ToListAsync();
                if (grades.Any())
                {
                    _context.Grades.RemoveRange(grades);
                }
                
                // 删除选课记录
                var enrollments = await _context.StudentCourses.Where(sc => sc.StudentID == id).ToListAsync();
                if (enrollments.Any())
                {
                    _context.StudentCourses.RemoveRange(enrollments);
                }
                
                // 删除关联的用户账户
                var userAccount = await _context.Users.FirstOrDefaultAsync(u => u.StudentID == id);
                if (userAccount != null)
                {
                    _context.Users.Remove(userAccount);
                }
            }
            
            // 删除学生记录
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
            }

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // 捕获可能的数据库异常
                TempData["ErrorMessage"] = $"删除学生失败: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentID == id);
        }
    }
}