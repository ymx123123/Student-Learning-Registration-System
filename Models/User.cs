using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentGradeManagementSystem.Models
{
    public class User
    {
        public int UserID { get; set; }
        
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
        public string Username { get; set; }
        
        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(100, ErrorMessage = "密码长度不能超过100个字符")]
        public string Password { get; set; }
        
        [Required(ErrorMessage = "角色不能为空")]
        [RegularExpression("学生|老师|管理员", ErrorMessage = "角色只能为学生、老师或管理员")]
        public string Role { get; set; }
        
        /// <summary>
        /// /外键 - 学生ID，可为空
        /// </summary>
        public int? StudentID { get; set; }
        
        // 外键 - 教师ID，可为空
        public int? TeacherID { get; set; }
        
        // 导航属性 - 关联的学生
        [ForeignKey("StudentID")]
        public Student? Student { get; set; }
        
        // 导航属性 - 关联的教师
        [ForeignKey("TeacherID")]
        public Teacher? Teacher { get; set; }
    }
}