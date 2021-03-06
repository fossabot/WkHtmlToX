#nullable enable
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using AdaskoTheBeAsT.WkHtmlToX.Abstractions;
using AdaskoTheBeAsT.WkHtmlToX.Exceptions;

namespace AdaskoTheBeAsT.WkHtmlToX.Modules
{
    [ExcludeFromCodeCoverage]
    internal abstract class WkHtmlToPdfModule
        : IWkHtmlToPdfModule
    {
        protected const int MaxBufferSize = 2048;

        public abstract IntPtr CreateObjectSettings();

        public abstract int DestroyObjectSetting(
            IntPtr settings);

        public abstract int SetObjectSetting(
            IntPtr settings,
            string name,
            string? value);

        public string GetObjectSetting(
            IntPtr settings,
            string name)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(MaxBufferSize);
            try
            {
                var retVal = GetObjectSettingImpl(
                    settings,
                    name,
                    buffer);

                if (retVal != 1)
                {
                    throw new GetObjectSettingsFailedException($"GetObjectSettings failed for obtaining setting={name}");
                }

                var nullPos = Array.IndexOf(buffer, byte.MinValue);

                return Encoding.UTF8.GetString(buffer, 0, nullPos);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public abstract void AddObject(
            IntPtr converter,
            IntPtr objectSettings,
            byte[] data);

        public abstract void AddObject(
            IntPtr converter,
            IntPtr objectSettings,
            string data);

        protected abstract int GetObjectSettingImpl(
            IntPtr settings,
            string name,
            byte[] buffer);
    }
}
