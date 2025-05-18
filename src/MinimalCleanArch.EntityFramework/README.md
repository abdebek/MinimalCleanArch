# MinimalCleanArch Core

The core components of the MinimalCleanArch library for implementing Clean Architecture.

## Overview

This package contains the fundamental interfaces and base classes for implementing Clean Architecture:

- Domain Entities (IEntity, BaseEntity)
- Domain Exceptions
- Repository Interfaces
- Specification Pattern Interfaces

## Key Components

- IEntity<TKey> - Base interface for all entities with a specific key type
- BaseEntity<TKey> - Base implementation of IEntity<TKey>
- BaseEntity - Convenience class for entities with int keys
- IAuditableEntity - Interface for entities with audit fields
- ISoftDelete - Interface for entities that support soft delete
- DomainException - Exception for domain validation errors
- ISpecification<T> - Interface for specifications
- BaseSpecification<T> - Base implementation of ISpecification<T>
"@ | Out-File -FilePath "src/MinimalCleanArch/README.md" -Encoding utf8

# Create src/MinimalCleanArch.EntityFramework/README.md
New-Item -ItemType Directory -Path "src/MinimalCleanArch.EntityFramework" -Force | Out-Null
@"
# MinimalCleanArch.EntityFramework

Entity Framework Core implementation for MinimalCleanArch.

## Overview

This package provides Entity Framework Core implementations for the core interfaces:

- DbContextBase with support for soft delete and auditing
- Repository implementations
- Specification evaluator

## Key Components

- DbContextBase - Base DbContext with soft delete and audit support
- Repository<TEntity> - Implementation of IRepository<TEntity>
- SpecificationEvaluator<T> - Translates specifications to LINQ queries
