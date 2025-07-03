using StudentGradeManagementSystem.Data;
using StudentGradeManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace StudentGradeManagementSystem.Services
{
    public class GpaService
    {
        private readonly ApplicationDbContext _context;

        public GpaService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 计算学生的平均GPA
        /// </summary>
        /// <param name="studentId">学生ID</param>
        /// <returns>平均GPA</returns>
        public async Task<decimal> GetStudentGpaAsync(int studentId)
        {
            var grades = await _context.Grades
                .Where(g => g.StudentID == studentId)
                .ToListAsync();

            if (!grades.Any())
                return 0;

            return Math.Round(grades.Average(g => g.GradePoint), 2);
        }

        /// <summary>
        /// 获取学生在全校的GPA排名
        /// </summary>
        /// <param name="studentId">学生ID</param>
        /// <returns>排名信息</returns>
        public async Task<(int rank, int total, decimal gpa)> GetStudentGpaRankingAsync(int studentId)
        {
            // 获取所有学生的成绩数据
            var allGrades = await _context.Grades
                .Include(g => g.Student)
                .ToListAsync();

            // 在客户端按学生分组并计算每个学生的GPA
            var allStudentGpas = allGrades
                .GroupBy(g => g.StudentID)
                .Select(group => new
                {
                    StudentId = group.Key,
                    Gpa = group.Average(g => g.GradePoint)
                })
                .Where(x => x.Gpa > 0)
                .OrderByDescending(x => x.Gpa)
                .ToList();

            var totalStudents = allStudentGpas.Count;
            var studentGpa = allStudentGpas.FirstOrDefault(x => x.StudentId == studentId)?.Gpa ?? 0;
            var rank = allStudentGpas.FindIndex(x => x.StudentId == studentId) + 1;

            return (rank, totalStudents, Math.Round(studentGpa, 2));
        }

        /// <summary>
        /// 获取班级GPA排名
        /// </summary>
        /// <param name="classId">班级ID</param>
        /// <returns>班级排名列表</returns>
        public async Task<List<StudentGpaRankingViewModel>> GetClassGpaRankingAsync(int? classId = null)
        {
            // 获取学生和成绩数据
            var studentsQuery = _context.Students
                .Include(s => s.Class)
                .AsQueryable();
            
            if (classId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.ClassID == classId);
            }

            var students = await studentsQuery.ToListAsync();
            var allGrades = await _context.Grades
                .Include(g => g.Course)
                .ToListAsync();

            // 在客户端计算GPA和其他统计信息
            var rankings = students
                .Select(s => {
                    var studentGrades = allGrades.Where(g => g.StudentID == s.StudentID).ToList();
                    return new StudentGpaRankingViewModel
                    {
                        StudentId = s.StudentID,
                        StudentName = s.StudentName,
                        ClassName = s.Class?.ClassName ?? "未分班",
                        Gpa = studentGrades.Any() ? Math.Round(studentGrades.Average(g => g.GradePoint), 2) : 0,
                        TotalCredits = studentGrades.Sum(g => g.Course?.Credit ?? 0),
                        PassedCourses = studentGrades.Count(g => g.GradeValue >= 60),
                        TotalCourses = studentGrades.Count()
                    };
                })
                .Where(x => x.TotalCourses > 0) // 只包含有成绩的学生
                .OrderByDescending(x => x.Gpa)
                .ToList();

            // 添加排名
            for (int i = 0; i < rankings.Count; i++)
            {
                rankings[i].Rank = i + 1;
            }

            return rankings;
        }
    }

    public class StudentGpaRankingViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public decimal Gpa { get; set; }
        public decimal TotalCredits { get; set; }
        public int PassedCourses { get; set; }
        public int TotalCourses { get; set; }
        public int Rank { get; set; }
        public decimal PassRate => TotalCourses > 0 ? Math.Round((decimal)PassedCourses / TotalCourses * 100, 1) : 0;
        public decimal AverageGpa { get; set; }
        public int SchoolRank { get; set; }
        public int TotalStudents { get; set; }
        public int ClassRank { get; set; }
        public int ClassStudents { get; set; }
    }
}