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

using System.Threading;

namespace JuvoPlayer2_0.Impl.Framework
{
    public class ReadWriteProperty<TValue> : IProperty<TValue>
    {
        private TValue _value;
        private readonly ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim();
        public PropertyChangedDelegate PropertyChanged { get; set; }

        public ReadWriteProperty(TValue defaultValue = default, PropertyChangedDelegate propertyChanged = default)
        {
            _value = defaultValue;
            PropertyChanged = propertyChanged;
        }

        public void Write(TValue value)
        {
            _readerWriterLock.EnterWriteLock();
            try
            {
                _value = value;
                NotifyBlock();
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        private void NotifyBlock()
        {
            PropertyChanged?.Invoke(this);
        }

        public TValue Read()
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                return _value;
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }
    }
}