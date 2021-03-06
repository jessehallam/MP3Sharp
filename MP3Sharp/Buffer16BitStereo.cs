﻿// /***************************************************************************
//  * Buffer16BitStereo.cs
//  * Copyright (c) 2015 the authors.
//  * 
//  * All rights reserved. This program and the accompanying materials
//  * are made available under the terms of the GNU Lesser General Public License
//  * (LGPL) version 3 which accompanies this distribution, and is available at
//  * https://www.gnu.org/licenses/lgpl-3.0.en.html
//  *
//  * This library is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  * Lesser General Public License for more details.
//  *
//  ***************************************************************************/

using System;
using MP3Sharp.Decoding;

namespace MP3Sharp
{
    /// <summary>
    ///     Internal class used to queue samples that are being obtained from an Mp3 stream. 
    ///     This class handles stereo 16-bit data! Switch it out if you want mono or something.
    /// </summary>
    internal class Buffer16BitStereo : ABuffer
    {
        // This is stereo!
        private static readonly int CHANNELS = 2;
        // Write offset used in append_bytes
        private readonly byte[] m_Buffer = new byte[OBUFFERSIZE * 2]; // all channels interleaved
        private readonly int[] m_Bufferp = new int[MAXCHANNELS]; // offset in each channel not same!
        // end marker, one past end of array. Same as bufferp[0], but
        // without the array bounds check.
        private int m_End;
        // Read offset used to read from the stream, in bytes.
        private int m_Offset;

        public Buffer16BitStereo()
        {
            // Initialize the buffer pointers
            ClearBuffer();
        }

        public int BytesLeft
        {
            get
            {
                return m_End - m_Offset;
            }
        }

        /// Copies as much of this buffer as will fit into the output
        /// buffer. Return The amount of bytes copied.
        public int Read(byte[] bufferOut, int offset, int count)
        {
            int remaining = BytesLeft;
            int copySize;
            if (count > remaining)
            {
                copySize = remaining;
            }
            else
            {
                // Copy an even number of sample frames
                int remainder = count % (2 * CHANNELS);
                copySize = count - remainder;
            }

            Array.Copy(m_Buffer, m_Offset, bufferOut, offset, copySize);

            m_Offset += copySize;
            return copySize;
        }

        // Inefficiently write one sample value
        public override void Append(int channel, short valueRenamed)
        {
            m_Buffer[m_Bufferp[channel]] = (byte)(valueRenamed & 0xff);
            m_Buffer[m_Bufferp[channel] + 1] = (byte)(valueRenamed >> 8);

            m_Bufferp[channel] += CHANNELS * 2;
        }

        // efficiently write 32 samples
        public override void AppendSamples(int channel, float[] f)
        {
            // Always, 32 samples are appended
            int pos = m_Bufferp[channel];

            for (int i = 0; i < 32; i++)
            {
                float fs = f[i];
                if (fs > 32767.0f) // can this happen?
                    fs = 32767.0f;
                else if (fs < -32767.0f)
                    fs = -32767.0f;

                int sample = (int)fs;
                m_Buffer[pos] = (byte)(sample & 0xff);
                m_Buffer[pos + 1] = (byte)(sample >> 8);

                pos += CHANNELS * 2;
            }

            m_Bufferp[channel] = pos;
        }

        /// <summary>
        ///     This implementation does not clear the buffer.
        /// </summary>
        public override sealed void ClearBuffer()
        {
            m_Offset = 0;
            m_End = 0;

            for (int i = 0; i < CHANNELS; i++)
                m_Bufferp[i] = i * 2; // two bytes per channel
        }

        public override void SetStopFlag()
        {
        }

        public override void WriteBuffer(int val)
        {
            m_Offset = 0;

            // speed optimization - save end marker, and avoid
            // array access at read time. Can you believe this saves
            // like 1-2% of the cpu on a PIII? I guess allocating
            // that temporary "new int(0)" is expensive, too.
            m_End = m_Bufferp[0];
        }

        public override void Close()
        {
        }
    }
}
