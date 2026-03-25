using Microsoft.EntityFrameworkCore;
using SecureChat.Models;

namespace SecureChat.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationMember> ConversationMembers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<PendingRegistration> PendingRegistrations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /* =========================
               USERS
            ========================= */
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasMaxLength(50);

                entity.Property(e => e.FullName)
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .HasMaxLength(256);

                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20);

                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(500);

                entity.Property(e => e.AvatarUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.Role)
                    .HasMaxLength(20)
                    .HasDefaultValue("User");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.FailedLoginCount)
                    .HasDefaultValue(0);

                entity.Property(e => e.IsOnline)
                    .HasDefaultValue(false);

                entity.Property(e => e.IsVerified)
                    .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(50);

                entity.Property(e => e.DeviceInfo)
                    .HasMaxLength(255);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            /* =========================
               OTP VERIFICATION
            ========================= */
            modelBuilder.Entity<OtpVerification>(entity =>
            {
                entity.ToTable("OtpVerifications");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.OtpCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Purpose)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            /* =========================
               FRIEND REQUESTS
            ========================= */
            modelBuilder.Entity<FriendRequest>(entity =>
            {
                entity.ToTable("FriendRequests");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Pending");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Receiver)
                    .WithMany()
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            /* =========================
               FRIENDS
            ========================= */
            modelBuilder.Entity<Friend>(entity =>
            {
                entity.ToTable("Friends");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.FriendUser)
                    .WithMany()
                    .HasForeignKey(e => e.FriendUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            /* =========================
               CONVERSATIONS
            ========================= */
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.ToTable("Conversations");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .HasMaxLength(200);

                entity.Property(e => e.Type)
                    .HasMaxLength(20);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            /* =========================
               CONVERSATION MEMBERS
            ========================= */
            modelBuilder.Entity<ConversationMember>(entity =>
            {
                entity.ToTable("ConversationMembers");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Role)
                    .HasMaxLength(20)
                    .HasDefaultValue("Member");

                entity.Property(e => e.JoinedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Members)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            /* =========================
               MESSAGES
            ========================= */
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Messages");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.EncryptedContent)
                    .IsRequired();

                entity.Property(e => e.SentAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.IsEdited)
                    .HasDefaultValue(false);

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);

                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.SenderId);
            });

            /* =========================
               PENDING REGISTRATION
            ========================= */
            modelBuilder.Entity<PendingRegistration>(entity =>
            {
                entity.ToTable("PendingRegistrations");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.OtpCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.Email);
            });
        }
    }
}