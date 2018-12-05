/*!
 * https://github.com/SamsungDForum/JuvoPlayer
 * Copyright 2018, Samsung Electronics Co., Ltd
 * Licensed under the MIT license
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rtsp.Messages
{
    public class RtspRequestSetup : RtspRequest
    {

        // Constructor
        public RtspRequestSetup()
        {
            Command = "SETUP * RTSP/1.0";
        }


        /// <summary>
        /// Gets the transports associate with the request.
        /// </summary>
        /// <value>The transport.</value>
        public RtspTransport[] GetTransports()
        {

            if (!Headers.ContainsKey(RtspHeaderNames.Transport))
                return new RtspTransport[] { new RtspTransport() };

            string[] items = Headers[RtspHeaderNames.Transport].Split(',');
            return items.Select(o => RtspTransport.Parse(o)).ToArray();
        }

        public void AddTransport(RtspTransport newTransport)
        {
            string actualTransport = string.Empty;
            if(Headers.ContainsKey(RtspHeaderNames.Transport))
                actualTransport = Headers[RtspHeaderNames.Transport] + ",";
            Headers[RtspHeaderNames.Transport] = actualTransport + newTransport.ToString();



        }

    }
}
