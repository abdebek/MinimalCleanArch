﻿# MinimalCleanArch.Extensions

Minimal API extensions for MinimalCleanArch.

## Overview

This package provides extensions for Minimal API endpoints:

- Validation filters
- Error handling filters
- Path parameter validation
- Standard response definitions

## Key Components

- ValidationFilter - Validates request bodies
- EndpointExtensions - Extension methods for endpoint definitions
- WithValidation<TDto>() - Adds validation for DTOs
- WithPathParamValidation<T>() - Validates path parameters
- WithErrorHandling() - Adds global error handling
- WithStandardResponses<TResponse>() - Adds standard OpenAPI response definitions
