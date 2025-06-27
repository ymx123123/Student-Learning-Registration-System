using Microsoft.EntityFrameworkCore;
using StudentGradeManagementSystem.Models;

namespace StudentGradeManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<StudentCourse> StudentCourses { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<User> Users { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany(t => t.Courses)
                .HasForeignKey(c => c.TeacherID)
                .IsRequired(false); // 设置为可选关系
                
            // 配置StudentCourse实体
            modelBuilder.Entity<StudentCourse>()
                .HasOne(sc => sc.Student)
                .WithMany(s => s.StudentCourses)
                .HasForeignKey(sc => sc.StudentID);
                
            modelBuilder.Entity<StudentCourse>()
                .HasOne(sc => sc.Course)
                .WithMany(c => c.StudentCourses)
                .HasForeignKey(sc => sc.CourseID);
                
            // 添加唯一约束，确保一个学生不会重复选同一门课程
            modelBuilder.Entity<StudentCourse>()
                .HasIndex(sc => new { sc.StudentID, sc.CourseID })
                .IsUnique();
                
            // 配置Student和Class的关系
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassID);
                
            // 配置User和Student的关系
            modelBuilder.Entity<User>()
                .HasOne(u => u.Student)
                .WithOne()
                .HasForeignKey<User>(u => u.StudentID)
                .IsRequired(false);
                
            // 配置User和Teacher的关系
            modelBuilder.Entity<User>()
                .HasOne(u => u.Teacher)
                .WithOne()
                .HasForeignKey<User>(u => u.TeacherID)
                .IsRequired(false);
                
            // 添加约束，确保一个学生只能有一个账户
            modelBuilder.Entity<User>()
                .HasIndex(u => u.StudentID)
                .IsUnique();
                
            // 添加约束，确保一个教师只能有一个账户
            modelBuilder.Entity<User>()
                .HasIndex(u => u.TeacherID)
                .IsUnique();
        }
    }
}