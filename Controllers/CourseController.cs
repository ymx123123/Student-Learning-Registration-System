using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagementSystem.Data;
using StudentGradeManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;

namespace StudentGradeManagementSystem.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseController(ApplicationDbContext context)
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

        // 显示所有课程信息
        public async Task<IActionResult> Index(string searchString, int? teacherId)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            var courses = _context.Courses.Include(c => c.Teacher).AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(c => c.CourseName.Contains(searchString));
            }
            if (teacherId.HasValue)
            {
                courses = courses.Where(c => c.TeacherID == teacherId.Value);
            }
            
            // 为教师搜索下拉框准备数据
            var teachers = await _context.Teachers.ToListAsync();
            var teacherSelectList = teachers.Select(t => new SelectListItem
            {
                Value = t.TeacherID.ToString(),
                Text = $"{t.TeacherID} - {t.TeacherName}",
                Selected = teacherId.HasValue && teacherId.Value == t.TeacherID
            }).ToList();
            teacherSelectList.Insert(0, new SelectListItem { Value = "", Text = "请选择教师", Selected = !teacherId.HasValue });
            ViewBag.TeacherSelectList = teacherSelectList;
            ViewBag.SelectedTeacherId = teacherId;
            
            return View(await courses.ToListAsync());
        }

        // 显示添加课程页面
        public IActionResult Create()
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            var teachers = _context.Teachers.ToList();
            var teacherSelectList = teachers.Select(t => new SelectListItem
            {
                Value = t.TeacherID.ToString(),
                Text = $"{t.TeacherID} - {t.TeacherName}"
            }).ToList();
            teacherSelectList.Insert(0, new SelectListItem { Value = "", Text = "不分配教师" });
            ViewBag.TeacherList = teacherSelectList;
            return View();
        }

        // 处理添加课程请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseID,CourseName,Credit,TeacherID")] Course course)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            if (ModelState.IsValid)
            {
                // 检查同一教师是否已有相同名称的课程
                if (course.TeacherID.HasValue)
                {
                    var existingCourseForTeacher = await _context.Courses
                        .FirstOrDefaultAsync(c => c.CourseName == course.CourseName && c.TeacherID == course.TeacherID);
                    if (existingCourseForTeacher != null)
                    {
                        ModelState.AddModelError("CourseName", "该教师已被分配了同名课程，无法重复分配。");
                        var teachers1 = _context.Teachers.ToList();
                        var teacherSelectList1 = teachers1.Select(t => new SelectListItem
                        {
                            Value = t.TeacherID.ToString(),
                            Text = $"{t.TeacherID} - {t.TeacherName}"
                        }).ToList();
                        teacherSelectList1.Insert(0, new SelectListItem { Value = "", Text = "不分配教师" });
                        ViewBag.TeacherList = teacherSelectList1;
                        return View(course);
                    }
                }

                // 检查同名课程的学分是否一致
                var existingCourseWithSameName = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseName == course.CourseName);
                if (existingCourseWithSameName != null && existingCourseWithSameName.Credit != course.Credit)
                {
                    ModelState.AddModelError("Credit", $"同名课程'{course.CourseName}'的学分必须保持一致，现有学分为{existingCourseWithSameName.Credit}。");
                    var teachers2 = _context.Teachers.ToList();
                    var teacherSelectList2 = teachers2.Select(t => new SelectListItem
                    {
                        Value = t.TeacherID.ToString(),
                        Text = $"{t.TeacherID} - {t.TeacherName}"
                    }).ToList();
                    teacherSelectList2.Insert(0, new SelectListItem { Value = "", Text = "不分配教师" });
                    ViewBag.TeacherList = teacherSelectList2;
                    return View(course);
                }

                try
                {
                    _context.Add(course);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "添加课程失败：" + ex.Message);
                }
            }
            
            var teachers3 = _context.Teachers.ToList();
            var teacherSelectList3 = teachers3.Select(t => new SelectListItem
            {
                Value = t.TeacherID.ToString(),
                Text = $"{t.TeacherID} - {t.TeacherName}"
            }).ToList();
            teacherSelectList3.Insert(0, new SelectListItem { Value = "", Text = "不分配教师" });
            ViewBag.TeacherList = teacherSelectList3;
            return View(course);
        }

        // 显示编辑课程页面
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Courses == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            var teachers4 = _context.Teachers.ToList();
            var teacherSelectList4 = teachers4.Select(t => new SelectListItem
            {
                Value = t.TeacherID.ToString(),
                Text = $"{t.TeacherID} - {t.TeacherName}"
            }).ToList();
            teacherSelectList4.Insert(0, new SelectListItem { Value = "", Text = "不分配教师" });
            ViewBag.TeacherList = teacherSelectList4;
            return View(course);
        }

        // 处理编辑课程请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CourseID,CourseName,Credit,TeacherID")] Course course)
        {
            if (id != course.CourseID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.CourseID))
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
            var teachers5 = _context.Teachers.ToList();
            var teacherSelectList5 = teachers5.Select(t => new SelectListItem
            {
                Value = t.TeacherID.ToString(),
                Text = $"{t.TeacherID} - {t.TeacherName}"
            }).ToList();
            teacherSelectList5.Insert(0, new SelectListItem { Value = "", Text = "不分配教师" });
            ViewBag.TeacherList = teacherSelectList5;
            return View(course);
        }

        // 显示删除课程页面
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Courses == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.CourseID == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // 处理删除课程请求
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool deleteRelatedRecords = false)
        {
            if (_context.Courses == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Courses'  is null.");
            }
            
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            
            try
            {
                // 检查是否有关联的选课记录
                var relatedStudentCourses = await _context.StudentCourses
                    .Where(sc => sc.CourseID == id)
                    .ToListAsync();
                
                // 检查是否有关联的成绩记录
                var relatedGrades = await _context.Grades
                    .Where(g => g.CourseID == id)
                    .ToListAsync();
                
                bool hasRelatedRecords = relatedStudentCourses.Any() || relatedGrades.Any();
                
                if (hasRelatedRecords && !deleteRelatedRecords)
                {
                    // 构建详细的错误信息
                    var errorMessages = new List<string>();
                    if (relatedStudentCourses.Any())
                    {
                        errorMessages.Add($"{relatedStudentCourses.Count} 条选课记录");
                    }
                    if (relatedGrades.Any())
                    {
                        errorMessages.Add($"{relatedGrades.Count} 条成绩记录");
                    }
                    
                    TempData["ErrorMessage"] = $"无法删除该课程，因为存在关联的 {string.Join("、", errorMessages)}。请勾选\"同时删除关联记录\"选项或先删除相关记录。";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }
                
                // 如果选择级联删除，先删除关联记录
                if (deleteRelatedRecords)
                {
                    // 删除关联的成绩记录
                    if (relatedGrades.Any())
                    {
                        _context.Grades.RemoveRange(relatedGrades);
                    }
                    
                    // 删除关联的选课记录
                    if (relatedStudentCourses.Any())
                    {
                        _context.StudentCourses.RemoveRange(relatedStudentCourses);
                    }
                }
                
                // 删除课程
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "课程删除成功！";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"删除课程时发生错误：{ex.Message}";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }

        private bool CourseExists(int id)
        {
            return (_context.Courses?.Any(e => e.CourseID == id)).GetValueOrDefault();
        }
    }
}
