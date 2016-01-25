using System;
using System.Runtime.Serialization;
using Ipc;
using Newtonsoft.Json;
using NUnit.Framework;
using UnitTests.Utils;

namespace UnitTests
{
    [TestFixture]
    public class JSonSerializerTest
    {
        [DataContract]
        public class SomeClass
        {
            [DataMember]
            public string SomeProperty { get; set; }
        }

        [Test()]
        public void Test_Serialize_Deserialize()
        {
            IObjectSerializer serializer = new MsDataContractJsonSerializer();
            var someClass = new SomeClass {SomeProperty = "SomeStringValue"};

            var jsonStr = serializer.SerializeObject(someClass);

            var someClass2 = serializer.DeserializeObject<SomeClass>(jsonStr);

            Assert.AreNotEqual(someClass.GetHashCode(), someClass2.GetHashCode());
            Assert.AreEqual(someClass.SomeProperty, someClass2.SomeProperty);
        }


        [DataContract]
        public class SomeBulkyClass
        {
            [DataMember]
            public byte[] SomeBulkyProperty { get; set; }
        }

        [TestCase(   1)]
        [TestCase(  10)]
        [TestCase( 100)]
        [TestCase(1000)]
        public void Test_Serialize_Deserialize_Bulk_InKiloByte(int kb)
        {
            int numberKiloBytes = 1024* kb;
            var buffer = new byte[numberKiloBytes];
            for (int i = 0; i < numberKiloBytes; ++i)
            {
                buffer[i] = (byte)(i % 2);
            }

            var  someBulkyClass = new SomeBulkyClass { SomeBulkyProperty = buffer };

            IObjectSerializer serializer = new MsDataContractJsonSerializer();
            SomeBulkyClass deserializedSomeBulkyClass = null;
            var measuredMs = Performance.MeasureMs(() =>
            {
                var jsonStr = serializer.SerializeObject(someBulkyClass);
                deserializedSomeBulkyClass = serializer.DeserializeObject<SomeBulkyClass>(jsonStr);
            });
            
            Assert.AreNotEqual(someBulkyClass.GetHashCode(), deserializedSomeBulkyClass.GetHashCode());
            
            var before = someBulkyClass.SomeBulkyProperty;
            var after  = deserializedSomeBulkyClass.SomeBulkyProperty;
            for (int i = 0; i < numberKiloBytes; ++i)
            {
                Assert.AreEqual(before[i], after[i]);    
            }

            Console.WriteLine("Serialize-Deserialize of {0} KB took {1}ms", kb, measuredMs);
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Test_Json_dot_NET_Serialize_Deserialize_Bulk_InKiloByte(int kb)
        {
            int numberKiloBytes = 1024 * kb;
            var buffer = new byte[numberKiloBytes];
            for (int i = 0; i < numberKiloBytes; ++i)
            {
                buffer[i] = (byte)(i % 2);
            }

            var someBulkyClass = new SomeBulkyClass { SomeBulkyProperty = buffer };

            SomeBulkyClass deserializedSomeBulkyClass = null;
            var measuredMs = Performance.MeasureMs(() =>
            {
                var jsonStr = JsonConvert.SerializeObject(someBulkyClass);
                deserializedSomeBulkyClass = JsonConvert.DeserializeObject<SomeBulkyClass>(jsonStr);
            });

            Assert.AreNotEqual(someBulkyClass.GetHashCode(), deserializedSomeBulkyClass.GetHashCode());

            var before = someBulkyClass.SomeBulkyProperty;
            var after = deserializedSomeBulkyClass.SomeBulkyProperty;
            for (int i = 0; i < numberKiloBytes; ++i)
            {
                Assert.AreEqual(before[i], after[i]);
            }

            Console.WriteLine("Serialize-Deserialize of {0} KB took {1}ms", kb, measuredMs);
        }
    }
}