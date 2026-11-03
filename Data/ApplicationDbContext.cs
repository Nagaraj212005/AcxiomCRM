using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerActivity> CustomerActivities => Set<CustomerActivity>();

    public DbSet<Lead> Leads => Set<Lead>();

    public DbSet<Opportunity> Opportunities => Set<Opportunity>();

    public DbSet<FollowUp> FollowUps => Set<FollowUp>();

    public DbSet<CrmActivity> CrmActivities => Set<CrmActivity>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>()
            .HasIndex(customer => customer.Email)
            .IsUnique()
            .HasDatabaseName("IX_Customers_Email");
        builder.Entity<Customer>()
            .HasIndex(customer => customer.Phone)
            .IsUnique()
            .HasDatabaseName("IX_Customers_Phone");
        builder.Entity<Customer>()
            .HasIndex(customer => customer.City)
            .HasDatabaseName("IX_Customers_City");
        builder.Entity<Customer>()
            .HasIndex(customer => customer.CreatedDate)
            .HasDatabaseName("IX_Customers_CreatedDate");
        builder.Entity<Customer>()
            .HasIndex(customer => new { customer.Status, customer.NextFollowUpDate })
            .HasDatabaseName("IX_Customers_Status_NextFollowUpDate");
        builder.Entity<CustomerActivity>()
            .HasIndex(activity => activity.OccurredAt)
            .HasDatabaseName("IX_CustomerActivities_OccurredAt");
        builder.Entity<CustomerActivity>()
            .HasOne(activity => activity.Customer)
            .WithMany(customer => customer.Activities)
            .HasForeignKey(activity => activity.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Customer>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(customer => customer.AssignedTo)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<Customer>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(customer => customer.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Lead>()
            .Property(lead => lead.LeadCode)
            .IsRequired();
        builder.Entity<Lead>()
            .HasIndex(lead => lead.LeadCode)
            .IsUnique();
        builder.Entity<Lead>()
            .HasIndex(lead => new { lead.Status, lead.AssignedTo });
        builder.Entity<Lead>()
            .Property(lead => lead.ExpectedValue)
            .HasPrecision(14, 2);
        builder.Entity<Lead>()
            .HasOne(lead => lead.ConvertedCustomer)
            .WithMany()
            .HasForeignKey(lead => lead.ConvertedCustomerId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<Lead>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(lead => lead.AssignedTo)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<Lead>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(lead => lead.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Opportunity>()
            .HasOne(opportunity => opportunity.Customer)
            .WithMany()
            .HasForeignKey(opportunity => opportunity.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<Opportunity>()
            .HasOne(opportunity => opportunity.Lead)
            .WithMany(lead => lead.Opportunities)
            .HasForeignKey(opportunity => opportunity.LeadId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<Opportunity>()
            .HasIndex(opportunity => new { opportunity.Stage, opportunity.AssignedTo });
        builder.Entity<Opportunity>()
            .Property(opportunity => opportunity.Amount)
            .HasPrecision(14, 2);
        builder.Entity<Opportunity>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(opportunity => opportunity.AssignedTo)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<Opportunity>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(opportunity => opportunity.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<FollowUp>()
            .HasOne(followUp => followUp.Customer)
            .WithMany()
            .HasForeignKey(followUp => followUp.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<FollowUp>()
            .HasOne(followUp => followUp.Lead)
            .WithMany(lead => lead.FollowUps)
            .HasForeignKey(followUp => followUp.LeadId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<FollowUp>()
            .HasOne(followUp => followUp.Opportunity)
            .WithMany()
            .HasForeignKey(followUp => followUp.OpportunityId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<FollowUp>()
            .HasIndex(followUp => new { followUp.Status, followUp.FollowUpDate, followUp.AssignedTo });
        builder.Entity<FollowUp>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(followUp => followUp.AssignedTo)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<FollowUp>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(followUp => followUp.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<CrmActivity>()
            .HasOne(activity => activity.Customer)
            .WithMany()
            .HasForeignKey(activity => activity.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<CrmActivity>()
            .HasOne(activity => activity.Lead)
            .WithMany()
            .HasForeignKey(activity => activity.LeadId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<CrmActivity>()
            .HasOne(activity => activity.Opportunity)
            .WithMany()
            .HasForeignKey(activity => activity.OpportunityId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<CrmActivity>()
            .HasIndex(activity => new { activity.Type, activity.ActivityDate, activity.AssignedTo });
        builder.Entity<CrmActivity>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(activity => activity.AssignedTo)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<CrmActivity>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(activity => activity.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<AuditLog>()
            .HasIndex(audit => audit.CreatedDate)
            .IsDescending();
        builder.Entity<AuditLog>()
            .HasIndex(audit => new { audit.UserId, audit.Module, audit.Action });
        builder.Entity<AuditLog>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(audit => audit.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}