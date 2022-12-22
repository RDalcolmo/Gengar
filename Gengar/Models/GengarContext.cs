using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Gengar.Models
{
	public partial class GengarContext : DbContext
	{
		public GengarContext(DbContextOptions<GengarContext> options)
			: base(options)
		{
		}

		public virtual DbSet<Tblbirthdays> TblBirthdays { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tblbirthdays>(entity =>
            {
                entity.ToTable("tblbirthdays");

                entity.Property(e => e.Birthday)
                    .HasColumnName("birthday")
                    .HasColumnType("date");

                entity.Property(e => e.Comments).HasColumnName("comments");

                entity.Property(e => e.Userid).HasColumnName("userid").IsRequired();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
