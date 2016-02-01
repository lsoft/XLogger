using System;
using XLogger.Helper;

namespace XLogger.Components.FileCycling
{
    /// <summary>
    /// File-writer with a file-cycling functionality.
    /// </summary>
    public interface IFileCycling : IWriteable, IDisposable
    {
    }
}