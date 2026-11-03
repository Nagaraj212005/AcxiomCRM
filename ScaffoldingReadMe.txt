AcxiomCRM setup

The application uses SQL Server via the DefaultConnection connection string.
In Development, startup applies pending EF Core migrations and inserts sample
customers only when the Customers table is empty. In other environments, apply
migrations as part of deployment with `dotnet ef database update`.

Identity email confirmation and password reset use SMTP. Configure these
environment variables for email delivery:

- Email__SmtpHost
- Email__SmtpPort (optional; defaults to 587)
- Email__EnableSsl (optional; defaults to true)
- Email__SenderAddress
- Email__Username (optional)
- Email__Password (optional; keep secrets in environment/user secrets)

Email configuration is not required for local Development registration or
sign-in because account confirmation is disabled in that environment.
