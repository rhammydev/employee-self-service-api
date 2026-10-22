namespace EmployeeSelfServiceAPI.Helpers;

public record AllowedFileType(string[] Extensions, string ContentType, byte[] Signature);

public class FileTypes
{
    public static readonly AllowedFileType Pdf  = new([".pdf"], "application/pdf", [0x25, 0x50, 0x44, 0x46]);         // %PDF
    public static readonly AllowedFileType Png  = new([".png"], "image/png",       [0x89, 0x50, 0x4E, 0x47]);
    public static readonly AllowedFileType Jpeg = new([".jpg", ".jpeg"], "image/jpeg", [0xFF, 0xD8, 0xFF]);

    public static readonly AllowedFileType[] Documents = [Pdf, Jpeg, Png];  
    public static readonly AllowedFileType[] PdfOnly = [Pdf];           
}