using Microsoft.EntityFrameworkCore;
using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Persistence;

public sealed class SpendnestDbContext : DbContext
{
    public SpendnestDbContext(DbContextOptions<SpendnestDbContext> options)
        : base(options)
    {
    }

    public DbSet<CardAccount> CardAccounts => Set<CardAccount>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CategoryRule> CategoryRules => Set<CategoryRule>();

    public DbSet<StatementImport> StatementImports => Set<StatementImport>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<TransactionCategoryAssignment> TransactionCategoryAssignments =>
        Set<TransactionCategoryAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CardAccount>(entity =>
        {
            entity.ToTable("CardAccounts");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Name).IsRequired();
            entity.HasIndex(account => account.Name).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).ValueGeneratedNever();
            entity.Property(category => category.Name).IsRequired();
        });

        modelBuilder.Entity<CategoryRule>(entity =>
        {
            entity.ToTable("CategoryRules");
            entity.HasKey(rule => rule.Id);
            entity.Property(rule => rule.Pattern).IsRequired();
            entity.Property(rule => rule.MatchType).HasConversion<int>();
            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(rule => rule.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StatementImport>(entity =>
        {
            entity.ToTable("StatementImports");
            entity.HasKey(statementImport => statementImport.Id);
            entity.Property(statementImport => statementImport.FilePath).IsRequired();
            entity.Property(statementImport => statementImport.FileName).IsRequired();
            entity.Property(statementImport => statementImport.FileHash).IsRequired();
            entity.Property(statementImport => statementImport.Status).HasConversion<int>();
            entity.HasIndex(statementImport => statementImport.FileHash);
            entity.HasOne<CardAccount>()
                .WithMany()
                .HasForeignKey(statementImport => statementImport.CardAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.OriginalDescription).IsRequired();
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.HasIndex(transaction => transaction.CardAccountId);
            entity.HasIndex(transaction => transaction.StatementImportId);
            entity.HasIndex(transaction => transaction.PostedDate);
            entity.HasOne<CardAccount>()
                .WithMany()
                .HasForeignKey(transaction => transaction.CardAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StatementImport>()
                .WithMany()
                .HasForeignKey(transaction => transaction.StatementImportId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TransactionCategoryAssignment>(entity =>
        {
            entity.ToTable("TransactionCategoryAssignments");
            entity.HasKey(assignment => assignment.TransactionId);
            entity.Property(assignment => assignment.TransactionId).ValueGeneratedNever();
            entity.Property(assignment => assignment.Confidence).HasPrecision(5, 4);
            entity.Property(assignment => assignment.Source).HasConversion<int>();
            entity.Property(assignment => assignment.Explanation).IsRequired();
            entity.HasIndex(assignment => assignment.CategoryId);
            entity.HasIndex(assignment => assignment.NeedsReview);
            entity.HasOne<Transaction>()
                .WithOne()
                .HasForeignKey<TransactionCategoryAssignment>(assignment => assignment.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(assignment => assignment.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
