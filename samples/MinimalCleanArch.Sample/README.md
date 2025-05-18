# MinimalCleanArch Sample Application

A sample application demonstrating MinimalCleanArch usage.

## Overview

This sample demonstrates:

- Domain entities with business rules
- Entity Framework Core integration
- Minimal API with validation
- Column-level encryption
- Soft delete functionality

## Running the Sample

1. Set your connection string in ppsettings.json:
   `json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MinimalCleanArchSample;Trusted_Connection=True;MultipleActiveResultSets=true"
     },
     "Encryption": {
       "Key": "your-strong-encryption-key-at-least-32-characters"
     }
   }
   `

2. Run the application:
   `
   dotnet run
   `

3. Browse to http://localhost:5000/swagger to see the API documentation
