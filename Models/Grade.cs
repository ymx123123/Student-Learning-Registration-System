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
        public int TeacherID { get; set; }
        
        // 分项成绩
        [Required(ErrorMessage = "请输入课堂表现成绩")]
        [Range(0, 100, ErrorMessage = "课堂表现成绩必须在0-100之间")]
        public decimal ClassPerformance { get; set; }
        
        [Required(ErrorMessage = "请输入实验成绩")]
        [Range(0, 100, ErrorMessage = "实验成绩必须在0-100之间")]
        public decimal ExperimentGrade { get; set; }
        
        [Required(ErrorMessage = "请输入课后作业成绩")]
        [Range(0, 100, ErrorMessage = "课后作业成绩必须在0-100之间")]
        public decimal HomeworkGrade { get; set; }
        
        [Required(ErrorMessage = "请输入大作业成绩")]
        [Range(0, 100, ErrorMessage = "大作业成绩必须在0-100之间")]
        public decimal MajorAssignmentGrade { get; set; }
        
        // 加分项
        [Range(0, 10, ErrorMessage = "普通加分必须在0-10之间")]
        public decimal RegularBonus { get; set; }
        
        [Range(0, 10, ErrorMessage = "特别加分必须在0-10之间")]
        public decimal SpecialBonus { get; set; }
        
        [StringLength(200, ErrorMessage = "特别加分理由不能超过200个字符")]
        public string? SpecialBonusReason { get; set; }
        
        // 计算属性 - 最终成绩
        public decimal GradeValue
        {
            get
            {
                var baseGrade = ClassPerformance * 0.1m + 
                               ExperimentGrade * 0.2m + 
                               HomeworkGrade * 0.2m + 
                               MajorAssignmentGrade * 0.5m;
                var finalGrade = baseGrade + RegularBonus + SpecialBonus;
                return Math.Min(finalGrade, 100); // 确保不超过100分
            }
        }
        
        // 计算属性 - 等级
        public string GradeLevel
        {
            get
            {
                var grade = GradeValue;
                if (grade >= 90) return "A";
                if (grade >= 80) return "B";
                if (grade >= 70) return "C";
                if (grade >= 60) return "D";
                return "F";
            }
        }
        
        // 计算属性 - 绩点
        public decimal GradePoint
        {
            get
            {
                var grade = GradeValue;
                if (grade >= 90) return 4.0m;
                if (grade >= 85) return 3.7m;
                if (grade >= 82) return 3.3m;
                if (grade >= 78) return 3.0m;
                if (grade >= 75) return 2.7m;
                if (grade >= 72) return 2.3m;
                if (grade >= 68) return 2.0m;
                if (grade >= 64) return 1.5m;
                if (grade >= 60) return 1.0m;
                return 0.0m;
            }
        }
        
        // 计算属性 - 是否通过
        public bool IsPassed => GradeValue >= 60;

        public Student? Student { get; set; }
        public Course? Course { get; set; }
        public Teacher? Teacher { get; set; }
    }
}