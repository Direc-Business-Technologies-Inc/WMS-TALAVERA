using Domain.Entities.Administration.User.Management;
using Domain.Entities.Administration.User.Role;
using Domain.Enums.System;
using Domain.ValueObjects.Others;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.Administration;

internal class UserCfg : IEntityTypeConfiguration<UserDEM>
{
    public void Configure(EntityTypeBuilder<UserDEM> builder)
    {
        builder.ToTable("OUSR");
        builder.Property(u => u.Id)
            .IsRequired();
        builder.HasKey(u => u.Id)
            .HasName("PK_OUSR");
        builder.OwnsOne(u => u.Account, acc =>
        {
            acc.ToTable("USR1");
            acc.WithOwner().HasForeignKey("UserId");
            acc.OwnsOne(a => a.UserName, unme =>
            {
                unme.Property(u => u.Value);
            });
        });
        builder.HasMany(u => u.LoginHistory)
            .WithOne()
            .HasForeignKey(l => l.AccountId);
        builder.OwnsOne(u => u.Name, name =>
        {
            //name.OwnsOne(n => n.FirstName, firstName =>
            //{
            //    firstName.Property(fn => fn.Value)
            //        .HasColumnName("FirstName")
            //        .HasMaxLength(50)
            //        .IsRequired();
            //});
            
            name.Property(n => n.FirstName)
                .HasColumnName("FirstName")
                .HasMaxLength(50)
                .IsRequired();
            name.Property(n => n.MiddleName)
                .HasColumnName("MiddleName")
                .HasMaxLength(50);
            name.Property(n => n.LastName)
                .HasColumnName("LastName")
                .HasMaxLength(50)
                .IsRequired();
        });
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Address)
                .HasColumnName("EmailAddress")
                .HasMaxLength(100)
                .IsRequired();
        });
        builder.OwnsOne(u => u.EmployeeNs, employee =>
        {
            employee.Property(e => e.NsId)
                .HasColumnName("NSEmployeeId")
                .IsRequired(false);
            employee.Property(e => e.EmployeeCode)
                .HasColumnName("NSEmployeeCode")
                .HasMaxLength(50)
                .IsRequired(false);
            employee.Property(e => e.FirstName)
                .HasColumnName("NSEmployeeFirstName")
                .HasMaxLength(100)
                .IsRequired(false);
            employee.Property(e => e.LastName)
                .HasColumnName("NSEmployeeLastName")
                .HasMaxLength(100)
                .IsRequired(false);
            employee.Property(e => e.NsDepartmentId)
                .HasColumnName("NSDepartmentId")
                .IsRequired(false);
            employee.Property(e => e.DepartmentName)
                .HasColumnName("NSDepartmentName")
                .HasMaxLength(200)
                .IsRequired(false);
            employee.Property(e => e.NsSubsidiaryId)
                .HasColumnName("NSSubsidiaryId")
                .IsRequired(false);
            employee.Property(e => e.SubsidiaryName)
                .HasColumnName("NSSubsidiaryName")
                .HasMaxLength(200)
                .IsRequired(false);
        });
        builder.Navigation(u => u.EmployeeNs)
            .IsRequired(false);
        builder.HasMany(u => u.Permissions)
            .WithOne()
            .HasForeignKey(p => p.UserId);
        builder.HasOne<RoleDEM>()
            .WithMany()
            .HasForeignKey(u => u.RoleId);
    }
}
