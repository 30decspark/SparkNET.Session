﻿## Usage

To use **SparkNET.Session** for storing user authentication, follow these steps:

### Example Code:

```csharp
using SparkNET.Session;

// Add Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddSparkSession();

// Injection
public class TestController(SparkSession session)

// Get Session
session.Get("US_ID");
```