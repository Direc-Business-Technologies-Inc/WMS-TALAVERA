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
        builder.HasMany(u => u.Permissions)
            .WithOne()
            .HasForeignKey(p => p.UserId);
        builder.HasOne<RoleDEM>()
            .WithMany()
            .HasForeignKey(u => u.RoleId);
    }
}
