#if (UseAuth)
namespace MCA.Infrastructure.Providers;

public static class EmailLayouts
{
    public static string WrapInLayout(string appName, string title, string bodyHtml) => $"""
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8">
          <title>{title}</title>
          <meta name="viewport" content="width=device-width, initial-scale=1">
        </head>
        <body style="font-family: Arial, sans-serif; background: #f4f4f4; margin: 0; padding: 20px;">
          <div style="max-width: 600px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 32px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
            <h2 style="color: #333; margin-top: 0;">{appName}</h2>
            <hr style="border: none; border-top: 1px solid #eee; margin: 16px 0;">
            {bodyHtml}
            <hr style="border: none; border-top: 1px solid #eee; margin: 24px 0 16px;">
            <small style="color: #888;">This email was sent by {appName}. If you didn't request this, please ignore it.</small>
          </div>
        </body>
        </html>
        """;
}
#endif
