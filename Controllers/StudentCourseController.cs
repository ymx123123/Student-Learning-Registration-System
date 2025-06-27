using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagementSystem.Data;
using StudentGradeManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;

namespace StudentGradeManagementSystem.Controllers
{
    public class StudentCourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentCourseController(ApplicationDbContext context)
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

        // 显示所有选课信息
        public async Task<IActionResult> Index(int? studentId, int? courseId)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            var studentCourses = _context.StudentCourses
                .Include(sc => sc.Student)
                .Include(sc => sc.Course)
                .Include(sc => sc.Course.Teacher)
                .AsQueryable();

            if (studentId.HasValue)
            {
                studentCourses = studentCourses.Where(sc => sc.StudentID == studentId.Value);
            }
            if (courseId.HasValue)
            {
                studentCourses = studentCourses.Where(sc => sc.CourseID == courseId.Value);
            }
            
            // 为学生搜索下拉框准备数据
            var students = await _context.Students.ToListAsync();
            var studentSelectList = students.Select(s => new SelectListItem
            {
                Value = s.StudentID.ToString(),
                Text = $"{s.StudentID} - {s.StudentName}",
                Selected = studentId.HasValue && studentId.Value == s.StudentID
            }).ToList();
            studentSelectList.Insert(0, new SelectListItem { Value = "", Text = "请选择学生", Selected = !studentId.HasValue });
            ViewBag.StudentSelectList = studentSelectList;
            ViewBag.SelectedStudentId = studentId;
            
            // 为课程搜索下拉框准备数据
            var courses = await _context.Courses.ToListAsync();
            var courseSelectList = courses.Select(c => new SelectListItem
            {
                Value = c.CourseID.ToString(),
                Text = $"{c.CourseID} - {c.CourseName}",
                Selected = courseId.HasValue && courseId.Value == c.CourseID
            }).ToList();
            courseSelectList.Insert(0, new SelectListItem { Value = "", Text = "请选择课程", Selected = !courseId.HasValue });
            ViewBag.CourseSelectList = courseSelectList;
            ViewBag.SelectedCourseId = courseId;
            
