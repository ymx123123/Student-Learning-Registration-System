﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StudentGradeManagementSystem.Data;

#nullable disable

namespace StudentGradeManagementSystem.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("StudentGradeManagementSystem.Models.Course", b =>
                {
                    b.Property<int>("CourseID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CourseID"));

                    b.Property<string>("CourseName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Credit")
                        .HasColumnType("int");

                    b.Property<int>("TeacherID")
                        .HasColumnType("int");

                    b.HasKey("CourseID");

                    b.HasIndex("TeacherID");

                    b.ToTable("Courses");
                });

            modelBuilder.Entity("StudentGradeManagementSystem.Models.Grade", b =>
                {
                    b.Property<int>("GradeID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("GradeID"));

                    b.Property<int>("CourseID")
                        .HasColumnType("int");

                    b.Property<double>("GradeValue")
                        .HasColumnType("float");

                    b.Property<int>("StudentID")
                        .HasColumnType("int");

                    b.Property<int>("TeacherID")
                        .HasColumnType("int");

                    b.HasKey("GradeID");

                    b.HasIndex("CourseID");

                    b.HasIndex("StudentID");

                    b.HasIndex("TeacherID");

                    b.ToTable("Grades");
                });

            modelBuilder.Entity("StudentGradeManagementSystem.Models.Student", b =>
                {
                    b.Property<int>("StudentID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("StudentID"));

                    b.Property<DateTime>("BirthDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Class")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Gender")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StudentName")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.HasKey("StudentID");

                    b.ToTable("Students");
                });

            modelBuilder.Entity("StudentGradeManagementSystem.Models.Teacher", b =>
                {
                    b.Property<int>("TeacherID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("TeacherID"));

                    b.Property<string>("TeacherName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("TeacherID");

                    b.ToTable("Teachers");
                });

            modelBuilder.Entity("StudentGradeManagementSystem.Models.Course", b =>
                {
                    b.HasOne("StudentGradeManagementSystem.Models.Teacher", "Teacher")
                        .WithMany("Courses")
                        .HasForeignKey("TeacherID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Teacher");
                });

            modelBuilder.Entity("StudentGradeManagementSystem.Models.Grade", b =>
                {
                    b.HasOne("StudentGradeManagementSystem.Models.Course", "Course")
                        .WithMany()
                        .HasForeignKey("CourseID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StudentGradeManagementSystem.Models.Student", "Student")
                        .WithMany()
                        .HasForeignKey("StudentID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StudentGradeManagementSystem.Models.Teacher", "Teacher")
                        .WithMany()
                        .HasForeignKey("TeacherID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Course");

                    b.Navigation("Student");

                    b.Navigation("Teacher");
                });

            modelBuilder.Entity("StudentGradeManagementSystem.Models.Teacher", b =>
                {
                    b.Navigation("Courses");
                });
#pragma warning restore 612, 618
        }
    }
}
