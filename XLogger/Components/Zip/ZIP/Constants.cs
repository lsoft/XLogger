namespace XLogger.Components.Zip.ZIP
{
    public sealed class Constants
	{
        public const int BufferSize = 4096;
        public const int CentralDirectoryEndSignature = 0x06054b50;
        public const int CentralDirSizeOffset = 12;
        public const int CentralHeaderSignature = 0x02014b50;
        public const int HeaderSignature = 0x04034b50;
        public const int HeaderSignatureBytes = 4;
        public const int IntSize = 4;
        public const int ShortSize = 2;
        public const uint StartCrc = 0xFFFFFFFF;
        public const short VersionMadeBy = 45;
        public const short VersionNeededToExtract = 20;

        private Constants()
        {
        }
	}//class
}//ns
