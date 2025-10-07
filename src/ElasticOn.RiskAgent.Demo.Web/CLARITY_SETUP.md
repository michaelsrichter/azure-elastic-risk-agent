# Microsoft Clarity Integration

This application includes support for [Microsoft Clarity](https://clarity.microsoft.com/) analytics tracking.

## Configuration

Clarity tracking is configured via the `appsettings.json` file:

```json
{
  "Clarity": {
    "Enabled": false,
    "ProjectId": ""
  }
}
```

### Settings

- **Enabled**: Set to `true` to enable Clarity tracking, `false` to disable
- **ProjectId**: Your Microsoft Clarity project ID (e.g., `abcd1234`)

## Setup Instructions

1. **Create a Clarity Account**
   - Go to [https://clarity.microsoft.com](https://clarity.microsoft.com)
   - Sign in with your Microsoft account
   - Create a new project

2. **Get Your Project ID**
   - In your Clarity project dashboard
   - Copy the Project ID (found in the setup instructions)

3. **Configure the Application**
   
   For **Development** (`wwwroot/appsettings.Development.json`):
   ```json
   {
     "Clarity": {
       "Enabled": false,
       "ProjectId": ""
     }
   }
   ```

   For **Production** (`wwwroot/appsettings.json`):
   ```json
   {
     "Clarity": {
       "Enabled": true,
       "ProjectId": "your-clarity-project-id"
     }
   }
   ```

4. **Deploy**
   - Deploy your application
   - Clarity will start tracking automatically
   - View analytics in your Clarity dashboard

## Features Tracked

Microsoft Clarity automatically tracks:
- **User Sessions**: Full session recordings
- **Heatmaps**: Click, scroll, and attention heatmaps
- **User Behavior**: Navigation patterns and interactions
- **Rage Clicks**: Frustrated user interactions
- **Dead Clicks**: Clicks on non-interactive elements
- **Quick Backs**: Users who quickly leave pages

## Privacy Considerations

- Clarity is disabled by default in development
- Consider enabling only in production environments
- Review [Clarity's privacy policy](https://clarity.microsoft.com/privacy)
- Add privacy notice to your Privacy Policy page
- Comply with GDPR/CCPA requirements for your users

## Logging

The application logs Clarity initialization:
- Info: When Clarity is disabled
- Info: When Clarity initializes successfully
- Warning: When enabled but ProjectId is missing
- Error: If initialization fails

Check browser console and application logs for status.

## Troubleshooting

### Clarity Not Working

1. Verify `Enabled` is set to `true`
2. Verify `ProjectId` is correct
3. Check browser console for errors
4. Wait 2-5 minutes for Clarity dashboard to update
5. Ensure no ad blockers are interfering

### Testing

To test Clarity in development:
1. Set `Enabled: true` in `appsettings.Development.json`
2. Add your ProjectId
3. Run the application
4. Check Clarity dashboard for live sessions

## Additional Resources

- [Clarity Documentation](https://docs.microsoft.com/en-us/clarity/)
- [Clarity Setup Guide](https://clarity.microsoft.com/setup)
- [Privacy & Compliance](https://clarity.microsoft.com/privacy)
