using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

public static class DevelopmentDataSeeder
{
    public static async Task SeedCustomersAsync(ApplicationDbContext context)
    {
        if (!await context.Customers.AnyAsync())
        {
            context.Customers.AddRange(
                new Customer
                {
                    FirstName = "Avery",
                    LastName = "Johnson",
                    Email = "avery.johnson@example.com",
                    Phone = "+1 555 010 1001",
                    Company = "Northstar Labs",
                    Address = "120 Market Street",
                    City = "San Francisco",
                    State = "CA",
                    Country = "United States",
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                },
                new Customer
                {
                    FirstName = "Jordan",
                    LastName = "Lee",
                    Email = "jordan.lee@example.com",
                    Phone = "+1 555 010 1002",
                    Company = "Brightpath Studio",
                    Address = "44 King Street",
                    City = "Toronto",
                    State = "ON",
                    Country = "Canada",
                    CreatedDate = DateTime.UtcNow.AddDays(-1)
                },
                new Customer
                {
                    FirstName = "Morgan",
                    LastName = "Patel",
                    Email = "morgan.patel@example.com",
                    Phone = "+1 555 010 1003",
                    Company = "Fieldstone Group",
                    Address = "8 River Road",
                    City = "Austin",
                    State = "TX",
                    Country = "United States",
                    CreatedDate = DateTime.UtcNow
                });
            await context.SaveChangesAsync();
        }

        if (!await context.Leads.AnyAsync())
        {
            context.Leads.AddRange(
                new Lead
                {
                    LeadCode = "LD-DEMO-0001",
                    Name = "Taylor Brooks",
                    Email = "taylor.brooks@example.com",
                    Phone = "+1 555 010 1004",
                    Company = "Cedar Peak",
                    Source = LeadSource.Referral,
                    Status = LeadStatus.Qualified,
                    Priority = LeadPriority.High,
                    ExpectedValue = 12000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-5)
                },
                new Lead
                {
                    LeadCode = "LD-DEMO-0002",
                    Name = "Jamie Nguyen",
                    Email = "jamie.nguyen@example.com",
                    Phone = "+1 555 010 1005",
                    Company = "Waypoint Works",
                    Source = LeadSource.Website,
                    Status = LeadStatus.Contacted,
                    Priority = LeadPriority.Normal,
                    ExpectedValue = 7500m,
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                });
        }

        var customer = await context.Customers.OrderBy(item => item.Id).FirstAsync();
        if (!await context.Opportunities.AnyAsync())
        {
            context.Opportunities.Add(new Opportunity
            {
                Name = "Northstar renewal",
                CustomerId = customer.Id,
                Stage = OpportunityStage.Proposal,
                Amount = 18000m,
                Probability = 65,
                ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(40),
                Source = "Customer expansion",
                Notes = "Development sample opportunity."
            });
        }

        if (!await context.FollowUps.AnyAsync())
        {
            context.FollowUps.Add(new FollowUp
            {
                CustomerId = customer.Id,
                Subject = "Quarterly check-in",
                Type = FollowUpType.Call,
                Status = FollowUpStatus.Planned,
                FollowUpDate = DateTime.UtcNow.Date.AddDays(3),
                Notes = "Development sample follow-up."
            });
        }

        await context.SaveChangesAsync();
    }
}
