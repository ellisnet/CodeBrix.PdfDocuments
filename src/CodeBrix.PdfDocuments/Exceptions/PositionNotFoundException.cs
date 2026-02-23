using CodeBrix.PdfDocuments.Pdf;

namespace CodeBrix.PdfDocuments.Exceptions; //Was previously: namespace PdfSharpCore.Exceptions;

public class PositionNotFoundException : System.Exception
{
    public PositionNotFoundException(PdfObjectID id) : base($"Object with ID {id} resolved with negative position ") { }
}