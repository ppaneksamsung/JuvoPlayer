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

using System;
using System.Collections.Generic;
using JuvoPlayer2_0.Impl.Framework;
using NSubstitute;
using NUnit.Framework;

namespace JuvoPlayer2_0.Tests.Impl.Framework
{
    [TestFixture]
    public class PropertyRegistryTests
    {
        [Test]
        public void GetByType_MissingProperty_ThrowsException()
        {
            var propertyStub = Substitute.For<IProperty>();
            var registry = new PropertyRegistry(new Dictionary<Type, IProperty>());

            Assert.Throws<KeyNotFoundException>(() => { registry.Get(propertyStub.GetType()); });
        }

        [Test]
        public void GetByTemplate_MissingProperty_ThrowsException()
        {
            var registry = new PropertyRegistry(new Dictionary<Type, IProperty>());

            Assert.Throws<KeyNotFoundException>(() => { registry.Get<DummyProperty>(); });
        }

        [Test]
        public void GetByType_PropertyAvailable_ReturnsProperty()
        {
            var propertyStub = Substitute.For<IProperty>();
            var properties = new Dictionary<Type, IProperty> {[propertyStub.GetType()] = propertyStub};
            var registry = new PropertyRegistry(properties);

            var received = registry.Get(propertyStub.GetType());

            Assert.That(received, Is.TypeOf(propertyStub.GetType()));
        }

        [Test]
        public void GetByTemplate_PropertyAvailable_ReturnsProperty()
        {
            var properties = new Dictionary<Type, IProperty> {[typeof(DummyProperty)] = new DummyProperty()};
            var registry = new PropertyRegistry(properties);

            var received = registry.Get<DummyProperty>();

            Assert.That(received, Is.TypeOf<DummyProperty>());
        }

        [Test]
        public void EnumerableConstructor_Called_ParsesProperly()
        {
            var registry = new PropertyRegistry(new List<IProperty> {new DummyProperty()});

            var received = registry.Get<DummyProperty>();

            Assert.That(received, Is.TypeOf<DummyProperty>());
        }

        private class DummyProperty : ReadWriteProperty<int>
        {
        }
    }
}