            return View(await studentCourses.ToListAsync());
        }

        // 显示添加选课页面
        public IActionResult Create()
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            
            // 准备学生下拉框数据
            var students = _context.Students.ToList();
            var studentSelectList = students.Select(s => new SelectListItem
            {
                Value = s.StudentID.ToString(),
                Text = $"{s.StudentID} - {s.StudentName}"
            }).ToList();
            ViewBag.StudentList = new SelectList(studentSelectList, "Value", "Text");
            
            // 准备课程下拉框数据
            var courses = _context.Courses.Include(c => c.Teacher).ToList();
            var courseSelectList = courses.Select(c => new SelectListItem
            {
                Value = c.CourseID.ToString(),
                Text = $"{c.CourseID} - {c.CourseName} ({(c.Teacher != null ? c.Teacher.TeacherName : "未分配教师")})"
            }).ToList();
            ViewBag.CourseList = new SelectList(courseSelectList, "Value", "Text");
            
            return View();
        }

        // 处理添加选课请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EnrollmentID,StudentID,CourseID")] StudentCourse studentCourse)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            
            // 检查是否已存在相同的选课记录
            var existingEnrollment = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentID == studentCourse.StudentID && sc.CourseID == studentCourse.CourseID);
                
            if (existingEnrollment != null)
            {
                ModelState.AddModelError("", "该学生已选择此课程，不能重复选课");
            }
            
            // 检查所选课程是否有指定教师
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.CourseID == studentCourse.CourseID);
                
            if (course != null && course.Teacher == null)
            {
                ModelState.AddModelError("", "所选课程未指定教师，不能选择此课程");
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(studentCourse);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "添加选课记录失败：" + ex.Message);
                }
            }
            
            // 如果模型验证失败，重新准备下拉框数据
            var students = _context.Students.ToList();
            var studentSelectList = students.Select(s => new SelectListItem
            {
                Value = s.StudentID.ToString(),
                Text = $"{s.StudentID} - {s.StudentName}"
            }).ToList();
            ViewBag.StudentList = new SelectList(studentSelectList, "Value", "Text", studentCourse.StudentID);
            
            var courses = _context.Courses.Include(c => c.Teacher).ToList();
            var courseSelectList = courses.Select(c => new SelectListItem
            {
                Value = c.CourseID.ToString(),
                Text = $"{c.CourseID} - {c.CourseName} ({(c.Teacher != null ? c.Teacher.TeacherName : "未分配教师")})"
            }).ToList();
            ViewBag.CourseList = new SelectList(courseSelectList, "Value", "Text", studentCourse.CourseID);
            
            return View(studentCourse);
        }

        // 显示编辑选课页面
        public async Task<IActionResult> Edit(int? id)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            
            if (id == null || _context.StudentCourses == null)
            {
                return NotFound();
            }

            var studentCourse = await _context.StudentCourses.FindAsync(id);
            if (studentCourse == null)
            {
                return NotFound();
            }
            
            // 准备学生下拉框数据
            var students = _context.Students.ToList();
            var studentSelectList = students.Select(s => new SelectListItem
            {
                Value = s.StudentID.ToString(),
                Text = $"{s.StudentID} - {s.StudentName}"
            }).ToList();
            ViewBag.StudentList = new SelectList(studentSelectList, "Value", "Text", studentCourse.StudentID);
            
            // 准备课程下拉框数据
            var courses = _context.Courses.Include(c => c.Teacher).ToList();
            var courseSelectList = courses.Select(c => new SelectListItem
            {
                Value = c.CourseID.ToString(),
                Text = $"{c.CourseID} - {c.CourseName} ({(c.Teacher != null ? c.Teacher.TeacherName : "未分配教师")})"
            }).ToList();
            ViewBag.CourseList = new SelectList(courseSelectList, "Value", "Text", studentCourse.CourseID);
            
            return View(studentCourse);
        }

        // 处理编辑选课请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EnrollmentID,StudentID,CourseID")] StudentCourse studentCourse)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            
            if (id != studentCourse.EnrollmentID)
            {
                return NotFound();
            }
            
            // 检查是否已存在相同的选课记录（排除当前记录）
            var existingEnrollment = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentID == studentCourse.StudentID && 
                                    sc.CourseID == studentCourse.CourseID && 
                                    sc.EnrollmentID != studentCourse.EnrollmentID);
                
            if (existingEnrollment != null)
            {
                ModelState.AddModelError("", "该学生已选择此课程，不能重复选课");
            }
            
            // 检查所选课程是否有指定教师
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.CourseID == studentCourse.CourseID);
                
            if (course != null && course.Teacher == null)
            {
                ModelState.AddModelError("", "所选课程未指定教师，不能选择此课程");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(studentCourse);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentCourseExists(studentCourse.EnrollmentID))
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
            
            // 如果模型验证失败，重新准备下拉框数据
            var students = _context.Students.ToList();
            var studentSelectList = students.Select(s => new SelectListItem
            {
                Value = s.StudentID.ToString(),
                Text = $"{s.StudentID} - {s.StudentName}"
            }).ToList();
            ViewBag.StudentList = new SelectList(studentSelectList, "Value", "Text", studentCourse.StudentID);
            
            var courses = _context.Courses.Include(c => c.Teacher).ToList();
            var courseSelectList = courses.Select(c => new SelectListItem
            {
                Value = c.CourseID.ToString(),
                Text = $"{c.CourseID} - {c.CourseName} ({(c.Teacher != null ? c.Teacher.TeacherName : "未分配教师")})"
            }).ToList();
            ViewBag.CourseList = new SelectList(courseSelectList, "Value", "Text", studentCourse.CourseID);
            
            return View(studentCourse);
        }

        // 显示删除选课页面
        public async Task<IActionResult> Delete(int? id)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            
            if (id == null || _context.StudentCourses == null)
            {
                return NotFound();
            }

            var studentCourse = await _context.StudentCourses
                .Include(sc => sc.Student)
                .Include(sc => sc.Course)
                .FirstOrDefaultAsync(m => m.EnrollmentID == id);
                
            if (studentCourse == null)
            {
                return NotFound();
            }

            return View(studentCourse);
        }

        // 处理删除选课请求
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            
            if (_context.StudentCourses == null)
            {
                return Problem("Entity set 'ApplicationDbContext.StudentCourses' is null.");
            }
            
            var studentCourse = await _context.StudentCourses
                .Include(sc => sc.Student)
                .Include(sc => sc.Course)
                .FirstOrDefaultAsync(sc => sc.EnrollmentID == id);
                
            if (studentCourse == null)
            {
                return NotFound();
            }
            
            try
            {
                _context.StudentCourses.Remove(studentCourse);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "选课记录删除成功！";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"删除选课记录时发生错误：{ex.Message}";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }

        private bool StudentCourseExists(int id)
        {
            return (_context.StudentCourses?.Any(e => e.EnrollmentID == id)).GetValueOrDefault();
        }
    }
}