using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentGradeManagementSystem.Models
{
    public class Class
    {
        [Key]
        public int ClassID { get; set; }
        
        [Required(ErrorMessage = "班级名称不能为空")]
        [StringLength(10, ErrorMessage = "班级名称长度不能超过10个字符")]
        public string ClassName { get; set; }
        
        // 导航属性 - 班级中的学生
        public ICollection<Student>? Students { get; set; }
    }
}