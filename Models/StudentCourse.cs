using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentGradeManagementSystem.Models
{
    public class StudentCourse
    {
        [Key]
        public int EnrollmentID { get; set; } // 主键选课ID
        
        [Required(ErrorMessage = "请选择学生")]
        public int StudentID { get; set; } // 外键StudentID
        
        [Required(ErrorMessage = "请选择课程")]
        public int CourseID { get; set; } // 外键CourseID
        
        // 导航属性
        public Student? Student { get; set; }
        public Course? Course { get; set; }
    }
}