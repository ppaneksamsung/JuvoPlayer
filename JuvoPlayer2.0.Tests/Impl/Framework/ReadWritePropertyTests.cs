/*!
 *
 * [https://github.com/SamsungDForum/JuvoPlayer])
 * Copyright 2020, Samsung Electronics Co., Ltd
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
 *
 */

using JuvoPlayer2_0.Impl.Framework;
using NUnit.Framework;

namespace JuvoPlayer2_0.Tests.Impl.Framework
{
    [TestFixture]
    public class ReadWritePropertyTests
    {
        [Test]
        public void Write_CalledWithoutDelegate_DoesntThrow()
        {
            var property = new ReadWriteProperty<int>();

            Assert.DoesNotThrow(() => property.Write(1));
        }

        [Test]
        public void ReadWrite_WriteCalled_ReadReturnsNewProperty()
        {
            var property = new ReadWriteProperty<int>();

            property.Write(1);
            var received = property.Read();

            Assert.That(received, Is.EqualTo(1));
        }

        [Test]
        public void Read_DefaultValue_ReadReturnsDefaultValue()
        {
            var property = new ReadWriteProperty<int>(1);

            var received = property.Read();

            Assert.That(received, Is.EqualTo(1));
        }

        [Test]
        public void Write_Called_InvokesPropertyChanged()
        {
            var property = new ReadWriteProperty<int>();
            var eventReceived = false;
            property.PropertyChanged = _ => { eventReceived = true; };

            property.Write(1);

            Assert.That(eventReceived, Is.True);
        }
    }
}