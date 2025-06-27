using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentGradeManagementSystem.Models
{
    public class Course
    {
        [Display(Name = "课程编号")]
        public int CourseID { get; set; }
        
        [Required(ErrorMessage = "课程名称不能为空")]
        [Display(Name = "课程名称")]
        public string CourseName { get; set; }
        
        [Required(ErrorMessage = "学分不能为空")]
        [Display(Name = "学分")]
        public int Credit { get; set; } // 保证与数据库一致
        
        [Display(Name = "任课教师")]
        public int? TeacherID { get; set; } // 允许为空
        public Teacher? Teacher { get; set; }
        
        // 导航属性 - 课程选课信息
        public ICollection<StudentCourse>? StudentCourses { get; set; }
    }
}