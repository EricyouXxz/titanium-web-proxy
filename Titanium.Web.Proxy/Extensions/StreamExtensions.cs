﻿using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using StreamExtended.Network;

namespace Titanium.Web.Proxy.Extensions
{
    /// <summary>
    /// Extensions used for Stream and CustomBinaryReader objects
    /// </summary>
    internal static class StreamExtensions
    {
        /// <summary>
        /// Copy streams asynchronously
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="onCopy"></param>
        /// <param name="bufferSize"></param>
        internal static async Task CopyToAsync(this Stream input, Stream output, Action<byte[], int, int> onCopy, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            while (true)
            {
                int num = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                int bytesRead;
                if ((bytesRead = num) != 0)
                {
                    await output.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                    onCopy?.Invoke(buffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// copies the specified bytes to the stream from the input stream
        /// </summary>
        /// <param name="streamReader"></param>
        /// <param name="stream"></param>
        /// <param name="totalBytesToRead"></param>
        /// <returns></returns>
        internal static async Task CopyBytesToStream(this CustomBinaryReader streamReader, Stream stream, long totalBytesToRead)
        {
            var buffer = streamReader.Buffer;
            long remainingBytes = totalBytesToRead;

            while (remainingBytes > 0)
            {
                int bytesToRead = buffer.Length;
                if (remainingBytes < bytesToRead)
                {
                    bytesToRead = (int)remainingBytes;
                }

                int bytesRead = await streamReader.ReadBytesAsync(buffer, bytesToRead);
                if (bytesRead == 0)
                {
                    break;
                }

                remainingBytes -= bytesRead;

                await stream.WriteAsync(buffer, 0, bytesRead);
            }
        }

        /// <summary>
        /// Copies the stream chunked
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        internal static async Task CopyBytesToStreamChunked(this CustomBinaryReader reader, Stream stream)
        {
            while (true)
            {
                string chunkHead = await reader.ReadLineAsync();
                int chunkSize = int.Parse(chunkHead, NumberStyles.HexNumber);

                if (chunkSize != 0)
                {
                    await CopyBytesToStream(reader, stream, chunkSize);
                }

                //chunk trail
                await reader.ReadLineAsync();

                if (chunkSize == 0)
                {
                    break;
                }
            }
        }
    }
}
