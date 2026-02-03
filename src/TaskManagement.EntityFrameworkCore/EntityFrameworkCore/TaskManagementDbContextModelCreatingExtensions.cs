using Microsoft.EntityFrameworkCore;
using TaskManagement.Tasks;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace TaskManagement.EntityFrameworkCore;

public static class TaskManagementDbContextModelCreatingExtensions
{
    public static void ConfigureTaskManagement(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<TaskItem>(b =>
        {
            b.ToTable(TaskManagementConsts.DbTablePrefix + "TaskItems", TaskManagementConsts.DbSchema);

            b.ConfigureByConvention();

            b.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(128);

            b.Property(x => x.Description)
                .HasMaxLength(2048);

            b.HasIndex(x => x.Status);
        });
    }
}