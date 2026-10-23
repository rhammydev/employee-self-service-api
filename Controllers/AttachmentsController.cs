using EmployeeSelfServiceAPI.Extensions;
using EmployeeSelfServiceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceAPI.Controllers;

[ApiController]
[Route("api/v1/attachments")]
[Authorize]
public class AttachmentsController(IAttachmentService attachmentService) : ControllerBase
{
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var (error, file) = await attachmentService.DownloadAsync(User.GetEmployeeId(), User.GetRoles(), id);
        if (error != null)
            return StatusCode(error.HttpStatusCode, error);

        return File(file!.Content, file.ContentType, file.FileName);
    }
}