﻿using JuvoPlayer.Common;
using JuvoPlayer.Common.Logging;
using JuvoPlayer.DRM.DummyDrm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuvoPlayer.TizenTests.testcase
{
    [TestFixture]
    public class TSDummyDrmSession
    {
        private LoggerManager savedLoggerManager;
        private byte[] data = new byte[] {

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            savedLoggerManager = LoggerManager.ResetForTests();
            LoggerManager.Configure("JuvoPlayer=Verbose", CreateLoggerFunc);
        }

        private static LoggerBase CreateLoggerFunc(string channel, LogLevel level)
        {
            return new ConsoleLogger(channel, level);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            LoggerManager.RestoreForTests(savedLoggerManager);
        }

        public EncryptedPacket CreateDummyEncryptedPacket()
        {
            return new EncryptedPacket
            {
                Data = data,
                Dts = TimeSpan.FromSeconds(1),
                Pts = TimeSpan.FromSeconds(1),
                IsEOS = false,
                IsKeyFrame = true,
                StreamType = StreamType.Video,
            };
        }

        [Test]
        public async Task DecryptPacket_WhenPacketIsValid_DecryptsSuccessfully()
        {
            using (var drmSession = DummyDrmSession.Create())
            {
                await drmSession.Initialize();

                var encrypted = CreateDummyEncryptedPacket();

                using (var decrypted = await drmSession.DecryptPacket(encrypted))
                {
                    Assert.That(decrypted, Is.Not.Null);
                    Assert.That(decrypted, Is.InstanceOf<DecryptedEMEPacket>());

                    var decryptedEme = decrypted as DecryptedEMEPacket;

                    Assert.That(decryptedEme.Dts, Is.EqualTo(encrypted.Dts));
                    Assert.That(decryptedEme.Pts, Is.EqualTo(encrypted.Pts));
                    Assert.That(decryptedEme.IsEOS, Is.EqualTo(encrypted.IsEOS));
                    Assert.That(decryptedEme.IsKeyFrame, Is.EqualTo(encrypted.IsKeyFrame));
                    Assert.That(decryptedEme.StreamType, Is.EqualTo(encrypted.StreamType));
                    Assert.That(decryptedEme.HandleSize, Is.Not.Null);
                    Assert.That(decryptedEme.HandleSize.handle, Is.GreaterThan(0));
                    Assert.That(decryptedEme.HandleSize.size, Is.EqualTo(data.Length));
                }
            }
        }
    }
}