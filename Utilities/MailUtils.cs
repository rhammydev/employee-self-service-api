using System.Net;

namespace EmployeeSelfServiceAPI.Utilities;

public static class MailUtils
{
    private const string Brand = "Employee Self-Service";

    public static string Notification(string firstName, string title, string message, string eyebrow) => Template(
        title,
        eyebrow,
        $"""
         <p style="margin:0 0 18px">Hello {E(firstName)},</p>
         <p style="margin:0 0 22px">{E(message)}</p>
         <p style="margin:22px 0 0;font-size:13px;color:#667085">Sign in to {Brand} to see the details.</p>
         """);

    public static string AccountSetup(string firstName, string employeeNumber, string link, string token, int expiryHours) => Template(
        "Set up your account",
        "Welcome",
        $"""
         <p style="margin:0 0 18px">Hello {E(firstName)},</p>
         <p style="margin:0 0 22px">An {Brand} account has been created for you. Set your password to activate it.</p>
         {Details(("Employee number", employeeNumber), ("Link expires", $"In {expiryHours} hours"), ("Use", "Once only"))}
         {Button("Set my password", link)}
         {TokenBox("Setup token", token)}
         <p style="margin:22px 0 0;font-size:13px;color:#667085">If you were not expecting this, contact HR.</p>
         """);

    public static string PasswordReset(string firstName, string link, string token, int expiryMinutes) => Template(
        "Reset your password",
        "Password assistance",
        $"""
         <p style="margin:0 0 18px">Hello {E(firstName)},</p>
         <p style="margin:0 0 22px">We received a request to reset your {Brand} password.</p>
         {Button("Reset my password", link)}
         {TokenBox("Reset token", token)}
         <div style="background:#fff7e8;border-left:4px solid #f0a23b;border-radius:8px;padding:14px 16px;color:#76501d;font-size:14px;margin-top:22px">
           This link expires in <strong>{expiryMinutes} minutes</strong> and works once. Never share it with anyone.
         </div>
         <p style="margin:22px 0 0;font-size:13px;color:#667085">If you did not request this, ignore this email. Your password has not changed.</p>
         """);

    private static string Template(string title, string eyebrow, string content) =>
        $"""
         <!doctype html>
         <html lang="en">
         <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{E(title)}</title></head>
         <body style="margin:0;background:#eef2f7;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;color:#263238">
           <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#eef2f7;padding:36px 12px">
             <tr><td align="center">
               <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:590px;background:#fff;border-radius:22px;overflow:hidden;box-shadow:0 12px 36px rgba(20,50,90,.12)">
                 <tr><td style="background:#1f4e79;padding:32px 38px;color:#fff">
                   <table role="presentation" width="100%"><tr>
                     <td style="width:50px"><div style="width:44px;height:44px;line-height:44px;text-align:center;border-radius:13px;background:#fff;color:#1f4e79;font-size:18px;font-weight:800">ESS</div></td>
                     <td><div style="font-size:12px;letter-spacing:2px;text-transform:uppercase;color:#b9d1ea">{E(eyebrow)}</div><div style="font-size:24px;font-weight:750;margin-top:5px">{E(title)}</div></td>
                   </tr></table>
                 </td></tr>
                 <tr><td style="padding:34px 38px;font-size:15px;line-height:1.65">{content}</td></tr>
                 <tr><td style="padding:24px 38px;background:#f5f8fb;border-top:1px solid #dce4ed;color:#667085;font-size:12px;line-height:1.6">
                   <strong style="color:#1f4e79">{Brand}</strong><br>
                   This is an automated message; please do not reply.
                 </td></tr>
               </table>
             </td></tr>
           </table>
         </body>
         </html>
         """;

    private static string Details(params (string Label, string Value)[] rows)
    {
        var body = string.Join(string.Empty, rows.Select(row =>
            $"<tr><td style='padding:12px 14px;color:#667085;border-bottom:1px solid #dce4ed;width:38%'>{E(row.Label)}</td>" +
            $"<td style='padding:12px 14px;font-weight:650;border-bottom:1px solid #dce4ed'>{E(row.Value)}</td></tr>"));
        return $"<table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='border:1px solid #dce4ed;border-radius:12px;border-collapse:separate;overflow:hidden;margin-bottom:22px'>{body}</table>";
    }

    private static string Button(string label, string url) =>
        $"""
         <div style="text-align:center;margin:0 0 22px">
           <a href="{E(url)}" style="display:inline-block;background:#1f4e79;color:#fff;text-decoration:none;font-weight:700;padding:13px 26px;border-radius:10px">{E(label)}</a>
         </div>
         """;

    private static string TokenBox(string label, string token) =>
        $"""
         <div style="background:#eef4ff;border:1px solid #b8d4fe;border-radius:12px;padding:16px;text-align:center">
           <div style="font-size:12px;letter-spacing:2px;text-transform:uppercase;color:#3b6cb5;margin-bottom:8px">{E(label)}</div>
           <div style="font-family:Consolas,monospace;font-size:13px;word-break:break-all;color:#1a4a8a">{E(token)}</div>
         </div>
         """;

    private static string E(string value) => WebUtility.HtmlEncode(value);
}
