using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagementSystem.Data;
using StudentGradeManagementSystem.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace StudentGradeManagementSystem.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeacherController(ApplicationDbContext context)
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

        // 显示所有教师信息
        public async Task<IActionResult> Index(string searchString, int? teacherId)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            var teachers = from t in _context.Teachers select t;
            
            // 按姓名搜索
            if (!string.IsNullOrEmpty(searchString))
            {
                teachers = teachers.Where(t => t.TeacherName.Contains(searchString));
            }
            
            // 按教师ID搜索
            if (teacherId.HasValue)
            {
                teachers = teachers.Where(t => t.TeacherID == teacherId.Value);
            }
            
            return View(await teachers.ToListAsync());
        }

        // 显示添加教师页面
        public IActionResult Create()
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        // 处理添加教师请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TeacherID,TeacherName,Department")] Teacher teacher)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(teacher);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "添加教师失败：" + ex.Message);
                }
            }
            return View(teacher);
        }

        // 显示编辑教师页面
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Teachers == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }
            return View(teacher);
        }

        // 处理编辑教师请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TeacherID,TeacherName")] Teacher teacher)
        {
            if (id != teacher.TeacherID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(teacher);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherExists(teacher.TeacherID))
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
            return View(teacher);
        }

        // 显示删除教师页面
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Teachers == null)
            {
                return NotFound();
            }
            var teacher = await _context.Teachers.FirstOrDefaultAsync(m => m.TeacherID == id);
            if (teacher == null)
            {
                return NotFound();
            }
            return View(teacher);
        }

        // 处理删除教师请求
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool deleteRelatedRecords = false)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            if (_context.Teachers == null)
            {
                return Problem("实体集 'ApplicationDbContext.Teachers' 为空。");
            }
            
            // 检查是否有关联的课程记录、成绩记录和用户账户
            var hasCourses = await _context.Courses.AnyAsync(c => c.TeacherID == id);
            var hasGrades = await _context.Grades.AnyAsync(g => g.TeacherID == id);
            var hasUserAccount = await _context.Users.AnyAsync(u => u.TeacherID == id);
            
            // 如果有关联记录且未选择级联删除，则返回错误信息
            if ((hasCourses || hasGrades || hasUserAccount) && !deleteRelatedRecords)
            {
                TempData["ErrorMessage"] = "该教师有关联的课程记录、成绩记录或用户账户，无法直接删除。请先删除相关记录，或勾选\"同时删除关联记录\"选项。";
                return RedirectToAction(nameof(Delete), new { id });
            }
            
            // 如果选择了级联删除，则先删除关联记录
            if (deleteRelatedRecords)
            {
                // 删除成绩记录
                var grades = await _context.Grades.Where(g => g.TeacherID == id).ToListAsync();
                if (grades.Any())
                {
                    _context.Grades.RemoveRange(grades);
                }
                
                // 删除课程记录（将TeacherID设为null，因为课程可以没有教师）
                var courses = await _context.Courses.Where(c => c.TeacherID == id).ToListAsync();
                foreach (var course in courses)
                {
                    course.TeacherID = null;
                }
                
                // 删除关联的用户账户
                var userAccount = await _context.Users.FirstOrDefaultAsync(u => u.TeacherID == id);
                if (userAccount != null)
                {
                    _context.Users.Remove(userAccount);
                }
            }
            
            // 删除教师记录
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher != null)
            {
                _context.Teachers.Remove(teacher);
            }

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // 捕获可能的数据库异常
                TempData["ErrorMessage"] = $"删除教师失败: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
        private bool TeacherExists(int id)
        {
            return (_context.Teachers?.Any(e => e.TeacherID == id)).GetValueOrDefault();
        }
    }
}