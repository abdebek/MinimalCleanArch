# MinimalCleanArch.Security

Security components for MinimalCleanArch.

## Overview

This package provides security features:

- Column-level encryption for sensitive data
- AES encryption implementation
- Value converters for Entity Framework Core

## Key Components

- EncryptedAttribute - Marks properties for encryption
- IEncryptionService - Interface for encryption services
- AesEncryptionService - AES implementation of IEncryptionService
- EncryptedConverter - Value converter for encrypted properties
- ModelBuilderExtensions - Extensions for configuring encryption
