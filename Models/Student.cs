using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace StudentGradeManagementSystem.Models
{
    public class Student
    {
        public int StudentID { get; set; }

        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(20, ErrorMessage = "姓名长度不能超过20个字符")]
        public string StudentName { get; set; }

        [Required(ErrorMessage = "性别不能为空")]
        [RegularExpression("男|女", ErrorMessage = "性别只能为男或女")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "出生日期不能为空")]
        [DataType(DataType.Date, ErrorMessage = "出生日期格式不正确")]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "班级不能为空")]
        public int ClassID { get; set; }
        
        // 导航属性 - 学生所属班级
        public Class? Class { get; set; }
        
        // 导航属性 - 学生选课信息
        public ICollection<StudentCourse>? StudentCourses { get; set; }
    }
}