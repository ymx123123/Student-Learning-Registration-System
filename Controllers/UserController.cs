using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagementSystem.Data;
using StudentGradeManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace StudentGradeManagementSystem.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 检查是否已登录
        private bool CheckLogin()
        {
            return HttpContext.Session.GetString("IsLoggedIn") == "true";
        }

        // 检查是否是管理员
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "管理员";
        }

        // GET: User
        public async Task<IActionResult> Index(string searchUsername, string filterRole)
        {
            // 获取所有用户并加载关联的学生和教师信息
            if (!CheckLogin() || !IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            var usersQuery = _context.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .AsQueryable();

            // 根据用户名搜索
            if (!string.IsNullOrEmpty(searchUsername))
            {
                usersQuery = usersQuery.Where(u => u.Username.Contains(searchUsername));
            }

            // 根据角色筛选
            if (!string.IsNullOrEmpty(filterRole))
            {
                usersQuery = usersQuery.Where(u => u.Role == filterRole);
            }

            var finalUserList = await usersQuery.ToListAsync();

            // 设置ViewBag用于前端显示
            ViewBag.SearchUsername = searchUsername;
            ViewBag.FilterRole = filterRole;
            
            // 创建角色选项列表
            var roleList = new[] { "学生", "老师", "管理员" };
            var roleOptions = roleList.Select(role => new SelectListItem
            {
                Value = role,
                Text = role,
                Selected = role == filterRole
            }).ToList();
            ViewBag.RoleOptions = roleOptions;

            return View(finalUserList);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            if (!CheckLogin() || !IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            // 获取未创建账户的学生列表
            var existingStudentIds = _context.Users.Where(u => u.StudentID != null).Select(u => u.StudentID).ToList();
            var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                .ToList();
            ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText");

            // 获取未创建账户的教师列表
            var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null).Select(u => u.TeacherID).ToList();
            var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                .ToList();
            ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText");

            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Password,Role,StudentID,TeacherID")] User user)
        {

            
            if (!CheckLogin() || !IsAdmin())
            {

                return RedirectToAction("Index", "Login");
            }

            // 先设置默认密码，避免Required验证失败
            user.Password = "123456";
            
            // 清除Password字段的验证错误，因为我们已经设置了默认值
            ModelState.Remove("Password");

            // 根据角色设置用户名和验证规则
            
            if (user.Role == "学生")
            {

                if (user.StudentID == null)
                {

                    ModelState.AddModelError("StudentID", "学生角色必须选择学生ID");
                    
                    // 重新加载学生列表
                    var existingStudentIds = _context.Users.Where(u => u.StudentID != null).Select(u => u.StudentID).ToList();
                    var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                        .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                        .ToList();
                    ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText");
                    
                    // 重新加载教师列表
                    var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null).Select(u => u.TeacherID).ToList();
                    var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                        .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                        .ToList();
                    ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText");
                    
                    return View(user);
                }

                // 获取学生信息并设置用户名为S+学生ID
                var student = await _context.Students.FindAsync(user.StudentID);
                if (student == null)
                {

                    ModelState.AddModelError("StudentID", "所选学生不存在");
                    
                    // 重新加载学生和教师列表
                    var existingStudentIds = _context.Users.Where(u => u.StudentID != null).Select(u => u.StudentID).ToList();
                    var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                        .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                        .ToList();
                    ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText");
                    
                    var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null).Select(u => u.TeacherID).ToList();
                    var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                        .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                        .ToList();
                    ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText");
                    
                    return View(user);
                }
                user.Username = "S" + student.StudentID.ToString();
                user.TeacherID = null;
                
                // 清除Username字段的验证错误，因为我们已经设置了值
                ModelState.Remove("Username");
            }
            else if (user.Role == "老师")
            {

                if (user.TeacherID == null)
                {

                    ModelState.AddModelError("TeacherID", "教师角色必须选择教师ID");
                    
                    // 重新加载学生和教师列表
                    var existingStudentIds = _context.Users.Where(u => u.StudentID != null).Select(u => u.StudentID).ToList();
                    var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                        .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                        .ToList();
                    ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText");
                    
                    var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null).Select(u => u.TeacherID).ToList();
                    var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                        .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                        .ToList();
                    ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText");
                    
                    return View(user);
                }

                // 获取教师信息并设置用户名为T+教师ID
                var teacher = await _context.Teachers.FindAsync(user.TeacherID);
                if (teacher == null)
                {

                    ModelState.AddModelError("TeacherID", "所选教师不存在");
                    
                    // 重新加载学生和教师列表
                    var existingStudentIds = _context.Users.Where(u => u.StudentID != null).Select(u => u.StudentID).ToList();
                    var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                        .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                        .ToList();
                    ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText");
                    
                    var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null).Select(u => u.TeacherID).ToList();
                    var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                        .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                        .ToList();
                    ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText");
                    
                    return View(user);
                }
                user.Username = "T" + teacher.TeacherID.ToString();
                user.StudentID = null;
                
                // 清除Username字段的验证错误，因为我们已经设置了值
                ModelState.Remove("Username");
            }
            else if (user.Role == "管理员")
            {

                if (string.IsNullOrEmpty(user.Username))
                {

                    ModelState.AddModelError("Username", "管理员角色必须输入用户名");
                    
                    // 重新加载学生和教师列表
                    var existingStudentIds = _context.Users.Where(u => u.StudentID != null).Select(u => u.StudentID).ToList();
                    var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                        .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                        .ToList();
                    ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText");
                    
                    var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null).Select(u => u.TeacherID).ToList();
                    var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                        .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                        .ToList();
                    ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText");
                    
                    return View(user);
                }
                
                // 检查用户名是否已存在
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
                if (existingUser != null)
                {

                    ModelState.AddModelError("Username", "该用户名已被使用");
                    
                    // 重新加载学生和教师列表
                    var existingStudentIds = _context.Users.Where(u => u.StudentID != null).Select(u => u.StudentID).ToList();
                    var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                        .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                        .ToList();
                    ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText");
                    
                    var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null).Select(u => u.TeacherID).ToList();
                    var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                        .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                        .ToList();
                    ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText");
                    
                    return View(user);
                }
                
                user.StudentID = null;
                user.TeacherID = null;

            }
            else
            {

                ModelState.AddModelError("Role", "请选择有效的用户角色");
                
                // 重新加载学生和教师列表
                var existingStudentIds = _context.Users.Where(u => u.StudentID != null).Select(u => u.StudentID).ToList();
                var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                    .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                    .ToList();
                ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText");
                
                var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null).Select(u => u.TeacherID).ToList();
                var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                    .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                    .ToList();
                ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText");
                
                return View(user);
            }

            // 检查模型验证状态
            if (ModelState.IsValid)
            {
                try
                {

                    _context.Add(user);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {

                    ModelState.AddModelError("", $"保存用户时发生错误: {ex.Message}");
                    
                    // 重新加载学生和教师列表
                    var existingStudentIds = _context.Users.Where(u => u.StudentID != null).Select(u => u.StudentID).ToList();
                    var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                        .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                        .ToList();
                    ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText");
                    
                    var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null).Select(u => u.TeacherID).ToList();
                    var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                        .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                        .ToList();
                    ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText");
                    
                    return View(user);
                }
            }
            else
            {

                
                // 重新加载学生和教师列表
                var existingStudentIds = _context.Users.Where(u => u.StudentID != null).Select(u => u.StudentID).ToList();
                var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                    .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                    .ToList();
                ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText");
                
                var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null).Select(u => u.TeacherID).ToList();
                var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                    .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                    .ToList();
                ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText");
                
                return View(user);
            }
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!CheckLogin() || !IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            // 检查是否是当前登录的管理员账户
            var currentUsername = HttpContext.Session.GetString("Username");
            var isCurrentAdmin = user.Role == "管理员" && user.Username == currentUsername;
            ViewBag.IsCurrentAdmin = isCurrentAdmin;

            // 获取可用的学生列表（排除已有账户的学生，但包括当前用户关联的学生）
            var existingStudentIds = _context.Users.Where(u => u.StudentID != null && u.UserID != id).Select(u => u.StudentID).ToList();
            var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                .ToList();
            ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText", user.StudentID);

            // 获取可用的教师列表（排除已有账户的教师，但包括当前用户关联的教师）
            var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null && u.UserID != id).Select(u => u.TeacherID).ToList();
            var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                .ToList();
            ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText", user.TeacherID);

            return View(user);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserID,Username,Password,Role,StudentID,TeacherID")] User user)
        {
            if (!CheckLogin() || !IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            if (id != user.UserID)
            {
                return NotFound();
            }

            // 获取原始用户数据
            var originalUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == id);
            if (originalUser == null)
            {
                return NotFound();
            }

            // 检查是否是当前登录的管理员账户
            var currentUsername = HttpContext.Session.GetString("Username");
            var isCurrentAdmin = originalUser.Role == "管理员" && originalUser.Username == currentUsername;
            
            if (isCurrentAdmin)
            {
                // 如果是当前登录的管理员，不允许修改用户名和密码
                user.Username = originalUser.Username;
                user.Password = originalUser.Password;
                user.Role = originalUser.Role; // 也不允许修改角色
                
                TempData["ErrorMessage"] = "不能修改当前登录的管理员账户的用户名、密码和角色！";
            }
            else
            {
                // 如果密码字段为空，保留原密码
                if (string.IsNullOrEmpty(user.Password))
                {
                    user.Password = originalUser.Password;
                }
            }
            
            ViewBag.IsCurrentAdmin = isCurrentAdmin;

            // 根据角色设置用户名和验证规则
            if (user.Role == "学生")
            {
                if (user.StudentID == null)
                {
                    ModelState.AddModelError("StudentID", "学生角色必须选择学生ID");
                    return View(user);
                }

                // 获取学生信息并设置用户名为S+学生ID
                var student = await _context.Students.FindAsync(user.StudentID);
                if (student == null)
                {
                    ModelState.AddModelError("StudentID", "所选学生不存在");
                    return View(user);
                }
                user.Username = "S" + student.StudentID.ToString();
                user.TeacherID = null;
            }
            else if (user.Role == "老师")
            {
                if (user.TeacherID == null)
                {
                    ModelState.AddModelError("TeacherID", "教师角色必须选择教师ID");
                    return View(user);
                }

                // 获取教师信息并设置用户名为T+教师ID
                var teacher = await _context.Teachers.FindAsync(user.TeacherID);
                if (teacher == null)
                {
                    ModelState.AddModelError("TeacherID", "所选教师不存在");
                    return View(user);
                }
                user.Username = "T" + teacher.TeacherID.ToString();
                user.StudentID = null;
            }
            else if (user.Role == "管理员")
            {
                user.StudentID = null;
                user.TeacherID = null;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserID))
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

            // 如果验证失败，重新加载下拉列表
            var existingStudentIds = _context.Users.Where(u => u.StudentID != null && u.UserID != id).Select(u => u.StudentID).ToList();
            var availableStudents = _context.Students.Where(s => !existingStudentIds.Contains(s.StudentID))
                .Select(s => new { s.StudentID, DisplayText = $"{s.StudentID} - {s.StudentName}" })
                .ToList();
            ViewBag.StudentList = new SelectList(availableStudents, "StudentID", "DisplayText", user.StudentID);

            var existingTeacherIds = _context.Users.Where(u => u.TeacherID != null && u.UserID != id).Select(u => u.TeacherID).ToList();
            var availableTeachers = _context.Teachers.Where(t => !existingTeacherIds.Contains(t.TeacherID))
                .Select(t => new { t.TeacherID, DisplayText = $"{t.TeacherID} - {t.TeacherName}" })
                .ToList();
            ViewBag.TeacherList = new SelectList(availableTeachers, "TeacherID", "DisplayText", user.TeacherID);

            return View(user);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!CheckLogin() || !IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(m => m.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            // 防止删除当前登录的管理员账户
            if (user.Username == "yumingxiang")
            {
                ViewBag.ErrorMessage = "不能删除当前登录的管理员账户！";
            }

            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!CheckLogin() || !IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // 防止删除当前登录的管理员账户
                if (user.Username == "yumingxiang")
                {
                    TempData["ErrorMessage"] = "不能删除当前登录的管理员账户！";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                // 删除用户账户
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "用户账户删除成功！";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"删除用户账户时发生错误：{ex.Message}";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserID == id);
        }

        // 批量注册用户
        [HttpPost]
        public async Task<IActionResult> BatchRegister()
        {
            if (!CheckLogin() || !IsAdmin())
            {
                return Json(new { success = false, message = "权限不足" });
            }

            try
            {
                var registeredCount = 0;
                var errors = new List<string>();

                // 获取未创建账户的学生列表
                var existingStudentIds = await _context.Users
                    .Where(u => u.StudentID != null)
                    .Select(u => u.StudentID)
                    .ToListAsync();
                
                var unregisteredStudents = await _context.Students
                    .Where(s => !existingStudentIds.Contains(s.StudentID))
                    .ToListAsync();

                // 为学生批量创建账户
                foreach (var student in unregisteredStudents)
                {
                    try
                    {
                        var user = new User
                        {
                            Username = $"S{student.StudentID}", // 学生账号格式：S + 学号
                            Password = "123456", // 初始密码：123456
                            Role = "学生",
                            StudentID = student.StudentID
                        };
                        
                        _context.Users.Add(user);
                        registeredCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"学生{student.StudentName}(ID:{student.StudentID})注册失败：{ex.Message}");
                    }
                }

                // 获取未创建账户的教师列表
                var existingTeacherIds = await _context.Users
                    .Where(u => u.TeacherID != null)
                    .Select(u => u.TeacherID)
                    .ToListAsync();
                
                var unregisteredTeachers = await _context.Teachers
                    .Where(t => !existingTeacherIds.Contains(t.TeacherID))
                    .ToListAsync();

                // 为教师批量创建账户
                foreach (var teacher in unregisteredTeachers)
                {
                    try
                    {
                        var user = new User
                        {
                            Username = $"T{teacher.TeacherID}", // 教师账号格式：T + 工号
                            Password = "123456", // 初始密码：123456
                            Role = "老师",
                            TeacherID = teacher.TeacherID
                        };
                        
                        _context.Users.Add(user);
                        registeredCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"教师{teacher.TeacherName}(ID:{teacher.TeacherID})注册失败：{ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                var message = $"批量注册完成！成功注册{registeredCount}个账户。";
                if (errors.Any())
                {
                    message += $" 失败{errors.Count}个：{string.Join("; ", errors)}";
                }

                return Json(new { success = true, message = message, count = registeredCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"批量注册失败：{ex.Message}" });
            }
        }
    }
}