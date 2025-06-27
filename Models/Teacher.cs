using System.ComponentModel.DataAnnotations;

namespace StudentGradeManagementSystem.Models
{
    public class Teacher
    {
        public int TeacherID { get; set; }

        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
        public string TeacherName { get; set; }
        
        // 导航属性 - 教师教授的课程
        public ICollection<Course>? Courses { get; set; }
    }
}