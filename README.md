# ETL Simulator Project Summary

This is a comprehensive educational diploma project demonstrating a complete ETL (Extract → Transform → Load) pipeline with a three-tier data warehouse architecture (Bronze/Silver/Gold layers) and web monitoring interface.

## Core Architecture

The system uses a shared SQL Server database (`EtlDb`) accessed by two applications:

1. **WPF Desktop Simulator** — generates test data and processes it through Bronze → Silver → Gold transformations
2. **ASP.NET Core Web Application** — provides dashboards, monitoring, scheduling, and administrative functions

The applications communicate via Named Pipes to trigger ETL jobs remotely.

## Key Technical Components

**Data Processing Pipeline:**
- Bronze layer ingests raw sales data with intentional quality issues
- Silver layer validates, deduplicates, and cleans records
- Gold layer populates a star-schema data warehouse with dimensions and fact tables

**Authentication & Authorization:**
Three user roles (Analyst, DataEngineer, Admin) with PBKDF2-SHA512 password hashing using 100,000 iterations.

**Infrastructure:**
- Entity Framework Core with 13 migrations
- 5 main controllers handling accounts, dashboards, ETL operations, scheduling, and administration
- Comprehensive logging via metadata tables tracking job executions

**Quality Assurance:**
117 unit tests (57 for WPF components, 60 for web services) using xUnit and EF Core in-memory providers.

## Deployment

Requirements: .NET 8 SDK, SQL Server 2019+, Windows OS. Setup involves configuring connection strings, applying database migrations, creating an initial admin account, then launching both the web application and WPF simulator separately.
