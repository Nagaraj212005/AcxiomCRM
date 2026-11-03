namespace AcxiomCRM.Models;

public enum LeadStatus
{
    New,
    Contacted,
    Qualified,
    Unqualified,
    Converted,
    Lost
}

public enum LeadSource
{
    Website,
    Referral,
    Campaign,
    Event,
    ColdCall,
    Other
}

public enum LeadPriority
{
    Low,
    Normal,
    High
}

public enum OpportunityStage
{
    Qualification,
    Proposal,
    Negotiation,
    Won,
    Lost
}

public enum FollowUpType
{
    Call,
    Meeting,
    Email,
    Task
}

public enum FollowUpStatus
{
    Planned,
    Completed,
    Missed,
    Cancelled
}

public enum ActivityType
{
    Call,
    Meeting,
    Email,
    Task
}

public enum ActivityStatus
{
    Planned,
    Completed,
    Cancelled
}
