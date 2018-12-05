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
using System.IO;
using System.Globalization;

namespace Rtsp.Sdp
{
    public class SdpFile
    {
        private static KeyValuePair<string, string> GetKeyValue(TextReader sdpStream)
        {
            string line = sdpStream.ReadLine();

            // end of file ?
            if(string.IsNullOrEmpty(line))
                return new KeyValuePair<string, string>(null, null);

            
            string[] parts = line.Split(new char[] { '=' }, 2);
            if (parts.Length != 2)
                throw new InvalidDataException();
            if (parts[0].Length != 1)
                throw new InvalidDataException();

            KeyValuePair<string, string> value = new KeyValuePair<string, string>(parts[0], parts[1]);
            return value;
        }

        /// <summary>
        /// Reads the specified SDP stream.
        /// As define in RFC 4566
        /// </summary>
        /// <param name="sdpStream">The SDP stream.</param>
        /// <returns></returns>
        public static SdpFile Read(TextReader sdpStream)
        {
            SdpFile returnValue = new SdpFile();
            KeyValuePair<string, string> value = GetKeyValue(sdpStream);

            // Version mandatory
            if (value.Key == "v")
            {
                returnValue.Version = int.Parse(value.Value, CultureInfo.InvariantCulture);
            }
            else
                throw new InvalidDataException();
            value = GetKeyValue(sdpStream);

            // Origin mandatory
            if (value.Key == "o")
            {
                returnValue.Origin = Origin.Parse(value.Value);
            }
            else
                throw new InvalidDataException();
            value = GetKeyValue(sdpStream);

            // Session mandatory
            if (value.Key == "s")
            {
                returnValue.Session = value.Value;
            }
            else
                throw new InvalidDataException();
            value = GetKeyValue(sdpStream);

            // Session optional
            if (value.Key == "i")
            {
                returnValue.SessionInformation = value.Value;
                value = GetKeyValue(sdpStream);
            }

            // Uri optional
            if (value.Key == "u")
            {
                returnValue.Url = new Uri(value.Value);
                value = GetKeyValue(sdpStream);
            }

            // Email optional
            if (value.Key == "e")
            {
                returnValue.Email = value.Value;
                value = GetKeyValue(sdpStream);
            }

            // Phone optional
            if (value.Key == "p")
            {
                returnValue.Phone = value.Value;
                value = GetKeyValue(sdpStream);
            }

            // Connexion optional
            if (value.Key == "c")
            {
                returnValue.Connection = Connection.Parse(value.Value);
                value = GetKeyValue(sdpStream);
            }

            // bandwidth optional
            if (value.Key == "b")
            {
                returnValue.Bandwidth = Bandwidth.Parse(value.Value);
                value = GetKeyValue(sdpStream);
            }

            //Timing
            while (value.Key == "t")
            {
                string timing = value.Value;
                string repeat = string.Empty;
                value = GetKeyValue(sdpStream);
                if (value.Key == "r")
                {
                    repeat = value.Value;
                    value = GetKeyValue(sdpStream);
                }
                returnValue.Timings.Add(new Timing(timing, repeat));
            }

            // timezone optional
            if (value.Key == "z")
            {

                returnValue.TimeZone = SdpTimeZone.ParseInvariant(value.Value);
                value = GetKeyValue(sdpStream);
            }

            // enkription key optional
            if (value.Key == "k")
            {

                returnValue.EncriptionKey = EncriptionKey.ParseInvariant(value.Value);
                value = GetKeyValue(sdpStream);
            }

            //Attribut optional multiple
            while (value.Key == "a")
            {
                returnValue.Attributs.Add(Attribut.ParseInvariant(value.Value));
                value = GetKeyValue(sdpStream);
            }

            while (value.Key == "m")
            {
                Media newMedia = ReadMedia(sdpStream, ref value);
                returnValue.Medias.Add(newMedia);
            }


            return returnValue;
        }

        private static Media ReadMedia(TextReader sdpStream, ref KeyValuePair<string, string> value)
        {
            Media returnValue = new Media(value.Value);
            value = GetKeyValue(sdpStream);

            // Media title
            if (value.Key == "i")
            {
                value = GetKeyValue(sdpStream);
            }

            // Connexion optional
            if (value.Key == "c")
            {
                returnValue.Connection = Connection.Parse(value.Value);
                value = GetKeyValue(sdpStream);
            }

            // bandwidth optional
            if (value.Key == "b")
            {
                returnValue.Bandwidth = Bandwidth.Parse(value.Value);
                value = GetKeyValue(sdpStream);
            }

            // enkription key optional
            if (value.Key == "k")
            {

                returnValue.EncriptionKey = EncriptionKey.ParseInvariant(value.Value);
                value = GetKeyValue(sdpStream);
            }

            //Attribut optional multiple
            while (value.Key == "a")
            {
                returnValue.Attributs.Add(Attribut.ParseInvariant(value.Value));
                value = GetKeyValue(sdpStream);
            }

            return returnValue;
        }


        public int Version { get; set; }


        public Origin Origin { get; set; }

        public string Session { get; set; }

        public string SessionInformation { get; set; }

        public Uri Url { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public Connection Connection { get; set; }

        public Bandwidth Bandwidth { get; set; }

        private readonly List<Timing> timingList = new List<Timing>();

        public IList<Timing> Timings
        {
            get
            {
                return timingList;
            }
        }

        public SdpTimeZone TimeZone { get; set; }

        public EncriptionKey EncriptionKey { get; set; }

        private readonly List<Attribut> attributs = new List<Attribut>();

        public IList<Attribut> Attributs
        {
            get
            {
                return attributs;
            }
        }

        private readonly List<Media> medias = new List<Media>();

        public IList<Media> Medias
        {
            get
            {
                return medias;
            }

        }
    
    }
}
