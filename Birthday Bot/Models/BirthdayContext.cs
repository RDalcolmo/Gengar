using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Birthday_Bot.Models
{
	public partial class BirthdayContext : DbContext
	{
        private static DbContextOptions<BirthdayContext> _options;
		public BirthdayContext()
            : base (_options)
		{
		}

		public BirthdayContext(DbContextOptions<BirthdayContext> options)
			: base(options)
		{
            _options = options;
		}

		public virtual DbSet<Tblbirthdays> TblBirthdays { get; set; }
		public virtual DbSet<Tblguilds> TblGuilds { get; set; }

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
