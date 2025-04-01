﻿## Usage

To use **SparkNET.Session** for storing user authentication, follow these steps:

### Example Code:

```csharp
using SparkNET.Session;

// Add Service
builder.Services.AddHttpContextAccessor(); // required
builder.Services.AddSparkSession();

// Dependency Injection
private readonly SparkSession session = _session;

// Get
string? USER_NAME = session.Get("USER_NAME");

// Set
session.Set("USER_NAME", USER_NAME);

// Remove
session.Remove("USER_NAME");

// Commit (after Get/Set/Remove)
session.SaveChanges();
```