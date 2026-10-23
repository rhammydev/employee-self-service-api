namespace EmployeeSelfServiceAPI.DTOs.Generic;


public record FileDownload(Stream Content, string ContentType, string FileName);
