namespace XLogger.Components.Zip.ZIP
{
    public interface IFileNamePreprocessor
    {
        string PreprocessName(string fullName);
    }
}