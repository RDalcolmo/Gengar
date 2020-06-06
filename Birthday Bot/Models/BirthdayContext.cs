using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Birthday_Bot.Models
{
	public partial class BirthdayContext : DbContext
	{
		public BirthdayContext()
		{
		}

		public BirthdayContext(DbContextOptions<BirthdayContext> options)
			: base(options)
		{
		}

		public virtual DbSet<Tblbirthdays> TblBirthdays { get; set; }
		public virtual DbSet<Tblguilds> TblGuilds { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseNpgsql(Startup.Configuration["ConnectionString"]);
			}
		}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tblbirthdays>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("tblbirthdays");

                entity.Property(e => e.Birthday)
                    .HasColumnName("birthday")
                    .HasColumnType("date");

                entity.Property(e => e.Comments).HasColumnName("comments");

                entity.Property(e => e.Userid).HasColumnName("userid");
            });

            modelBuilder.Entity<Tblguilds>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("tblguilds");

                entity.Property(e => e.Channelid).HasColumnName("channelid");

                entity.Property(e => e.Guildid).HasColumnName("guildid");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
