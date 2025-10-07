# CORS Configuration for Azure Functions

## Overview
CORS (Cross-Origin Resource Sharing) has been configured differently for local development and Azure deployment to ensure security while maintaining development flexibility.

## Configuration Details

### 🔒 Azure Deployment (Production)
**File:** `infra/main.bicep`

CORS is **restricted** to only allow requests from the Azure Static Web App:

```bicep
cors: {
  allowedOrigins: [
    'https://${staticWebApp.properties.defaultHostname}'
  ]
  supportCredentials: true
}
```

**Security Benefits:**
- ✅ Only your Static Web App can make requests to the Function App
- ✅ Prevents unauthorized cross-origin requests
- ✅ Supports credentials (cookies, auth headers) for authenticated requests
- ✅ Automatically uses the Static Web App's deployed URL

**Example Allowed Origin:**
```
https://azswa<resourceToken>.azurestaticapps.net
```

### 🛠️ Local Development
**File:** `src/ElasticOn.RiskAgent.Demo.Functions/local.settings.json`

CORS is **open** to allow all origins for local testing:

```json
{
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

**Development Benefits:**
- ✅ Allows testing from any localhost port (e.g., `http://localhost:3000`, `http://localhost:5173`)
- ✅ Enables testing with local Static Web App development server
- ✅ No CORS errors during development
- ✅ Credentials disabled for simplicity in local testing

## How It Works

### Local Development Flow
1. Run Function App locally: `func start` or `dotnet run`
2. Function App listens on `http://localhost:7071`
3. CORS accepts requests from **any origin** (`*`)
4. Test from your local web app on any port

### Azure Deployment Flow
1. Static Web App deployed at: `https://azswa<token>.azurestaticapps.net`
2. Function App deployed with CORS restricted to Static Web App URL
3. Static Web App makes API calls to Function App
4. CORS allows requests **only** from the Static Web App
5. Other origins are blocked (returns 403 Forbidden)

## Testing CORS

### Test Locally
1. Start the Function App:
   ```bash
   cd src/ElasticOn.RiskAgent.Demo.Functions
   func start
   ```

2. From your browser console (on any localhost origin):
   ```javascript
   fetch('http://localhost:7071/api/Chat', {
     method: 'POST',
     headers: { 'Content-Type': 'application/json' },
     body: JSON.stringify({ message: 'test' })
   })
   .then(r => r.json())
   .then(console.log);
   ```

   Should work without CORS errors ✅

### Test Azure Deployment
1. From the Static Web App (allowed):
   ```javascript
   // This will work ✅
   fetch('https://azfunc<token>.azurewebsites.net/api/Chat', {
     method: 'POST',
     headers: { 'Content-Type': 'application/json' },
     body: JSON.stringify({ message: 'test' })
   })
   ```

2. From another domain (blocked):
   ```javascript
   // This will fail with CORS error ❌
   // "Access-Control-Allow-Origin" error
   ```

## Customizing CORS Origins

### Add Multiple Origins (Azure)
If you need to allow multiple origins in Azure, update `main.bicep`:

```bicep
cors: {
  allowedOrigins: [
    'https://${staticWebApp.properties.defaultHostname}'
    'https://custom-domain.com'
    'https://another-domain.com'
  ]
  supportCredentials: true
}
```

### Add Specific Origins (Local)
For local development with specific origins, update `local.settings.json`:

```json
{
  "Host": {
    "CORS": "http://localhost:3000,http://localhost:5173,http://localhost:8080"
  }
}
```

## Troubleshooting

### CORS Error in Browser Console
```
Access to fetch at 'https://azfunc.../api/Chat' from origin 'https://example.com' 
has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present
```

**Solution:**
1. Check if the origin is in the allowed list in `main.bicep`
2. Redeploy infrastructure: `azd provision`
3. Verify CORS settings in Azure Portal → Function App → CORS

### CORS Working Locally But Not in Azure
**Likely Cause:** The Azure deployment has restricted CORS while local allows all origins.

**Solution:**
1. Verify the Static Web App URL is correct in the allowed origins
2. Check Function App CORS settings in Azure Portal
3. Ensure you deployed the latest Bicep changes

### Preflight OPTIONS Request Failing
**Cause:** Browser sends OPTIONS request before actual request (CORS preflight).

**Solution:** Azure Functions automatically handles OPTIONS requests. Ensure:
1. Function App is running
2. CORS configuration includes the origin
3. HTTP trigger allows anonymous access or authentication is properly configured

## Best Practices

### ✅ DO:
- Keep production CORS restrictive (specific origins only)
- Use `*` for local development only
- Enable credentials (`supportCredentials: true`) when using authentication
- Document all allowed origins and reasons

### ❌ DON'T:
- Never use `"*"` in production (security risk)
- Don't commit secrets in CORS configuration
- Don't disable CORS entirely (use proper configuration instead)
- Don't add origins without verifying they need access

## Security Considerations

### Why Restrict CORS in Production?
- **Prevents Data Theft:** Malicious sites can't make requests to your API
- **Protects User Data:** User sessions/cookies can't be hijacked
- **Reduces Attack Surface:** Limits who can interact with your API
- **Compliance:** Many security standards require CORS restrictions

### When to Allow Multiple Origins?
- Multiple frontend deployments (staging, production)
- Partner integrations (carefully verified partners only)
- Mobile apps with specific domains
- Internal tools with known URLs

Always prefer the **principle of least privilege** - only allow what's absolutely necessary.

## References
- [Azure Functions CORS Documentation](https://learn.microsoft.com/azure/azure-functions/functions-how-to-use-azure-function-app-settings#cors)
- [MDN CORS Guide](https://developer.mozilla.org/docs/Web/HTTP/CORS)
- [Azure Static Web Apps with Functions](https://learn.microsoft.com/azure/static-web-apps/functions-bring-your-own)
