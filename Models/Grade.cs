using System;
using System.ComponentModel.DataAnnotations;

namespace StudentGradeManagementSystem.Models
{
    public class Grade
    {
        public int GradeID { get; set; }
        [Required(ErrorMessage = "请选择学生")]
        public int StudentID { get; set; }
        [Required(ErrorMessage = "请选择课程")]
        public int CourseID { get; set; }
        [Required(ErrorMessage = "请选择任课老师")]
        public int TeacherID { get; set; } // 外键
        [Required(ErrorMessage = "请输入成绩")]
        [Range(0, 100, ErrorMessage = "成绩必须在0-100之间")]
        public decimal GradeValue { get; set; }

        public Student? Student { get; set; }
        public Course? Course { get; set; }
        public Teacher? Teacher { get; set; }
    }
}